using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SiegeUp.Core
{
    [ExecuteInEditMode]
    public class BoundingBox : MonoBehaviour
    {
        [SerializeField]
        Vector3Int size = Vector3Int.one;

        public Vector3Int Size { get => size; set => size = value; }

        public Rect GetLocalRect()
        {
            return new Rect(transform.localPosition.GetXZ() - ((Vector2)size.GetXZ()) / 2, (Vector2) size.GetXZ());
        }

        public Rect GetRect(Quaternion rotation)
        {
            var rotMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, rotation.eulerAngles.y, 0));

            var topLeft = - (Vector3)size / 2;
            var bottomRight = (Vector3)size / 2;

            var topLeftRot = rotMatrix * topLeft;
            var bottomRightRot = rotMatrix * bottomRight;

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