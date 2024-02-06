using System.Collections.Generic;
using UnityEditor;
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
    }
}