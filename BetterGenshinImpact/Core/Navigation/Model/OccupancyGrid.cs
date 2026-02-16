using System;

namespace BetterGenshinImpact.Core.Navigation.Model;

public class OccupancyGrid
{
    public int Width { get; }
    public int Height { get; }
    private readonly byte[,] _grid;

    public enum CellType : byte
    {
        Free = 0,
        Obstacle = 1,
        Goal = 2,
        Player = 3
    }

    public OccupancyGrid(int width, int height)
    {
        Width = width;
        Height = height;
        _grid = new byte[width, height];
    }

    public void SetCell(int x, int y, CellType type)
    {
        if (x >= 0 && x < Width && y >= 0 && y < Height)
        {
            _grid[x, y] = (byte)type;
        }
    }

    public CellType GetCell(int x, int y)
    {
        if (x >= 0 && x < Width && y >= 0 && y < Height)
        {
            return (CellType)_grid[x, y];
        }
        return CellType.Obstacle;
    }

    public void Clear()
    {
        Array.Clear(_grid, 0, _grid.Length);
    }

    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }
}
