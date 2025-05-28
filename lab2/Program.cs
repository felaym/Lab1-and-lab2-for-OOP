using System;
using System.Drawing;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

public abstract class GameObject
{
    public virtual Color Color { get; } = Color.Gray;
    public virtual int ZIndex { get; } = 0;
    public virtual string Name { get; protected set; } = "GameObject";
    public virtual Image? Texture { get; } = null;
}

public class Player : GameObject
{
    private static Image _texture = Image.FromFile("Resources/player.png");
    public override Color Color => Color.CornflowerBlue;
    public override int ZIndex => 2;
    public override string Name => "Player";
    public override Image Texture => _texture;
}

public class Wall : GameObject
{
    private static Image _texture = Image.FromFile("Resources/wall.png");
    public override Color Color => Color.DimGray;
    public override int ZIndex => 1;
    public override string Name => "Wall";
    public override Image Texture => _texture;
}

public class Prize : GameObject
{
    private static Image _texture = Image.FromFile("Resources/star.png");
    public override Color Color => Color.Gold;
    public override int ZIndex => 1;
    public override string Name => "Prize";
    public override Image Texture => _texture;
}

public class GameField
{
    public event EventHandler Updated = delegate { };
    public event EventHandler ScoreChanged = delegate { };

    private readonly GameObject[,,] _layers;
    private Player _player = null!;

    public int Width { get; }
    public int Height { get; }
    public int CellSize { get; set; } = 40;
    public int PlayerX { get; private set; }
    public int PlayerY { get; private set; }

    private int _score;
    public int Score
    {
        get => _score;
        private set
        {
            _score = value;
            ScoreChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public GameField(int width, int height)
    {
        Width = width;
        Height = height;
        _layers = new GameObject[width, height, 3];

        InitializeWalls();
    }

    private void InitializeWalls()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (x == 0 || x == Width - 1 || y == 0 || y == Height - 1)
                {
                    PlaceObject(new Wall(), x, y, 1);
                }
            }
        }
    }

    public void PlaceObject(GameObject gameObject, int x, int y, int layer = 1)
    {
        if (gameObject is Player player)
        {
            if (_player != null)
            {
                _layers[PlayerX, PlayerY, 2] = null;
            }
            _player = player;
            PlayerX = x;
            PlayerY = y;
            layer = 2;
        }

        if (IsWithinBounds(x, y))
        {
            _layers[x, y, layer] = gameObject;
            Updated?.Invoke(this, EventArgs.Empty);
        }
    }

    public GameObject? GetTopObject(int x, int y)
    {
        for (int layer = 2; layer >= 0; layer--)
        {
            var obj = _layers[x, y, layer];
            if (obj != null) return obj;
        }
        return null;
    }

    public bool IsWithinBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

    public bool TryMovePlayer(int deltaX, int deltaY)
    {
        int newX = PlayerX + deltaX;
        int newY = PlayerY + deltaY;

        if (!IsWithinBounds(newX, newY)) return false;
        if (GetTopObject(newX, newY) is Wall) return false;

        GameObject? topObj = GetTopObject(newX, newY);
        if (topObj is Prize)
        {
            Score++;
            for (int layer = 0; layer < 3; layer++)
            {
                if (_layers[newX, newY, layer] is Prize)
                {
                    _layers[newX, newY, layer] = null;
                    break;
                }
            }
        }

        _layers[PlayerX, PlayerY, 2] = null;
        PlayerX = newX;
        PlayerY = newY;
        _layers[PlayerX, PlayerY, 2] = _player;

        Updated?.Invoke(this, EventArgs.Empty);
        return true;
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

    private void InitializeGame()
    {
        _gameField.PlaceObject(new Player(), 1, 1);

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

        _gameField.ScoreChanged += (s, e) =>
        {
            lblScore.Text = $"SCORE: {_gameField.Score}";
        };
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        switch (e.KeyCode)
        {
            case Keys.W:
            case Keys.Up:
                _gameField.TryMovePlayer(0, -1);
                break;
            case Keys.S:
            case Keys.Down:
                _gameField.TryMovePlayer(0, 1);
                break;
            case Keys.A:
            case Keys.Left:
                _gameField.TryMovePlayer(-1, 0);
                break;
            case Keys.D:
            case Keys.Right:
                _gameField.TryMovePlayer(1, 0);
                break;
            case Keys.Escape:
                Close();
                break;
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

                var obj = _gameField.GetTopObject(x, y);
                if (obj != null)
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
        SuspendLayout();

        this.lblScore.AutoSize = true;
        this.lblScore.Location = new Point(10, 10);
        this.lblScore.TabIndex = 0;

        ClientSize = new Size(500, 500);
        Controls.Add(this.lblScore);
        Name = "MainForm";
        ResumeLayout(false);
        PerformLayout();
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
