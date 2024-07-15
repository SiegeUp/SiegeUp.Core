using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SiegeUp.Core
{
    [ExecuteInEditMode]
    public class BoundingBox : MonoBehaviour
    {
        [SerializeField]
        Vector3 size = Vector3.one;

        [SerializeField]
        Vector3Int sizeInt = Vector3Int.one;

        public Vector3Int Size { get => sizeInt; set => sizeInt = value; }

        public Rect GetLocalRect()
        {
            return new Rect(transform.localPosition.GetXZ() - Size.GetXZ() / 2, Size.GetXZ());
        }

        public Rect GetRect(Quaternion rotation)
        {
            var rotMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, rotation.eulerAngles.y, 0));

            var topLeft = -size / 2;
            var bottomRight = size / 2;

            var topLeftRot = rotMatrix * (Vector3) topLeft;
            var bottomRightRot = rotMatrix * (Vector3) bottomRight;

            var newMin = new Vector2(Mathf.Min(topLeftRot.x, bottomRightRot.x), Mathf.Min(topLeftRot.z, bottomRightRot.z));
            var newMax = new Vector2(Mathf.Max(topLeftRot.x, bottomRightRot.x), Mathf.Max(topLeftRot.z, bottomRightRot.z));

            var rect = new Rect(newMin.x, newMin.y, newMax.x - newMin.x, newMax.y - newMin.y);

            return rect;
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