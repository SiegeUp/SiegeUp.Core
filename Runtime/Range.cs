using System.Collections.Generic;
using UnityEngine;

namespace SiegeUp.Core
{
    [ComponentId(64)]
    public class Range : MonoBehaviour
    {
        public enum RangeShapeType
        {
            Sphere,
            Box,
            Circle
        }

        [AutoSerialize(1), QuickEdit, SerializeField]
        RangeShapeType rangeType = RangeShapeType.Sphere;

        [AutoSerialize(2), QuickEdit, SerializeField]
        float radius;

        [AutoSerialize(3), QuickEdit, SerializeField]
        Bounds bounds;

        [AutoSerialize(4), QuickEdit, SerializeField]
        bool global;

        Vector3 positionCache;
        int cacheFrame;
        public RangeShapeType RangeType => rangeType;
        public float Radius { get => radius; set => radius = value; }
        public Bounds Bounds { get => bounds; set => bounds = value; }
        public bool Global => global;

        public bool IsPointInRangeOptimized(Vector3 point, int currentFrame)
        {
            // Optimization to avoid call transform.position in UnitDetectorModel
            if (cacheFrame != currentFrame)
            {
                positionCache = transform.position;
                cacheFrame = currentFrame;
            }

            switch (rangeType)
            {
                case RangeShapeType.Sphere:
                    return MathUtils.IsPointInRange(point, positionCache, radius);
                case RangeShapeType.Circle:
                    return MathUtils.IsPointInRange2D(point, positionCache, radius);
                case RangeShapeType.Box:
                    return bounds.Contains(transform.worldToLocalMatrix * (point - positionCache));
            }

            return false;
        }

        public bool IsPointInRange(Vector3 point)
        {
            switch (rangeType)
            {
                case RangeShapeType.Sphere:
                    return MathUtils.IsPointInRange(point, transform.position, radius);
                case RangeShapeType.Circle:
                    return MathUtils.IsPointInRange2D(point, transform.position, radius);
                case RangeShapeType.Box:
                    return bounds.Contains(transform.worldToLocalMatrix * (point - transform.position));
            }

            return false;
        }

        public bool IsRectInRange(Vector3 corner, Vector3 size)
        {
            const float posCompensation = 0.1f;
            switch (rangeType)
            {
                case RangeShapeType.Sphere:
                    return MathUtils.IsRectInRange3D(corner, size, transform.position, radius + posCompensation);
                case RangeShapeType.Circle:
                    return MathUtils.IsRectInRange2D(corner, size, transform.position, radius + posCompensation);
                case RangeShapeType.Box:
                    throw new System.Exception("Box with box contact is not implemented"); // the problem is that both boxes can be rotated
            }

            return false;
        }

        public bool IsObjectInRange(GameObject obj)
        {
            var building = obj.GetComponent<BoxCollider>();
            if (building)
            {
                var boundingBox = gameObject.GetComponent<BoundingBoxList>() is {} list ? list.MainBound : null;
                if (boundingBox != null)
                {
                    var objPos = obj.transform.position;
                    var rect = MathUtils.GetRectFromBoxWorld(obj.transform.position, obj.transform.rotation, boundingBox);
                    var corner = new Vector3(rect.min.x, objPos.y, rect.min.y);
                    var size = new Vector3(rect.size.x, objPos.y, rect.size.y);
                    bool result = IsRectInRange(corner, size);
                    return result;
                }
            }

            return IsPointInRange(obj.transform.position);
        }
    }
}