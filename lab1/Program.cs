using System;
using System.IO;
using System.Diagnostics;

public abstract class GameObject
{
    public abstract char Symbol { get; }
}

public class EmptyCell : GameObject
{
    public override char Symbol => '.';
}

public class Player : GameObject
{
    public override char Symbol => 'P';

    public int X { get; set; }
    public int Y { get; set; }
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
    protected readonly GameObject[,] _grid;
    protected Player _player;
    public int Width { get; }
    public int Height { get; }
    public int Score { get; private set; }
    public int TotalPrizes { get; private set; }
    private Stopwatch _gameTimer;

    public GameField(int width, int height)
    {
        Width = width;
        Height = height;
        _grid = new GameObject[width, height];
        _gameTimer = new Stopwatch();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                {
                    _grid[x, y] = new Wall();
                }
                else
                {
                    _grid[x, y] = new EmptyCell();
                }
            }
        }
    }

    public void PlaceObject(GameObject gameObject, int x, int y)
    {
        if (gameObject is Player player)
        {
            if (_player != null)
            {
                _grid[_player.X, _player.Y] = new EmptyCell();
            }
            _player = player;
            _player.X = x;
            _player.Y = y;
            _gameTimer.Start();
        }

        if (gameObject is Prize)
        {
            TotalPrizes++;
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

    protected bool IsWithinBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

    public void Display()
    {
        Console.Clear();
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                Console.Write(_grid[x, y].Symbol + " ");
            }
            Console.WriteLine();
        }
        Console.WriteLine($"Score: {Score}/{TotalPrizes}");
        Console.WriteLine($"Time: {_gameTimer.Elapsed:mm\\:ss}");
    }

    public bool TryMovePlayer(int deltaX, int deltaY)
    {
        int newX = _player.X + deltaX;
        int newY = _player.Y + deltaY;

        if (!IsWithinBounds(newX, newY)) return false;
        if (_grid[newX, newY] is Wall) return false;

        if (_grid[newX, newY] is Prize)
        {
            Score++;
        }

        _grid[_player.X, _player.Y] = new EmptyCell();
        _player.X = newX;
        _player.Y = newY;
        _grid[_player.X, _player.Y] = _player;

        if (Score >= TotalPrizes)
        {
            _gameTimer.Stop();
            SaveGameResult();
            Console.WriteLine("Вітаю! Ви зібрали всі призи:)!");
            Console.WriteLine($"У вас це зайняло: {_gameTimer.Elapsed:mm\\:ss}");
            Console.WriteLine("Натисніть esc...");
            Console.ReadKey();
            Environment.Exit(0);
        }

        return true;
    }

    private void SaveGameResult()
    {
        string fileName = "game_results.txt";
        string result = $"{DateTime.Now}: Зібрав {Score} призів за {_gameTimer.Elapsed:mm\\:ss}";

        try
        {
            File.AppendAllText(fileName, result + Environment.NewLine);
            Console.WriteLine($"Результат гри збережено до {fileName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Помилка при збереженні результатів гри: {ex.Message}");
        }
    }
}

public static class Program
{
    public static void Main()
    {
        GameField field = new GameField(10, 10);

        field.PlaceObject(new Player(), 1, 1);

        field.PlaceObject(new Wall(), 2, 2);
        field.PlaceObject(new Wall(), 3, 3);
        field.PlaceObject(new Wall(), 4, 4);
        field.PlaceObject(new Wall(), 5, 5);
        field.PlaceObject(new Wall(), 7, 3);
        field.PlaceObject(new Wall(), 6, 6);
        field.PlaceObject(new Wall(), 2, 7);
        field.PlaceObject(new Wall(), 5, 2);

        field.PlaceObject(new Prize(), 6, 5);
        field.PlaceObject(new Prize(), 3, 4);
        field.PlaceObject(new Prize(), 2, 6);
        field.PlaceObject(new Prize(), 8, 8);
        field.PlaceObject(new Prize(), 4, 7);
        field.PlaceObject(new Prize(), 1, 8);

        bool isRunning = true;
        while (isRunning)
        {
            field.Display();
            ConsoleKeyInfo key = Console.ReadKey(true);

            if (KeyBindings.MovementKeys.TryGetValue(key.Key, out var move))
            {
                field.TryMovePlayer(move.dx, move.dy);
            }
            else if (key.Key == ConsoleKey.Escape)
            {
                isRunning = false;
            }
        }
    }
}
