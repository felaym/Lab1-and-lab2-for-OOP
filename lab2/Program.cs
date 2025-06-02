using System;
using System.Drawing;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;
using System.Diagnostics;
using System.Linq;
using System.IO;

public abstract class GameObject
{
    public virtual Color Color { get; } = Color.Gray;
    public virtual int ZIndex { get; } = 0;
    public virtual Image? Texture { get; } = null;

    public abstract bool CanStepOn(GameField gameField);

    public abstract void OnPlayerInteraction(GameField gameField);
}

public class Player : GameObject
{
    private static Image _texture = Image.FromFile("Resources/player.png");
    public override Color Color => Color.CornflowerBlue;
    public override int ZIndex => 2;
    public override Image Texture => _texture;

    public override bool CanStepOn(GameField gameField) => true;

    public override void OnPlayerInteraction(GameField gameField)
    {
    }
}

public class Finish : GameObject
{
    private static Image _texture = Image.FromFile("Resources/finish.png");
    public override Color Color => Color.CornflowerBlue;
    public override int ZIndex => 10;
    public override Image Texture => _texture;

    public override bool CanStepOn(GameField gameField)
    {
        return gameField.Score >= gameField.GetTotalPrizes();
    }

    public override void OnPlayerInteraction(GameField gameField)
    {
        if (CanStepOn(gameField))
        {
            gameField.OnGameFinished();
        }
    }
}

public class Wall : GameObject
{
    private static Image _texture = Image.FromFile("Resources/wall.png");
    public override Color Color => Color.DimGray;
    public override int ZIndex => 1;
    public override Image Texture => _texture;

    public override bool CanStepOn(GameField gameField) => false;

    public override void OnPlayerInteraction(GameField gameField)
    {
    }
}

public class Prize : GameObject
{
    private static Image _texture = Image.FromFile("Resources/star.png");
    public override Color Color => Color.Gold;
    public override int ZIndex => 1;
    public override Image Texture => _texture;

    public override bool CanStepOn(GameField gameField) => true;

    public override void OnPlayerInteraction(GameField gameField)
    {
        gameField.CollectPrize(this);
    }
}

public class GameField
{
    public event Action<int> ScoreChanged = delegate { };
    public event EventHandler GameFinished = delegate { };

    private readonly List<GameObject>[,] _objects;
    private Player _player = null!;
    private int _totalPrizes = 0;

    public int Width { get; }
    public int Height { get; }
    public int CellSize { get; set; } = 40;
    public int PlayerX { get; private set; }
    public int PlayerY { get; private set; }

    private Stopwatch _gameTimer;

    private int _score;
    public int Score
    {
        get => _score;
        private set
        {
            _score = value;
            ScoreChanged?.Invoke(_score);
        }
    }

    public GameField(int width, int height)
    {
        Width = width;
        Height = height;
        _objects = new List<GameObject>[width, height];

        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                _objects[x, y] = new List<GameObject>();

        InitializeWalls();

        _gameTimer = new Stopwatch();
    }

    public TimeSpan GetGameTime() => _gameTimer.Elapsed;

    public int GetTotalPrizes() => _totalPrizes;

    public void OnGameFinished()
    {
        GameFinished?.Invoke(this, EventArgs.Empty);
    }

    public void CollectPrize(Prize prize)
    {
        RemoveObject(prize, PlayerX, PlayerY);
        Score++;
    }

    private void InitializeWalls()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (x == 0 || x == Width - 1 || y == 0 || y == Height - 1)
                {
                    PlaceObject(new Wall(), x, y);
                }
            }
        }
    }

    public void PlaceObject(GameObject gameObject, int x, int y)
    {
        if (!IsWithinBounds(x, y)) return;

        if (gameObject is Player player)
        {
            if (_player != null)
                RemoveObject(_player, PlayerX, PlayerY);

            _player = player;
            PlayerX = x;
            PlayerY = y;
        }

        if (gameObject is Prize)
        {
            _totalPrizes++;
        }

        _objects[x, y].Add(gameObject);
        _objects[x, y].Sort((a, b) => a.ZIndex.CompareTo(b.ZIndex));
    }

    public void RemoveObject(GameObject obj, int x, int y)
    {
        if (IsWithinBounds(x, y))
            _objects[x, y].Remove(obj);
    }

    public GameObject? GetTopObject(int x, int y)
    {
        if (!IsWithinBounds(x, y)) return null;
        if (_objects[x, y].Count == 0) return null;

        return _objects[x, y][^1];
    }

    public bool IsWithinBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

    public bool TryMovePlayer(int deltaX, int deltaY)
    {
        if (!_gameTimer.IsRunning)
        {
            _gameTimer.Start();
        }

        int newX = PlayerX + deltaX;
        int newY = PlayerY + deltaY;

        if (!IsWithinBounds(newX, newY)) return false;

        var objectsAtCell = _objects[newX, newY].ToList();

        foreach (var obj in objectsAtCell)
        {
            if (!obj.CanStepOn(this))
                return false;
        }

        RemoveObject(_player, PlayerX, PlayerY);
        PlayerX = newX;
        PlayerY = newY;
        PlaceObject(_player, PlayerX, PlayerY);

        foreach (var obj in objectsAtCell)
        {
            obj.OnPlayerInteraction(this);
        }

        return true;
    }

    public IEnumerable<GameObject> GetObjectsAt(int x, int y)
    {
        if (IsWithinBounds(x, y))
            return _objects[x, y];
        return Enumerable.Empty<GameObject>();
    }
}

public partial class MainForm : Form
{
    private GameField _gameField = null!;
    private Bitmap _buffer = null!;
    private readonly Timer _gameTimer;

    private Label lblScore = null!;

    public MainForm()
    {
        InitializeComponent();
        DoubleBuffered = true;
        KeyPreview = true;
        BackColor = Color.FromArgb(30, 30, 30);

        lblScore = new Label();
        Controls.Add(lblScore);

        _gameField = new GameField(10, 10);
        _gameTimer = new Timer { Interval = 16 };
        _gameTimer.Tick += (s, e) => Invalidate();

        InitializeGame();
        SetupUI();
        _gameTimer.Start();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        _buffer = new Bitmap(ClientSize.Width, ClientSize.Height);
        Invalidate();
    }

    private void SaveGameResult()
    {
        string fileName = "game_results.txt";
        string result = $"{DateTime.Now}: Зібрано {_gameField.Score} призів за {_gameField.GetGameTime():mm\\:ss}";

        try
        {
            File.AppendAllText(fileName, result + Environment.NewLine);
            MessageBox.Show($"Результат гри збережено до {fileName}", "Гру завершено",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Помилка при збереженні результатів гри: {ex.Message}", "Помилка",
            MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void InitializeGame()
    {
        _gameField.PlaceObject(new Player(), 1, 1);

        _gameField.PlaceObject(new Finish(), _gameField.Width - 2, _gameField.Height - 2);

        _gameField.GameFinished += (s, e) =>
        {
            _gameTimer.Stop();
            SaveGameResult();
            MessageBox.Show($"Вітаю! Ви завершили гру за {_gameField.GetGameTime():mm\\:ss}!",
            "Перемога!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        };

        for (int x = 2; x < _gameField.Width - 2; x += 2)
        {
            _gameField.PlaceObject(new Wall(), x, 2);
            _gameField.PlaceObject(new Wall(), x, _gameField.Height - 3);
        }

        for (int i = 3; i < 7; i++)
        {
            _gameField.PlaceObject(new Wall(), i, i);
        }

        Random rand = new Random();
        for (int i = 0; i < 10; i++)
        {
            int x, y;
            do
            {
                x = rand.Next(1, _gameField.Width - 1);
                y = rand.Next(1, _gameField.Height - 1);
            }
            while (_gameField.GetTopObject(x, y) != null);

            _gameField.PlaceObject(new Prize(), x, y);
        }
    }

    private void SetupUI()
    {
        lblScore.ForeColor = Color.Gold;
        lblScore.Font = new Font("Consolas", 14, FontStyle.Bold);
        lblScore.BackColor = Color.Transparent;
        lblScore.Text = "SCORE: 0";

        _gameField.ScoreChanged += score =>
        {
            lblScore.Text = $"SCORE: {score} / {_gameField.GetTotalPrizes()}";
        };
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (KeyBindings.MovementKeys.TryGetValue(e.KeyCode, out var move))
        {
            _gameField.TryMovePlayer(move.dx, move.dy);
        }
        else if (e.KeyCode == Keys.Escape)
        {
            Close();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (_buffer == null)
            return;

        using (var g = Graphics.FromImage(_buffer))
        {
            g.Clear(BackColor);
            DrawGrid(g);
        }

        e.Graphics.DrawImage(_buffer, ClientRectangle);
    }

    private void DrawGrid(Graphics g)
    {
        int cellSize = Math.Min(ClientSize.Width / _gameField.Width, ClientSize.Height / _gameField.Height);

        for (int x = 0; x < _gameField.Width; x++)
        {
            for (int y = 0; y < _gameField.Height; y++)
            {
                var rect = new Rectangle(x * cellSize, y * cellSize, cellSize, cellSize);
                using (var brush = new SolidBrush(Color.FromArgb(50, 50, 50)))
                    g.FillRectangle(brush, rect);

                foreach (var obj in _gameField.GetObjectsAt(x, y).OrderBy(o => o.ZIndex))
                {
                    if (obj.Texture != null)
                        g.DrawImage(obj.Texture, rect);
                    else
                        using (var brush = new SolidBrush(obj.Color))
                            g.FillRectangle(brush, rect);

                    using (var pen = new Pen(Color.FromArgb(70, 70, 70), 1))
                        g.DrawRectangle(pen, rect);
                }
            }
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        _buffer = new Bitmap(ClientSize.Width, ClientSize.Height);
        Invalidate();
    }

    #region Windows Form Designer generated code
    private void InitializeComponent()
    {
        this.lblScore = new Label();
        this.SuspendLayout();

        this.lblScore.AutoSize = true;
        this.lblScore.ForeColor = Color.Gold;
        this.lblScore.Location = new Point(10, 10);
        this.lblScore.Font = new Font("Consolas", 14, FontStyle.Bold);
        this.lblScore.BackColor = Color.Transparent;

        this.ClientSize = new Size(500, 500);
        this.Controls.Add(this.lblScore);
        this.Name = "MainForm";
        this.ResumeLayout(false);
        this.PerformLayout();
    }
    #endregion
}

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }
}
