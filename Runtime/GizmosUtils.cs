using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GizmosUtils
{
    public static void DrawRectWithStartAndSize(Color color, Vector3 point, Vector3 size)
    {
        var oldColor = Gizmos.color;
        Gizmos.color = color;
        var rectCenterPosition = new Vector3(point.x + 0.5f, point.y, point.z + 0.5f);
        Gizmos.DrawWireCube(rectCenterPosition, size);

        Gizmos.color = oldColor;
    }

    public static void DrawRectWithCenterAndSize(Color color, Vector3 point, Vector3 size)
    {
        var oldColor = Gizmos.color;
        Gizmos.color = color;
        Gizmos.DrawWireCube(point, size);
        Gizmos.color = oldColor;
    }

    public static void DrawCells(Color color, List<Vector2Int> points, float height, Vector3 size)
    {
        foreach (var point in points)
            DrawCell(color, point, height, size);
    }

    public static void DrawCell(Color color, Vector2Int point, float height, Vector3 size)
    {
        var oldColor = Gizmos.color;
        Gizmos.color = color;

        var cellCenter = new Vector2(point.x + 0.5f, point.y + 0.5f);
        var cellCenterPosition = new Vector3(cellCenter.x, height, cellCenter.y);
        Gizmos.DrawWireCube(cellCenterPosition, size);

        Gizmos.color = oldColor;
    }
}
