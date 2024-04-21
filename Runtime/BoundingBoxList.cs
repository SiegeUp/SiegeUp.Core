using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SiegeUp.Core
{
    public class BoundingBoxList : MonoBehaviour
    {
        [SerializeField] List<BoundingBox> boundingBoxes;
        [SerializeField] List<BoundingBox> passableBoundingBoxes;

        public BoundingBox MainBound => boundingBoxes.Count > 0 ? boundingBoxes[0] : null;
        public IReadOnlyList<BoundingBox> BoundingBoxes => boundingBoxes;

        public List<Vector2Int> GetCurrentPointsOnGrid()
        {
            var intersectingPoints = GetPointsOnGrid(transform.position, transform.rotation, false);
            return intersectingPoints.Select(x => MathUtils.RoundToVector2Int(x)).ToList();
        }

        public List<Vector2Int> GetCurrentPassablePointsOnGrid()
        {
            var intersectingPoints = GetPointsOnGrid(transform.position, transform.rotation, true);
            return intersectingPoints.Select(x => MathUtils.RoundToVector2Int(x)).ToList();
        }

        public List<Vector2Int> GetPointsOnGrid(Vector3 position, Quaternion rotation, bool isPassable)
        {
            var intersectingPoints = GetIntersectingPoints(position, rotation, isPassable);
            return intersectingPoints.Select(x => MathUtils.RoundToVector2Int(x)).ToList();
        }

        List<Vector2> GetIntersectingPoints(Vector3 position, Quaternion rotation, bool isPassable)
        {
            var localIntersectingPoints = GetLocalIntersectingPoints(rotation, isPassable);
            return localIntersectingPoints.Select(x => x + position.GetXZ()).ToList();
        }

        List<Vector2> GetLocalIntersectingPoints(Quaternion rotation, bool isPassable)
        {
            var boundingBoxesToCheck = isPassable ? passableBoundingBoxes : boundingBoxes;
            var boundigBoxesRects = boundingBoxesToCheck.Select(x => x.GetLocalRect()).ToList();
            return GetLocalIntersectingPointsOfRects(boundigBoxesRects, rotation.eulerAngles.y);
        }

        public List<Vector2> GetLocalIntersectingPointsOfRects(List<Rect> rects, float angle)
        {
            if (rects == null || rects.Count == 0)
                return new List<Vector2>();

            Vector2[] directions = new[] { new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, -1), new Vector2(-1, 1) };
            var rotQuaternion = Quaternion.Euler(0, 0, angle);
            var worldPoints = new HashSet<Vector2>();

            var overallRect = MathUtils.GetOverallRect(rects);

            float offsetX = Mathf.Ceil(overallRect.size.x) % 2 == 0 ? 0.5f : 0;
            float offsetY = Mathf.Ceil(overallRect.size.y) % 2 == 0 ? 0.5f : 0;
            var offset = new Vector2(offsetX, offsetY);

            float maxSide = Mathf.Max(overallRect.size.x, overallRect.size.y);
            float diagonalSize = Mathf.Ceil(Mathf.Sqrt(maxSide * maxSide * 2));

            float rotatedGridSide = Mathf.Ceil(overallRect.center.magnitude + diagonalSize);

            for (float x = 0; x <= rotatedGridSide; x += 1)
            {
                for (float y = 0; y <= rotatedGridSide; y += 1)
                {
                    foreach (var direction in directions)
                    {
                        var point = new Vector2(x * direction.x, y * direction.y);
                        var worldPoint = (Vector2)(rotQuaternion * point) + offset;

                        foreach (var rect in rects)
                        {
                            if (rect.Contains(worldPoint))
                            {
                                worldPoints.Add(point + (Vector2)(Quaternion.Euler(0, 0, -angle) * offset));
                            }
                        }
                    }
                }
            }
            return worldPoints.ToList();
        }

#if UNITY_EDITOR
        [ContextMenu("Find all bounds")]
        public void FindAllBounds()
        {
            boundingBoxes = new List<BoundingBox>(GetComponentsInChildren<BoundingBox>());
            UnityEditor.EditorUtility.SetDirty(gameObject);
        }

        void LateUpdate()
        {
            if (Application.isPlaying)
                return;
            transform.localPosition -= new Vector3(0, transform.localPosition.y, 0);
        }

        void OnDrawGizmos()
        {
            var gizmosGridHeight = transform.position.y;

            var rawPoints = GetIntersectingPoints(transform.position, transform.rotation, false);
            
            foreach (var point in rawPoints)
            {
                GizmosUtils.DrawRectWithStartAndSize(Color.blue, point.GetX0Y() + (Vector3.up * gizmosGridHeight), Vector2.one.GetX0Y());
            }

            var passableGridPoints = GetCurrentPassablePointsOnGrid();
            var notPassableGridPoints = GetCurrentPointsOnGrid();

            GizmosUtils.DrawCells(Color.red, passableGridPoints, gizmosGridHeight + 0.2f, Vector2.one.GetX0Y());
            GizmosUtils.DrawCells(Color.green, notPassableGridPoints, gizmosGridHeight, Vector2.one.GetX0Y());
        }
#endif
    }
}
