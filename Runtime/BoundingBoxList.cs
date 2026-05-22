using System.Collections.Generic;
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
        [SerializeField] bool only90DegreesRotation = false;
        [SerializeField] bool noRotationMirroringInSymmetryMode = false;

        public BoundingBox MainBound => mainBoundingBox;
        public bool Only90DegreesRotation => only90DegreesRotation;
        public bool NoRotationMirroringInSymmetryMode => noRotationMirroringInSymmetryMode;

        public List<Vector2Int> GetCurrentPassablePointsOnGrid()
        {
            return GetPassablePointsOnGrid(transform.position, transform.rotation);
        }

        public List<Vector2Int> GetPassablePointsOnGrid(Vector3 position, Quaternion rotation)
        {
            var rawPoints = GetPassableIntersectingPoints(position, rotation);
            var result = new List<Vector2Int>(rawPoints.Count);

            for (int i = 0; i < rawPoints.Count; i++)
            {
                result.Add(MathUtils.RoundToVector2Int(rawPoints[i]));
            }
            return result;
        }

        List<Vector2> GetPassableIntersectingPoints(Vector3 position, Quaternion rotation)
        {
            var result = new List<Vector2>();
            var posXZ = position.GetXZ();
            float angle = rotation.eulerAngles.y;

            foreach (var passableBoundingBox in passableBoundingBoxes)
            {
                var localPoints = GetIntersectingPoints(passableBoundingBox.GetLocalRect(), angle);
                for (int i = 0; i < localPoints.Count; i++)
                {
                    result.Add(localPoints[i] + posXZ);
                }
            }
            return result;
        }

        public List<Vector2Int> GetMainBoundCurrentPointsOnGrid()
        {
            return GetMainBoundPointsOnGrid(transform.position, transform.rotation);
        }

        public List<Vector2Int> GetMainBoundPointsOnGrid(Vector3 position, Quaternion rotation)
        {
            var rawPoints = GetMainBoundIntersectingPoints(position, rotation);
            var result = new List<Vector2Int>(rawPoints.Count);

            for (int i = 0; i < rawPoints.Count; i++)
            {
                result.Add(MathUtils.RoundToVector2Int(rawPoints[i]));
            }
            return result;
        }

        List<Vector2> GetMainBoundIntersectingPoints(Vector3 position, Quaternion rotation)
        {
            var localPoints = GetIntersectingPoints(mainBoundingBox.GetLocalRect(), rotation.eulerAngles.y);
            var posXZ = position.GetXZ();

            for (int i = 0; i < localPoints.Count; i++)
            {
                localPoints[i] += posXZ;
            }
            return localPoints;
        }

        public List<Vector2> GetIntersectingPoints(Rect rect, float angle)
        {
            if (angle == 0)
                return GetIntersectingPointsNoRotation(rect);

            var rotQuaternion = Quaternion.Euler(0, 0, angle);
            var inverseRotQuaternion = Quaternion.Euler(0, 0, -angle);

            var worldPoints = new List<Vector2>();

            float offsetX = Mathf.Ceil(rect.size.x) % 2 == 0 ? 0.5f : 0;
            float offsetY = Mathf.Ceil(rect.size.y) % 2 == 0 ? 0.5f : 0;
            var offset = new Vector2(offsetX, offsetY);

            var rotatedOffset = (Vector2)(inverseRotQuaternion * offset);

            float maxSide = Mathf.Max(rect.size.x, rect.size.y);
            float diagonalSize = Mathf.Ceil(Mathf.Sqrt(maxSide * maxSide * 2));

            for (float x = 0; x <= diagonalSize; x += 1)
            {
                for (float y = 0; y <= diagonalSize; y += 1)
                {
                    foreach (var direction in MathUtils.inCorners)
                    {
                        var point = new Vector2(x * direction.x, y * direction.y);
                        var worldPoint = (Vector2)(rotQuaternion * point) + offset;

                        if (rect.Contains(worldPoint))
                        {
                            var newPoint = point + rotatedOffset - new Vector2(0.5f, 0.5f);
                            if (!worldPoints.Contains(newPoint))
                                worldPoints.Add(newPoint);
                        }
                    }
                }
            }
            return worldPoints;
        }

        private List<Vector2> GetIntersectingPointsNoRotation(Rect rect)
        {
            var worldPoints = new List<Vector2>();

            float offsetX = Mathf.Ceil(rect.size.x) % 2 == 0 ? 0.5f : 0f;
            float offsetY = Mathf.Ceil(rect.size.y) % 2 == 0 ? 0.5f : 0f;
            var offset = new Vector2(offsetX, offsetY);
            var shift = offset - new Vector2(0.5f, 0.5f);

            int minX = Mathf.FloorToInt(rect.xMin - offset.x);
            int maxX = Mathf.CeilToInt(rect.xMax - offset.x);
            int minY = Mathf.FloorToInt(rect.yMin - offset.y);
            int maxY = Mathf.CeilToInt(rect.yMax - offset.y);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    var worldPoint = new Vector2(x, y) + offset;

                    if (!rect.Contains(worldPoint))
                        continue;

                    worldPoints.Add(new Vector2(x, y) + shift);
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
            {
                Vector2 size = Vector2.one;
                Vector3 upHeight = Vector3.up * gizmosGridHeight;
                foreach (var point in rawPoints)
                    GizmosUtils.DrawRectWithStartAndSize(Color.blue, point.GetX0Y() + upHeight, size.GetX0Y());
            }

            if (showMainBoundPassablePointsOnGridGizmos)
                GizmosUtils.DrawCells(Color.red, passableGridPoints, gizmosGridHeight + 0.2f, Vector2.one.GetX0Y());

            if (showMainBoundPointsOnGridGizmos)
                GizmosUtils.DrawCells(Color.green, notPassableGridPoints, gizmosGridHeight, Vector2.one.GetX0Y());
        }
    }
}