using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AStarCell
{
    public int X { get; set; }
    public int Y { get; set; }
    public float Height { get; set; }
    public bool IsWalkable { get; set; }

    public float GCost { get; set; } // Стоимость от начала
    public float HCost { get; set; } // Эвристическая стоимость до конца
    public AStarCell Parent { get; set; }

    public float FCost
    {
        get { return GCost + HCost; }
    }

    public AStarCell(int x, int y, float z, bool isWalkable = true)
    {
        X = x;
        Y = y;
        Height = z;
        IsWalkable = isWalkable;
    }

    // Метод для получения соседей (4 направления: вверх, вниз, влево, вправо)
    public List<AStarCell> GetNeighbors(AStarCell[,] grid)
    {
        List<AStarCell> neighbors = new List<AStarCell>();

        int[] dx = { -1, 1, 0, 0 };
        int[] dy = { 0, 0, -1, 1 };

        for (int i = 0; i < 4; i++)
        {
            int newX = X + dx[i];
            int newY = Y + dy[i];

            if (newX >= 0 && newX < grid.GetLength(0) && newY >= 0 && newY < grid.GetLength(1))
            {
                neighbors.Add(grid[newX, newY]);
            }
        }

        return neighbors;
    }
}

public class AStarPathFinding
{
    private AStarCell[,] grid;
    private int gridWidth;
    private int gridHeight;

    public AStarPathFinding(AStarCell[,] grid)
    {
        this.grid = grid;
        gridWidth = grid.GetLength(0);
        gridHeight = grid.GetLength(1);
    }

    public List<AStarCell> FindPath(AStarCell startCell, AStarCell endCell)
    {
        List<AStarCell> openSet = new List<AStarCell>();
        HashSet<AStarCell> closedSet = new HashSet<AStarCell>();

        openSet.Add(startCell);

        while (openSet.Count > 0)
        {
            // Выбираем клетку с наименьшим FCost
            AStarCell currentCell = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].FCost < currentCell.FCost ||
                   (openSet[i].FCost == currentCell.FCost && openSet[i].HCost < currentCell.HCost))
                {
                    currentCell = openSet[i];
                }
            }

            openSet.Remove(currentCell);
            closedSet.Add(currentCell);

            // Если достигли цели, восстанавливаем путь
            if (currentCell == endCell)
            {
                return RetracePath(startCell, endCell);
            }

            foreach (AStarCell neighbor in currentCell.GetNeighbors(grid))
            {
                if (!neighbor.IsWalkable || closedSet.Contains(neighbor))
                    continue;

                // Проверяем условие перепада высот
                if (Mathf.Abs(neighbor.Height - currentCell.Height) > 0.5f)
                    continue;

                float tentativeGCost = currentCell.GCost + GetDistance(currentCell, neighbor);

                if (tentativeGCost < neighbor.GCost || !openSet.Contains(neighbor))
                {
                    neighbor.GCost = tentativeGCost;
                    neighbor.HCost = GetDistance(neighbor, endCell);
                    neighbor.Parent = currentCell;

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        // Если путь не найден
        return new List<AStarCell>();
    }

    private List<AStarCell> RetracePath(AStarCell startCell, AStarCell endCell)
    {
        List<AStarCell> path = new List<AStarCell>();
        AStarCell currentCell = endCell;

        while (currentCell != startCell)
        {
            path.Add(currentCell);
            currentCell = currentCell.Parent;
        }
        path.Add(startCell);
        path.Reverse();
        return path;
    }

    private float GetDistance(AStarCell a, AStarCell b)
    {
        // Используем манхэттенское расстояние
        return Mathf.Abs(a.X - b.X) + Mathf.Abs(a.Y - b.Y);
    }
}
