using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AStarCell
{
    public int x;
    public int y;
    public float height;
    public bool isWalkable;

    public AStarCell(int x, int y, float height, bool isWalkable = true)
    {
        this.x = x;
        this.y = y;
        this.height = height;
        this.isWalkable = isWalkable;
    }
}

public class AStarPathFinding
{
    AStarCell[,] grid;
    int gridWidth;
    int gridHeight;

    float[,] gCost;         // Массив для хранения GCost
    float[,] hCost;         // Массив для хранения HCost
    AStarCell[,] parents;   // Массив для хранения ссылок на родителя
    bool[,] closedSet;      // Массив для хранения принадлежности клетки к закрытому набору

    List<AStarCell>[] neighbors; // Предкэшированные соседи для каждой клетки

    public AStarCell[,] Grid => grid;

    public AStarPathFinding(AStarCell[,] grid)
    {
        this.grid = grid;
        gridWidth = grid.GetLength(0);
        gridHeight = grid.GetLength(1);

        // Инициализируем вспомогательные массивы
        gCost = new float[gridWidth, gridHeight];
        hCost = new float[gridWidth, gridHeight];
        parents = new AStarCell[gridWidth, gridHeight];
        closedSet = new bool[gridWidth, gridHeight];

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                gCost[x, y] = float.MaxValue;
                hCost[x, y] = float.MaxValue;
                parents[x, y] = null;
                closedSet[x, y] = false;
            }
        }

        // Кэшируем список соседей для всех клеток
        CacheNeighbors();
    }

    private void CacheNeighbors()
    { 
        neighbors = new List<AStarCell>[gridWidth * gridHeight];

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                int index = x * gridHeight + y; // Одномерный индекс вместо двумерного
                neighbors[index] = new List<AStarCell>();

                int[] dx = { -1, 1, 0, 0 };
                int[] dy = { 0, 0, -1, 1 };

                for (int i = 0; i < 4; i++)
                {
                    int newX = x + dx[i];
                    int newY = y + dy[i];

                    if (newX >= 0 && newX < gridWidth && newY >= 0 && newY < gridHeight)
                    {
                        neighbors[index].Add(grid[newX, newY]);
                    }
                }
            }
        }
    }

    public List<AStarCell> FindPath(AStarCell startCell, AStarCell endCell)
    {
        // Очистка вспомогательных данных
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                gCost[x, y] = float.MaxValue;
                hCost[x, y] = float.MaxValue;
                parents[x, y] = null;
                closedSet[x, y] = false;
            }
        }

        List<AStarCell> openSet = new List<AStarCell>();

        // Инициализация начальной клетки
        gCost[startCell.x, startCell.y] = 0;
        hCost[startCell.x, startCell.y] = GetDistance(startCell, endCell);
        openSet.Add(startCell);

        while (openSet.Count > 0)
        {
            // Находим клетку с наименьшим значением FCost
            AStarCell currentCell = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                float fCost = gCost[openSet[i].x, openSet[i].y] + hCost[openSet[i].x, openSet[i].y];
                float currentFCost = gCost[currentCell.x, currentCell.y] + hCost[currentCell.x, currentCell.y];

                if (fCost < currentFCost ||
                    (fCost == currentFCost && hCost[openSet[i].x, openSet[i].y] < hCost[currentCell.x, currentCell.y]))
                {
                    currentCell = openSet[i];
                }
            }

            openSet.Remove(currentCell);
            closedSet[currentCell.x, currentCell.y] = true;

            // Если достигли конечной клетки, восстанавливаем путь
            if (currentCell == endCell)
            {
                return RetracePath(startCell, endCell);
            }

            foreach (AStarCell neighbor in neighbors[currentCell.x * gridHeight + currentCell.y])
            {
                if (!neighbor.isWalkable || closedSet[neighbor.x, neighbor.y])
                    continue;

                // Условие перепада высот
                if (Mathf.Abs(neighbor.height - currentCell.height) > 0.5f)
                    continue;

                float tentativeGCost = /*gCost[currentCell.x, currentCell.y] + GetDistance(currentCell, neighbor);*/ 0;
                if (tentativeGCost < gCost[neighbor.x, neighbor.y])
                {
                    gCost[neighbor.x, neighbor.y] = tentativeGCost;
                    hCost[neighbor.x, neighbor.y] = GetDistance(neighbor, endCell);
                    parents[neighbor.x, neighbor.y] = currentCell;

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
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
            currentCell = parents[currentCell.x, currentCell.y];
        }
        path.Add(startCell);
        path.Reverse();
        return path;
    }

    private float GetDistance(AStarCell a, AStarCell b)
    {
        // Манхэттенское расстояние
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
