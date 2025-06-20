﻿using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using System.Linq;
using System;

namespace SiegeUp.Core
{
    public static class MathUtils
    {
        public static float[] angles = { 0, 90, 180, 270 };

        public static Vector2Int[] allSides = { new(0, 1), new(1, 0), new(0, -1), new(-1, 0), new(1, 1), new(1, -1), new(-1, -1), new(-1, 1) };
        public static Vector2Int[] fourSides = { new(0, 1), new(1, 0), new(0, -1), new(-1, 0) };
        public static Vector2Int[] inCorners = { new(1, 1), new(1, -1), new(-1, -1), new(-1, 1) };

        public enum CompareOperation
        {
            More = 1,
            MoreOrEqual = 2,
            Less = 3,
            LessOrEqual = 4,
            Equal = 5,
            NotEqual = 6
        }

        public enum LogicalOperation
        {
            And = 1,
            Or = 2
        }

        public static byte[] GetHash(byte[] bytes)
        {
            using (HashAlgorithm algorithm = SHA256.Create())
                return algorithm.ComputeHash(bytes);
        }

        public static string GetHashString(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in GetHash(bytes))
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }

        static byte[] GetHash(string inputString)
        {
            using (HashAlgorithm algorithm = SHA256.Create())
                return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        }

        public static string GetHashString(string inputString)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in GetHash(inputString))
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }

        public static bool IsPointInRange(Vector3 point, Vector3 center, float range)
        {
            // optimization. substract each component fater then vector
            float diffX = point.x - center.x;
            float diffY = point.y - center.y;
            float diffZ = point.z - center.z;
            var squareDist = diffX * diffX + diffY * diffY + diffZ * diffZ;
            float rangeVal = range;
            return squareDist < rangeVal * rangeVal;
        }

        public static bool IsPointInRange2D(Vector3 point, Vector3 center, float range)
        {
            var diff = point - center;
            var squareDist = diff.x * diff.x + diff.z * diff.z;
            float rangeVal = range;
            return squareDist < rangeVal * rangeVal;
        }

        public static bool IsRectInRange3D(Vector3 corner, Vector3 size, Vector3 pos, float range)
        {
            float deltaX = pos.x - Mathf.Max(corner.x, Mathf.Min(pos.x, corner.x + size.x));
            float deltaY = pos.y - Mathf.Max(corner.y, Mathf.Min(pos.y, corner.y + size.y));
            float deltaZ = pos.z - Mathf.Max(corner.z, Mathf.Min(pos.z, corner.z + size.z));
            bool result = (deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ) < (range * range);
            return result;
        }

        public static bool IsRectInRange2D(Vector2 corner, Vector2 size, Vector2 pos, float range)
        {
            float deltaX = pos.x - Mathf.Max(corner.x, Mathf.Min(pos.x, corner.x + size.x));
            float deltaY = pos.y - Mathf.Max(corner.y, Mathf.Min(pos.y, corner.y + size.y));
            bool result = (deltaX * deltaX + deltaY * deltaY) < (range * range);
            return result;
        }

        public static bool IsRectInRange2D(Vector3 corner, Vector3 size, Vector3 pos, float range)
        {
            return IsRectInRange2D(new Vector2(corner.x, corner.z), new Vector2(size.x, size.z), new Vector2(pos.x, pos.z), range);
        }

        public static RectInt GetRectIntFromBoxWorld(Vector3 position, Quaternion rotation, BoundingBox boundingBox)
        {
            var rect = GetRectFromBoxWorld(position, rotation, boundingBox);    

            var rectInt = new RectInt(
                Mathf.FloorToInt(rect.x),
                Mathf.FloorToInt(rect.y),
                Mathf.CeilToInt(rect.width),
                Mathf.CeilToInt(rect.height));

            return rectInt;
        }

        public static Rect GetRectFromBoxWorld(Vector3 position, Quaternion rotation, BoundingBox boundingBox)
        {
            var rectLocal = boundingBox.GetRect(rotation);

            var globalPos = boundingBox.transform.localPosition + position;

            var rect = new Rect(
                globalPos.x + rectLocal.x,
                globalPos.z + rectLocal.y,
                rectLocal.width,
                rectLocal.height);

            return rect;
        }

        public static float DistancePointToRectangle(Vector2 point, Rect rect)
        {
            if (rect.size.x == 0 && rect.size.y == 0)
                return Vector2.Distance(point, rect.center);

            //  Calculate a distance between a point and a rectangle.
            //  The area around/in the rectangle is defined in terms of
            //  several regions:
            //
            //  O--x
            //  |
            //  y
            //
            //
            //        I   |    II    |  III
            //      ======+==========+======   --yMin
            //       VIII |  IX (in) |  IV
            //      ======+==========+======   --yMax
            //       VII  |    VI    |   V
            //
            //
            //  Note that the +y direction is down because of Unity's GUI coordinates.

            if (point.x < rect.xMin)
            {
                // Region I, VIII, or VII
                if (point.y < rect.yMin)
                {
                    // I
                    Vector2 diff = point - new Vector2(rect.xMin, rect.yMin);
                    return diff.magnitude;
                }

                if (point.y > rect.yMax)
                {
                    // VII
                    Vector2 diff = point - new Vector2(rect.xMin, rect.yMax);
                    return diff.magnitude;
                } // VIII

                return rect.xMin - point.x;
            }

            if (point.x > rect.xMax)
            {
                // Region III, IV, or V
                if (point.y < rect.yMin)
                {
                    // III
                    Vector2 diff = point - new Vector2(rect.xMax, rect.yMin);
                    return diff.magnitude;
                }

                if (point.y > rect.yMax)
                {
                    // V
                    Vector2 diff = point - new Vector2(rect.xMax, rect.yMax);
                    return diff.magnitude;
                } // IV

                return point.x - rect.xMax;
            } // Region II, IX, or VI

            if (point.y < rect.yMin)
            {
                // II
                return rect.yMin - point.y;
            }

            if (point.y > rect.yMax)
            {
                // VI
                return point.y - rect.yMax;
            } // IX

            return 0f;
        }

        public static bool Compare(CompareOperation operation, float current, float target)
        {
            if (operation == CompareOperation.More && current > target)
                return true;
            if (operation == CompareOperation.MoreOrEqual && current >= target)
                return true;
            if (operation == CompareOperation.Less && current < target)
                return true;
            if (operation == CompareOperation.LessOrEqual && current <= target)
                return true;
            if (operation == CompareOperation.Equal && current == target)
                return true;
            if (operation == CompareOperation.NotEqual && current != target)
                return true;
            return false;
        }

        public static bool IsInfinity(Vector3 pos)
        {
            return float.IsInfinity(pos.x) || float.IsInfinity(pos.y) || float.IsInfinity(pos.z);
        }

        public static Vector3Int RoundToVector3Int(Vector3 point)
        {
            return new Vector3Int(RoundFloatToInt(point.x), RoundFloatToInt(point.y), RoundFloatToInt(point.z));
        }

        public static Vector2Int RoundToVector2Int(Vector2 point)
        {
            return new Vector2Int(RoundFloatToInt(point.x), RoundFloatToInt(point.y));
        }

        public static Vector2Int GetMiddlePoint(List<Vector2Int> points)
        {
            if (points == null || points.Count == 0)
                return Vector2Int.zero;

            int sumX = 0;
            int sumY = 0;
            foreach (var point in points)
            {
                sumX += point.x;
                sumY += point.y;
            }

            int averageX = sumX / points.Count;
            int averageY = sumY / points.Count;

            return new Vector2Int(averageX, averageY);
        }

        public static Vector3 GetMiddlePoint(IEnumerable<Vector3> points)
        {
            int count = points.Count();
            if (count == 0)
                return Vector3.zero;

            float invCount = 1f / count;

            float sumX = 0f;
            float sumY = 0f;
            float sumZ = 0f;

            for (int i = 0; i < count; i++)
            {
                Vector3 v = points.ElementAt(i);
                sumX += v.x;
                sumY += v.y;
                sumZ += v.z;
            }

            return new Vector3(sumX * invCount, sumY * invCount, sumZ * invCount);
        }

        public static List<Vector2Int> GetPointsAroundPoint(Vector2Int point)
        {
            return GetPointsAroundPoints(new List<Vector2Int> { point });
        }

        public static List<Vector2Int> GetPointsAroundPoints(List<Vector2Int> points)
        {
            var pointsAround = new HashSet<Vector2Int>();
            var pointsSet = new HashSet<Vector2Int>(points);

            Vector2Int[] allSides = { new(1, 1), new(1, -1), new(-1, -1), new(-1, 1), new(0, 1), new(1, 0), new(0, -1), new(-1, 0) };

            foreach (var point in points)
            {
                foreach (var side in allSides)
                {
                    var pointToCheck = point + side;

                    if (!pointsSet.Contains(pointToCheck))
                        pointsAround.Add(pointToCheck);
                }
            }

            return pointsAround.ToList();
        }

        public static int RoundFloatToInt(float number)
        {
            double adjustment = 0.000000001;
            return (int)Math.Round(number + adjustment);
        }

        public static List<Vector2> GetPointsAroundBox(Rect rect, float step, float distance)
        {
            if (step <= 0f)
                throw new ArgumentException("Step must be greater than zero", nameof(step));
            if (distance < 0f)
                throw new ArgumentException("Distance cannot be negative", nameof(distance));

            var points = new List<Vector2>();

            int layers = Mathf.CeilToInt(distance / step);
            for (int layer = 1; layer <= layers; layer++)
            {
                float layerDist = layer * step;
                if (layerDist > distance)
                    layerDist = distance;

                float xMin = rect.xMin - layerDist;
                float xMax = rect.xMax + layerDist;
                float yMin = rect.yMin - layerDist;
                float yMax = rect.yMax + layerDist;

                for (float x = xMin; x <= xMax; x += step)
                    points.Add(new Vector2(x, yMin));
                for (float y = yMin + step; y <= yMax; y += step)
                    points.Add(new Vector2(xMax, y));
                for (float x = xMax - step; x >= xMin; x -= step)
                    points.Add(new Vector2(x, yMax));
                for (float y = yMax - step; y > yMin; y -= step)
                    points.Add(new Vector2(xMin, y));
            }

            float distanceOffset = distance <= 1f ? 0.5f : 0;
            points = points.Where(i => IsRectInRange2D(rect.position, rect.size, i, distance + distanceOffset)).ToList();

            foreach (var p in points)
            {
                Debug.DrawLine(p.GetX0Y(), p.GetX0Y() + Vector3.up * 4f, Color.green, 10f);
            }
            
            return points;
        }

        public static List<Vector3> SortPointsByDistanceToPoint(List<Vector3> points, Vector3 point, float maxDist = float.MaxValue, int numberOfClosestPoints = int.MaxValue)
        {
            float maxDistSquared = maxDist * maxDist;

            return points
                .Where(p => Vector3.SqrMagnitude(p - point) <= maxDistSquared) 
                .OrderBy(p => Vector3.SqrMagnitude(p - point))              
                .Take(numberOfClosestPoints)                                 
                .ToList();                                                  
        }

        public static float GetNearestDivisible(float num, float divisor)
        {
            if (divisor == 0)
                throw new ArgumentException("Divisor can't be 0!");

            float lower = num - (num % divisor);
            float upper = lower + (num % divisor == 0 ? 0 : divisor);

            return Math.Abs(num - lower) <= Math.Abs(num - upper) ? lower : upper;
        }

        public static float GetNearestNumberWithFraction(float target, float fraction)
        {
            float floor = Mathf.Floor(target) + fraction;
            float ceil = Mathf.Ceil(target) + fraction;

            return Math.Abs(target - floor) <= Math.Abs(target - ceil) ? floor : ceil;
        }

        public static Vector3 GetRayIntersectionWithPlane(Ray ray, Vector3 planeNormal, Vector3 planePoint)
        {
            float denominator = Vector3.Dot(planeNormal, ray.direction);
            if (Mathf.Abs(denominator) < Mathf.Epsilon)
            {
                return Vector3.zero;
            }

            float t = Vector3.Dot(planePoint - ray.origin, planeNormal) / denominator;
            return ray.origin + t * ray.direction;
        }

        public static Vector2Int ClampVector(Vector2Int value, Vector2Int min, Vector2Int max)
        {
            return new Vector2Int(
                Mathf.Clamp(value.x, min.x, max.x),
                Mathf.Clamp(value.y, min.y, max.y)
            );
        }

        public static Vector3 ClampVector(Vector3 value, Vector3 min, Vector3 max)
        {
            return new Vector3(
                Mathf.Clamp(value.x, min.x, max.x),
                Mathf.Clamp(value.y, min.y, max.y),
                Mathf.Clamp(value.z, min.z, max.z)
            );
        }

        public static RectInt GetSubrect(int x, int y, int subRectSideLength, int originRectSizeX, int originrectSizeY)
        {
            return new RectInt(x * subRectSideLength,
                               y * subRectSideLength,
                               Mathf.Min(originRectSizeX - x * subRectSideLength, subRectSideLength),
                               Mathf.Min(originrectSizeY - y * subRectSideLength, subRectSideLength));
        }

        public static RectInt GetBoundingRect(List<Vector2Int> points, int padding)
        {
            if (padding < 0)
                padding = 0;

            if (points == null || points.Count == 0)
            {
                return new RectInt(0, 0, 0, 0);
            }

            int minX = points[0].x;
            int minY = points[0].y;
            int maxX = points[0].x;
            int maxY = points[0].y;

            foreach (var p in points)
            {
                if (p.x < minX) minX = p.x;
                if (p.y < minY) minY = p.y;
                if (p.x > maxX) maxX = p.x;
                if (p.y > maxY) maxY = p.y;
            }

            int rawWidth = maxX - minX;
            int rawHeight = maxY - minY;

            int x = minX - padding;
            int y = minY - padding;

            int width = rawWidth + 2 * padding;
            int height = rawHeight + 2 * padding;

            return new RectInt(x, y, width, height);
        }

        public static RectInt ClampRect(RectInt rect, RectInt bounds)
        {
            int xMin = Mathf.Max(rect.xMin, bounds.xMin);
            int yMin = Mathf.Max(rect.yMin, bounds.yMin);
            int xMax = Mathf.Min(rect.xMax, bounds.xMax);
            int yMax = Mathf.Min(rect.yMax, bounds.yMax);
            int width = Mathf.Max(0, xMax - xMin);
            int height = Mathf.Max(0, yMax - yMin);
            return new RectInt(xMin, yMin, width, height);
        }
    }
}
