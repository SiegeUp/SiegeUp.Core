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
        public enum CompareOperation
        {
            More = 1,
            MoreOrEqual = 2,
            Less = 3,
            LessOrEqual = 4,
            Equal = 5,
            NotEqual = 6
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

        public static Rect GetOverallRect(List<Rect> rects)
        {
            if (rects == null || rects.Count == 0)
            {
                return new Rect(); // Возвращает пустой Rect, если список пуст
            }

            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;

            foreach (Rect rect in rects)
            {
                if (rect.xMin < minX) minX = rect.xMin;
                if (rect.yMin < minY) minY = rect.yMin;
                if (rect.xMax > maxX) maxX = rect.xMax;
                if (rect.yMax > maxY) maxY = rect.yMax;
            }

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        public static bool IsPointInCylinderSector(Vector3 point, Vector3 center, float radius, float height, float sectorAngle)
        {
            // Перемещение точки и центра цилиндра так, чтобы центр цилиндра оказался в начале координат
            Vector3 relativePoint = point - center;

            // Проверка высоты
            if (relativePoint.y < 0 || relativePoint.y > height)
                return false;

            // Проверка радиуса
            float distanceSquared = relativePoint.x * relativePoint.x + relativePoint.z * relativePoint.z;
            if (distanceSquared > radius * radius)
                return false;

            // Проверка угла сектора
            float angle = Mathf.Atan2(relativePoint.z, relativePoint.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360;

            return angle <= sectorAngle;
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

        public static RectInt GetRectFromBoxWorld(Vector3 position, Quaternion rotation, BoundingBox boundingBox)
        {
            var rectLocal = boundingBox.GetRect(rotation);

            var globalPos = boundingBox.transform.localPosition + position;

            var rect = new RectInt(
                Mathf.FloorToInt(globalPos.x + rectLocal.x),
                Mathf.FloorToInt(globalPos.z + rectLocal.y),
                Mathf.CeilToInt(rectLocal.width),
                Mathf.CeilToInt(rectLocal.height));

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

        public static int RoundFloatToInt(float number)
        {
            double adjustment = 0.000000001;
            return (int)Math.Round(number + adjustment);
        }
    }
}
