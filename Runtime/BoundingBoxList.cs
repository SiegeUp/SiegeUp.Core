using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SiegeUp.Core
{
    public class BoundingBoxList : MonoBehaviour
    {
        [SerializeField] BoundingBox mainBoundingBox;
        [SerializeField] List<BoundingBox> passableBoundingBoxes;

        [SerializeField] bool showMainBoundIntersectionPointsGizmos = true;
        [SerializeField] bool showMainBoundPointsOnGridGizmos = true;
        [SerializeField] bool showMainBoundPassablePointsOnGridGizmos = true;

        public BoundingBox MainBound => mainBoundingBox;

        public List<Vector2Int> GetCurrentPassablePointsOnGrid()
        {
            var passablePointsOnGrid = GetPassablePointsOnGrid(transform.position, transform.rotation);
            return passablePointsOnGrid.Select(x => MathUtils.RoundToVector2Int(x)).ToList();
        }

        public List<Vector2Int> GetPassablePointsOnGrid(Vector3 position, Quaternion rotation)
        {
            var passableIntersectingPoints = GetPassableIntersectingPoints(position, rotation);
            return passableIntersectingPoints.Select(x => MathUtils.RoundToVector2Int(x)).ToList();
        }

        List<Vector2> GetPassableIntersectingPoints(Vector3 position, Quaternion rotation)
        {
            List<Vector2> passableLocalIntersectingPoints = new List<Vector2>();
            foreach (var passableBoundingBox in passableBoundingBoxes)
            {
                passableLocalIntersectingPoints.AddRange(GetIntersectingPoints(passableBoundingBox.GetLocalRect(), rotation.eulerAngles.y));
            }
            var passableIntersectingPoints = passableLocalIntersectingPoints.Select(x => x + position.GetXZ()).ToList();
            return passableIntersectingPoints;
        }
        

        public List<Vector2Int> GetMainBoundCurrentPointsOnGrid()
        {
            return GetMainBoundPointsOnGrid(transform.position, transform.rotation);
        }

        public List<Vector2Int> GetMainBoundPointsOnGrid(Vector3 position, Quaternion rotation)
        {
            var mainBoundIntersectingPoints = GetMainBoundIntersectingPoints(position, rotation);
            return mainBoundIntersectingPoints.Select(x => MathUtils.RoundToVector2Int(x)).ToList();
        }

        List<Vector2> GetMainBoundIntersectingPoints(Vector3 position, Quaternion rotation)
        {
            var localMainBoundIntersectingPoints = GetIntersectingPoints(mainBoundingBox.GetLocalRect(), rotation.eulerAngles.y);
            var mainBoundintersectingPoints = localMainBoundIntersectingPoints.Select(x => x + position.GetXZ()).ToList();
            return mainBoundintersectingPoints;
        }
        
        public List<Vector2> GetIntersectingPoints(Rect rect, float angle)
        {
            Vector2[] directions = new[] { new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, -1), new Vector2(-1, 1) };
            var rotQuaternion = Quaternion.Euler(0, 0, angle);
            var worldPoints = new List<Vector2>();

            float offsetX = Mathf.Ceil(rect.size.x) % 2 == 0 ? 0.5f : 0;
            float offsetY = Mathf.Ceil(rect.size.y) % 2 == 0 ? 0.5f : 0;
            var offset = new Vector2(offsetX, offsetY);
            Debug.Log("Current offset: " + offsetX + " : " + offsetY);
            float maxSide = Mathf.Max(rect.size.x, rect.size.y);
            float diagonalSize = Mathf.Ceil(Mathf.Sqrt(maxSide * maxSide * 2));
            for (float x = 0; x <= diagonalSize; x += 1)
            {
                for (float y = 0; y <= diagonalSize; y += 1)
                {
                    foreach (var direction in directions)
                    {
                        var point = new Vector2(x * direction.x, y * direction.y);

                        var worldPoint = (Vector2)(rotQuaternion * point) + offset;
                        if (rect.Contains(worldPoint))
                            worldPoints.Add(point + (Vector2)(Quaternion.Euler(0, 0, -angle) * offset) - new Vector2(0.5f, 0.5f));
                    }
                }
            }
            return worldPoints;
        }

#if UNITY_EDITOR
        [ContextMenu("Set first found in children bound as main")]
        public void SetFirstFoundInChildrenBoundAsMain()
        {
            mainBoundingBox = GetComponentInChildren<BoundingBox>();
            UnityEditor.EditorUtility.SetDirty(gameObject);
        }
#endif
        void LateUpdate()
        {
            if (Application.isPlaying)
                return;
            transform.localPosition -= new Vector3(0, transform.localPosition.y, 0);
        }

        void OnDrawGizmos()
        {
            var gizmosGridHeight = transform.position.y;
            var rawPoints = GetMainBoundIntersectingPoints(transform.position, transform.rotation);
            var passableGridPoints = GetCurrentPassablePointsOnGrid();
            var notPassableGridPoints = GetMainBoundCurrentPointsOnGrid();

            if (showMainBoundIntersectionPointsGizmos)
                foreach (var point in rawPoints)
                    GizmosUtils.DrawRectWithStartAndSize(Color.blue, point.GetX0Y() + (Vector3.up * gizmosGridHeight), Vector2.one.GetX0Y());

            if (showMainBoundPassablePointsOnGridGizmos)
                GizmosUtils.DrawCells(Color.red, passableGridPoints, gizmosGridHeight + 0.2f, Vector2.one.GetX0Y());

            if (showMainBoundPointsOnGridGizmos)
                GizmosUtils.DrawCells(Color.green, notPassableGridPoints, gizmosGridHeight, Vector2.one.GetX0Y());
        }

        public List<Vector2> GetLocalIntersectingPointsLegacy(List<Rect> rects, float angle)
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
    }
}
