using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AStarCell
{
    public int x;
    public int y;
    public float height;
    public float movementCost;

    public AStarCell(int x, int y, float height, float movementCost = 1f)
    {
        this.x = x;
        this.y = y;
        this.height = height;
        this.movementCost = movementCost;
    }
}

public class AStarPathFinding
{
    // Heuristic weight > 1 trades optimality for speed: search aims more directly at the goal,
    // expanding far fewer cells. Path may be slightly longer but is found much faster.
    private const float HeuristicWeight = 2.5f;

    private AStarCell[,] grid;
    private int gridWidth;
    private int gridHeight;

    private float[,] gCost;
    private float[,] hCost;
    private AStarCell[,] parents;
    private bool[,] closedSet;

    private List<AStarCell>[] neighbors;

    private readonly List<(float priority, AStarCell cell)> heapBuffer = new();

    // How many A* nodes to expand per frame before yielding. Higher = faster
    // completion but bigger frame spikes.
    private const int NodesPerFrame = 500;

    // Single A* instance is shared across all bots; serialize concurrent
    // coroutine calls so they don't corrupt each other's buffers.
    private bool isBusy;

    public AStarCell[,] Grid => grid;

    public AStarPathFinding(AStarCell[,] grid)
    {
        this.grid = grid;
        gridWidth = grid.GetLength(0);
        gridHeight = grid.GetLength(1);

        gCost = new float[gridWidth, gridHeight];
        hCost = new float[gridWidth, gridHeight];
        parents = new AStarCell[gridWidth, gridHeight];
        closedSet = new bool[gridWidth, gridHeight];

        CacheNeighbors();
    }

    private void CacheNeighbors()
    {
        neighbors = new List<AStarCell>[gridWidth * gridHeight];

        int[] dx = { -1, 1, 0, 0 };
        int[] dy = { 0, 0, -1, 1 };

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                int index = x * gridHeight + y;
                neighbors[index] = new List<AStarCell>();

                for (int i = 0; i < 4; i++)
                {
                    int newX = x + dx[i];
                    int newY = y + dy[i];

                    if (newX >= 0 && newX < gridWidth && newY >= 0 && newY < gridHeight)
                        neighbors[index].Add(grid[newX, newY]);
                }
            }
        }
    }

    public IEnumerator FindPath(AStarCell startCell, AStarCell endCell, List<AStarCell> outPath)
    {
        outPath.Clear();

        // Wait if another coroutine is using the shared buffers
        while (isBusy)
            yield return null;
        isBusy = true;

        try
        {
            for (int x = 0; x < gridWidth; x++)
                for (int y = 0; y < gridHeight; y++)
                {
                    gCost[x, y] = float.MaxValue;
                    hCost[x, y] = float.MaxValue;
                    parents[x, y] = null;
                    closedSet[x, y] = false;
                }

            heapBuffer.Clear();

            gCost[startCell.x, startCell.y] = 0;
            hCost[startCell.x, startCell.y] = GetDistance(startCell, endCell);
            HeapPush(startCell, hCost[startCell.x, startCell.y] * HeuristicWeight);

            int processed = 0;

            while (heapBuffer.Count > 0)
            {
                AStarCell currentCell = HeapPop();

                // Lazy deletion: node may have been re-added with a better cost
                if (closedSet[currentCell.x, currentCell.y])
                    continue;

                closedSet[currentCell.x, currentCell.y] = true;

                if (currentCell == endCell)
                {
                    RetracePath(startCell, endCell, outPath);
                    yield break;
                }

                foreach (AStarCell neighbor in neighbors[currentCell.x * gridHeight + currentCell.y])
                {
                    if (closedSet[neighbor.x, neighbor.y])
                        continue;

                    if (Mathf.Abs(neighbor.height - currentCell.height) > 0.5f)
                        continue;

                    float stepCost = GetDistance(currentCell, neighbor) * neighbor.movementCost;
                    float tentativeGCost = gCost[currentCell.x, currentCell.y] + stepCost;

                    if (tentativeGCost < gCost[neighbor.x, neighbor.y])
                    {
                        gCost[neighbor.x, neighbor.y] = tentativeGCost;
                        hCost[neighbor.x, neighbor.y] = GetDistance(neighbor, endCell);
                        parents[neighbor.x, neighbor.y] = currentCell;
                        HeapPush(neighbor, tentativeGCost + hCost[neighbor.x, neighbor.y] * HeuristicWeight);
                    }
                }

                if (++processed >= NodesPerFrame)
                {
                    processed = 0;
                    yield return null;
                }
            }
        }
        finally
        {
            isBusy = false;
        }
    }

    private void HeapPush(AStarCell cell, float priority)
    {
        heapBuffer.Add((priority, cell));
        int i = heapBuffer.Count - 1;
        while (i > 0)
        {
            int parent = (i - 1) / 2;
            if (heapBuffer[parent].priority <= heapBuffer[i].priority)
                break;
            (heapBuffer[parent], heapBuffer[i]) = (heapBuffer[i], heapBuffer[parent]);
            i = parent;
        }
    }

    private AStarCell HeapPop()
    {
        var top = heapBuffer[0].cell;
        int last = heapBuffer.Count - 1;
        heapBuffer[0] = heapBuffer[last];
        heapBuffer.RemoveAt(last);

        int i = 0;
        int n = heapBuffer.Count;
        while (true)
        {
            int left = 2 * i + 1;
            int right = 2 * i + 2;
            int smallest = i;
            if (left < n && heapBuffer[left].priority < heapBuffer[smallest].priority)
                smallest = left;
            if (right < n && heapBuffer[right].priority < heapBuffer[smallest].priority)
                smallest = right;
            if (smallest == i)
                break;
            (heapBuffer[smallest], heapBuffer[i]) = (heapBuffer[i], heapBuffer[smallest]);
            i = smallest;
        }

        return top;
    }

    private void RetracePath(AStarCell startCell, AStarCell endCell, List<AStarCell> outPath)
    {
        AStarCell currentCell = endCell;

        while (currentCell != startCell)
        {
            outPath.Add(currentCell);
            currentCell = parents[currentCell.x, currentCell.y];
        }
        outPath.Add(startCell);
        outPath.Reverse();
    }

    private float GetDistance(AStarCell a, AStarCell b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
