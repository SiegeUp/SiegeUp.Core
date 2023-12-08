using System.Collections.Generic;
using UnityEngine;

namespace SiegeUp.Core
{
    [ExecuteInEditMode]
    public class BoundingBox : MonoBehaviour
    {
        [SerializeField]
        bool passable = false;

        [SerializeField]
        Vector3 size = Vector3.one;

        public bool Passable => passable;
        public Vector3 Size { get => size; set => size = value; }

        public Vector3 SizeSnapped(float snapStep)
        {
            var tmpRawSize = RawSize();
            var actualSize = new Vector3(SnapValue(tmpRawSize.x, snapStep), 0, SnapValue(tmpRawSize.z, snapStep));
            return actualSize;
        }

        public Rect GetRect(Quaternion rotation)
        {
            var rotMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, rotation.eulerAngles.y, 0));

            var size = RawSize();
            var topLeft = -size / 2;
            var bottomRight = size / 2;

            var topLeftRot = rotMatrix * topLeft;
            var bottomRightRot = rotMatrix * bottomRight;

            var newMin = new Vector2(Mathf.Min(topLeftRot.x, bottomRightRot.x), Mathf.Min(topLeftRot.z, bottomRightRot.z));
            var newMax = new Vector2(Mathf.Max(topLeftRot.x, bottomRightRot.x), Mathf.Max(topLeftRot.z, bottomRightRot.z));

            var rect = new Rect(newMin.x, newMin.y, newMax.x - newMin.x, newMax.y - newMin.y);

            return rect;
        }

        public Vector3 RawSize()
        {
            var result = size;
            result.Scale(transform.lossyScale);
            return result;
        }

        public Vector3 Position()
        {
            var result = transform.localPosition;
            result.y = 0;
            return result;
        }

        float SnapValue(float value, float snapStep)
        {
            return Mathf.Ceil(value / snapStep) * snapStep;
        }
        
        public List<Vector2> GetIntersectingPoints2(Rect rect, float angle)
        {
            Vector2[] directions = new[] { new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, -1), new Vector2(-1, 1) };
            var rotQuaternion = Quaternion.Euler(0, 0, angle);
            var worldPoints = new List<Vector2>();

            float offsetX = Mathf.Ceil(rect.size.x) % 2 == 0 ? 0.5f : 0;
            float offsetY = Mathf.Ceil(rect.size.y) % 2 == 0 ? 0.5f : 0;
            var offset = new Vector2(offsetX, offsetY);

            float maxSide = Mathf.Max(rect.size.x, rect.size.y);
            float diagonalSize = Mathf.Ceil(Mathf.Sqrt(maxSide * maxSide * 2));
            for (float x = 0; x <= diagonalSize; x += 1)
            {
                for (float y = 0; y <= diagonalSize; y += 1)
                {
                    foreach (var direction in directions)
                    {
                        var point = new Vector2(x * direction.x, y * direction.y);
                        var worldPoint = rect.center + (Vector2)(rotQuaternion * point) + offset;

                        if (rect.Contains(worldPoint))
                            worldPoints.Add(point + rect.center + (Vector2)(Quaternion.Euler(0, 0, -angle) * offset));
                    }
                }
            }
            return worldPoints;
        }

#if UNITY_EDITOR
        void LateUpdate()
        {
            if (Application.isPlaying)
                return;
            transform.localPosition -= new Vector3(0, transform.localPosition.y, 0);
        }

        public void DrawWire(Vector3 rootPosition, Color color, float snapStep)
        {
            var actualSize = SizeSnapped(snapStep);

            var rotMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0));


            var boxYOffset = new Vector3(0, transform.localPosition.y, 0);
            var rotatedSize = rotMatrix * actualSize;
            Vector3 rotatedOffset = new Vector3(rotatedSize.x / 2, 0, rotatedSize.z / 2);

            Vector3 rotatedPosition = rotMatrix * Position();

            Vector3 rotatedSnapStep = rotMatrix * new Vector3(snapStep, 0, snapStep);

            Vector3 start = rootPosition + rotatedPosition - rotatedOffset + rotatedSnapStep * 0.5f - boxYOffset;
            int xNum = Mathf.CeilToInt(Mathf.Abs(rotatedSize.x) / snapStep - 0.1f);
            int zNum = Mathf.CeilToInt(Mathf.Abs(rotatedSize.z) / snapStep - 0.1f);

            for (int i = 0; i < xNum; i++)
            {
                for (int j = 0; j < zNum; j++)
                {
                    var offset = new Vector3(rotatedSnapStep.x * i, 0, rotatedSnapStep.z * j);
                    DrawRect(color, start + offset, new Vector3(rotatedSnapStep.x, 0, rotatedSnapStep.z));
                }
            }
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
            //if (transform.parent)
            //    DrawWire(transform.parent.position, Color.green, 1);
            
            var points = GetIntersectingPoints2(new Rect(transform.position.GetXZ() - size.GetXZ() / 2, size.GetXZ()), transform.rotation.eulerAngles.y);
            foreach (var point in points)
            {
                DrawRect(Color.blue, point.GetX0Y(), Vector2.one.GetX0Y());
            }
        }
#endif
    }
}