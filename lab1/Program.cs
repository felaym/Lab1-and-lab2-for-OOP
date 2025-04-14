using System;

public abstract class GameObject
{
    public abstract char Symbol { get; }
}

public class Player : GameObject
{
    public override char Symbol => 'P';
}

public class Wall : GameObject
{
    public override char Symbol => '#';
}

public class Prize : GameObject
{
    public override char Symbol => '*';
}

public class GameField
{
    private readonly GameObject[,] _grid;
    public int Width { get; }
    public int Height { get; }

    public GameField(int width, int height)
    {
        Width = width;
        Height = height;
        _grid = new GameObject[width, height];
    }

    public void PlaceObject(GameObject gameObject, int x, int y)
    {
        if (IsWithinBounds(x, y))
        {
            _grid[x, y] = gameObject;
        }
    }

    public GameObject this[int x, int y]
    {
        get
        {
            if (!IsWithinBounds(x, y))
                throw new IndexOutOfRangeException("Координати виходять за межі поля.");
            return _grid[x, y];
        }
        set
        {
            if (IsWithinBounds(x, y))
                _grid[x, y] = value;
            else
                throw new IndexOutOfRangeException("Координати виходять за межі поля.");
        }
    }

    private bool IsWithinBounds(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }

    public void Display()
    {
        Console.Clear();
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                char symbol = _grid[x, y]?.Symbol ?? '.';
                Console.Write(symbol + " ");
            }
            Console.WriteLine();
        }
    }
}

// public class Program
// {
//     public static void Main()
//     {
//         GameField field = new GameField(8, 8);
//         field.PlaceObject(new Player(), 0, 0);
//         field.PlaceObject(new Wall(), 2, 2);
//         field.PlaceObject(new Prize(), 6, 5);
//         field.Display();
//     }
// }
