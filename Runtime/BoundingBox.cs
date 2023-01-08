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
                    var oldColor = Gizmos.color;
                    Gizmos.color = color;
                    Gizmos.DrawWireCube(start + offset, new Vector3(rotatedSnapStep.x, 0, rotatedSnapStep.z));
                    Gizmos.color = oldColor;
                }
            }
        }

        void OnDrawGizmos()
        {
            if (transform.parent)
                DrawWire(transform.parent.position, Color.green, 1);
        }
#endif
    }
}