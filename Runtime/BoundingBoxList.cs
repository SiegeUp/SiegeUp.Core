using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SiegeUp.Core
{
    public class BoundingBoxList : MonoBehaviour
    {
        [SerializeField] List<BoundingBox> boundingBoxes;
        public IReadOnlyList<BoundingBox> BoundingBoxes => boundingBoxes;
        public BoundingBox MainBound => boundingBoxes.Count > 0 ? boundingBoxes[0] : null;

        public List<Vector2> GetLocalIntersectingPoints()
        {
            List<Rect> boundigBoxesRects = boundingBoxes.Select(x => new Rect(x.transform.localPosition.GetXZ() - x.Size.GetXZ() / 2, x.Size.GetXZ())).ToList();
            return GetLocalIntersectingPoints(boundigBoxesRects, transform.rotation.eulerAngles.y);
        }

        public List<Vector2> GetLocalIntersectingPoints(List<Rect> rects, float angle)
        {
            Vector2[] directions = new[] { new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, -1), new Vector2(-1, 1) };
            var rotQuaternion = Quaternion.Euler(0, 0, angle);
            var worldPoints = new List<Vector2>();

            var overallRect = MathUtils.GetOverallRect(rects);

            float offsetX = Mathf.Ceil(overallRect.size.x) % 2 == 0 ? 0.5f : 0;
            float offsetY = Mathf.Ceil(overallRect.size.y) % 2 == 0 ? 0.5f : 0;
            var offset = new Vector2(offsetX, offsetY);

            float maxSide = Mathf.Max(overallRect.size.x, overallRect.size.y);
            float diagonalSize = Mathf.Ceil(Mathf.Sqrt(maxSide * maxSide * 2));

            float rotatedGridSide = Mathf.Ceil(overallRect.center.magnitude + diagonalSize / 2);

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
            return worldPoints;
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

        void DrawRect(Color color, Vector3 point, Vector3 size)
        {
            var oldColor = Gizmos.color;
            Gizmos.color = color;
            Gizmos.DrawWireCube(point, size);
            Gizmos.color = oldColor;
        }

        void OnDrawGizmos()
        {
            var points = GetLocalIntersectingPoints();
            foreach (var point in points)
            {
                DrawRect(Color.blue, point.GetX0Y() + transform.position, Vector2.one.GetX0Y());
            }
        }
    }
#endif
}
