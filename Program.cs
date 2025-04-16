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
    private Player _player;
    public int Width { get; }
    public int Height { get; }
    public int PlayerX { get; private set; }
    public int PlayerY { get; private set; }

    public GameField(int width, int height)
    {
        Width = width;
        Height = height;
        _grid = new GameObject[width, height];
    }

    public void PlaceObject(GameObject gameObject, int x, int y)
    {
        if (gameObject is Player player)
        {
            if (_player != null)
            {
                _grid[PlayerX, PlayerY] = null;
            }
            _player = player;
            PlayerX = x;
            PlayerY = y;
        }

        if (IsWithinBounds(x, y))
        {
            _grid[x, y] = gameObject;
        }
    }

    public GameObject this[int x, int y]
    {
        get => IsWithinBounds(x, y) ? _grid[x, y] : throw new IndexOutOfRangeException();
        set
        {
            if (IsWithinBounds(x, y)) _grid[x, y] = value;
            else throw new IndexOutOfRangeException();
        }
    }

    private bool IsWithinBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

    public void Display()
    {
        Console.Clear();
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                Console.Write((_grid[x, y]?.Symbol ?? '.') + " ");
            }
            Console.WriteLine();
        }
    }

    public bool TryMovePlayer(int deltaX, int deltaY)
    {
        int newX = PlayerX + deltaX;
        int newY = PlayerY + deltaY;

        if (!IsWithinBounds(newX, newY) || _grid[newX, newY] is Wall)
            return false;

        _grid[PlayerX, PlayerY] = null;
        PlayerX = newX;
        PlayerY = newY;
        _grid[PlayerX, PlayerY] = _player;

        return true;
    }
}

public static class Program
{
    public static void Main()
    {
        GameField field = new GameField(8, 8);
        field.PlaceObject(new Player(), 0, 0);
        field.PlaceObject(new Wall(), 2, 2);
        field.PlaceObject(new Prize(), 6, 5);

        bool isRunning = true;
        while (isRunning)
        {
            field.Display();
            ConsoleKeyInfo key = Console.ReadKey(true);

            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                case ConsoleKey.W:
                    field.TryMovePlayer(0, -1);
                    break;
                case ConsoleKey.DownArrow:
                case ConsoleKey.S:
                    field.TryMovePlayer(0, 1);
                    break;
                case ConsoleKey.LeftArrow:
                case ConsoleKey.A:
                    field.TryMovePlayer(-1, 0);
                    break;
                case ConsoleKey.RightArrow:
                case ConsoleKey.D:
                    field.TryMovePlayer(1, 0);
                    break;
                case ConsoleKey.Escape:
                    isRunning = false;
                    break;
            }
        }
    }
}
