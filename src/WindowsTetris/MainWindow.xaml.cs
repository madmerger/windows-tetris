using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using WindowsTetris.Audio;
using WindowsTetris.Game;

namespace WindowsTetris;

public partial class MainWindow : Window
{
    private const int CellSize = 28;
    private const int NextCell = 24;

    private static readonly SolidColorBrush[] CellBrushes = BuildBrushes();

    private readonly SoundManager _sound = new();
    private readonly DispatcherTimer _gravity = new(DispatcherPriority.Render);
    private readonly Stopwatch _clock = new();

    private GameEngine? _engine;
    private bool _paused;
    private bool _countingDown;

    public MainWindow()
    {
        InitializeComponent();
        _gravity.Tick += OnGravityTick;
        KeyDown += OnKeyDown;
    }

    private static SolidColorBrush[] BuildBrushes()
    {
        SolidColorBrush B(string hex)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            brush.Freeze();
            return brush;
        }

        // Indexed by CellType (0 unused/empty handled separately).
        return new[]
        {
            B("#161C2B"), // 0 empty (not used for fills)
            B("#22D3EE"), // 1 I  cyan
            B("#FACC15"), // 2 O  yellow
            B("#C084FC"), // 3 T  purple
            B("#4ADE80"), // 4 S  green
            B("#F87171"), // 5 Z  red
            B("#60A5FA"), // 6 J  blue
            B("#FB923C"), // 7 L  orange
            B("#64748B"), // 8 wall slate
            B("#FFC83D"), // 9 gem gold
        };
    }

    // ----- Game lifecycle -----------------------------------------------------

    private void StartGame(GameMode mode)
    {
        _engine = new GameEngine(mode);
        _paused = false;
        _countingDown = false;

        ModeText.Text = mode == GameMode.Stage ? "STAGE MODE" : "INFINITE MODE";
        StagePanel.Visibility = mode == GameMode.Stage ? Visibility.Visible : Visibility.Collapsed;

        MenuOverlay.Visibility = Visibility.Collapsed;
        MessageOverlay.Visibility = Visibility.Collapsed;

        _clock.Restart();
        _sound.PlayEffect("start");
        _sound.StartBgm();

        RestartGravity();
        Render();
        Focus();
    }

    private void RestartGravity()
    {
        if (_engine is null) return;
        double ms = Math.Max(50, 800 * Math.Pow(0.85, _engine.Level - 1));
        _gravity.Interval = TimeSpan.FromMilliseconds(ms);
        _gravity.Start();
    }

    private void OnGravityTick(object? sender, EventArgs e)
    {
        if (_engine is null || _paused || _countingDown) return;
        var result = _engine.Tick();
        HandleResult(result);
        Render();
    }

    private void HandleResult(LockResult? result)
    {
        if (_engine is null) return;
        RestartGravity(); // keep interval in sync with level

        if (result is not { } r) return;

        if (r.LinesCleared > 0) _sound.PlayEffect("blip");

        if (r.GameOver)
        {
            EndGame();
        }
        else if (r.StageClear)
        {
            BeginStageClear();
        }
    }

    private void EndGame()
    {
        _gravity.Stop();
        _clock.Stop();
        _sound.StopBgm();
        ShowMessage("GAME OVER", "Press R to play again");
    }

    private async void BeginStageClear()
    {
        _gravity.Stop();
        _countingDown = true;
        _sound.StopBgm();
        _sound.PlayEffect("clear");
        Render();

        // The user can restart (R) or open the menu mid-countdown. Both clear
        // _countingDown (and restart swaps _engine), so bail out if either
        // happens across an await to avoid mutating an abandoned/new game.
        var expected = _engine;

        ShowMessage("STAGE CLEAR", "Get ready...");
        await Task.Delay(900);
        if (!StillCounting(expected)) return;

        for (int n = 3; n >= 1; n--)
        {
            MessageText.Text = n.ToString();
            MessageSub.Text = string.Empty;
            await Task.Delay(600);
            if (!StillCounting(expected)) return;
        }

        expected!.AdvanceStage();
        MessageOverlay.Visibility = Visibility.Collapsed;
        _countingDown = false;
        _sound.StartBgm();
        RestartGravity();
        Render();
    }

    private bool StillCounting(GameEngine? expected) =>
        _countingDown && ReferenceEquals(_engine, expected);

    private void ShowMessage(string title, string sub)
    {
        MessageText.Text = title;
        MessageSub.Text = sub;
        MessageOverlay.Visibility = Visibility.Visible;
    }

    private void TogglePause()
    {
        if (_engine is null || _countingDown) return;
        if (_engine.State != GameState.Playing) return;

        _paused = !_paused;
        if (_paused)
        {
            _gravity.Stop();
            _clock.Stop();
            _sound.PauseBgm();
            ShowMessage("PAUSED", "Press P to resume");
            PauseButton.Content = "Resume (P)";
        }
        else
        {
            MessageOverlay.Visibility = Visibility.Collapsed;
            _clock.Start();
            _sound.ResumeBgm();
            RestartGravity();
            PauseButton.Content = "Pause (P)";
            Focus();
        }
    }

    // ----- Input --------------------------------------------------------------

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (MenuOverlay.Visibility == Visibility.Visible)
        {
            if (e.Key == Key.Enter) OnStartInfinite(this, e);
            return;
        }

        switch (e.Key)
        {
            case Key.P:
                TogglePause();
                e.Handled = true;
                return;
            case Key.M:
                ToggleMute();
                e.Handled = true;
                return;
            case Key.R:
                if (_engine is not null) StartGame(_engine.Mode);
                e.Handled = true;
                return;
        }

        if (_engine is null || _paused || _countingDown ||
            _engine.State != GameState.Playing)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Left:
                _engine.MoveLeft();
                break;
            case Key.Right:
                _engine.MoveRight();
                break;
            case Key.Up:
            case Key.X:
                _engine.Rotate();
                break;
            case Key.Down:
                HandleResult(_engine.SoftDrop());
                break;
            case Key.Space:
                HandleResult(_engine.HardDrop());
                break;
            default:
                return;
        }

        e.Handled = true;
        Render();
    }

    private void ToggleMute()
    {
        _sound.SetMuted(!_sound.IsMuted);
        MuteButton.Content = _sound.IsMuted ? "Unmute (M)" : "Mute (M)";
    }

    // ----- Button handlers ----------------------------------------------------

    private void OnStartStage(object sender, RoutedEventArgs e) => StartGame(GameMode.Stage);
    private void OnStartInfinite(object sender, RoutedEventArgs e) => StartGame(GameMode.Infinite);
    private void OnPauseClick(object sender, RoutedEventArgs e) => TogglePause();
    private void OnMuteClick(object sender, RoutedEventArgs e) => ToggleMute();

    private void OnRestartClick(object sender, RoutedEventArgs e)
    {
        if (_engine is not null) StartGame(_engine.Mode);
    }

    private void OnMenuClick(object sender, RoutedEventArgs e)
    {
        _gravity.Stop();
        _clock.Stop();
        _sound.StopBgm();
        _paused = false;
        _countingDown = false;
        PauseButton.Content = "Pause (P)";
        MessageOverlay.Visibility = Visibility.Collapsed;
        MenuOverlay.Visibility = Visibility.Visible;
    }

    // ----- Rendering ----------------------------------------------------------

    private void Render()
    {
        if (_engine is null) return;

        UpdateStats();
        DrawBoard();
        DrawNext();
    }

    private void UpdateStats()
    {
        if (_engine is null) return;
        ScoreText.Text = _engine.Score.ToString("N0");
        LevelText.Text = _engine.Level.ToString();
        LinesText.Text = _engine.Lines.ToString();
        StageText.Text = (_engine.CurrentStage + 1).ToString();
        GemsText.Text = _engine.GemsRemaining.ToString();

        var t = _clock.Elapsed;
        TimeText.Text = $"{(int)t.TotalMinutes:00}:{t.Seconds:00}";
    }

    private void DrawBoard()
    {
        if (_engine is null) return;
        BoardCanvas.Children.Clear();

        // Locked cells + empty background grid.
        for (int r = 0; r < GameEngine.Height; r++)
        {
            for (int c = 0; c < GameEngine.Width; c++)
            {
                int v = _engine.CellAt(r, c);
                if (v == CellType.Empty)
                {
                    AddCell(BoardCanvas, c, r, CellSize, (Brush)Resources["EmptyCellBrush"], filled: false);
                }
                else
                {
                    AddCell(BoardCanvas, c, r, CellSize, CellBrushes[v], filled: true, gem: v == CellType.Gem);
                }
            }
        }

        var piece = _engine.Current;

        // Ghost piece.
        if (_engine.State == GameState.Playing)
        {
            int ghostRow = _engine.GhostRow();
            int n = piece.Shape.Size;
            for (int r = 0; r < n; r++)
                for (int c = 0; c < n; c++)
                    if (piece.Cells[r, c] != 0)
                        AddGhost(BoardCanvas, piece.Col + c, ghostRow + r, CellSize, CellBrushes[piece.Color]);
        }

        // Active piece.
        {
            int n = piece.Shape.Size;
            for (int r = 0; r < n; r++)
                for (int c = 0; c < n; c++)
                    if (piece.Cells[r, c] != 0 && piece.Row + r >= 0)
                        AddCell(BoardCanvas, piece.Col + c, piece.Row + r, CellSize,
                            CellBrushes[piece.Color], filled: true);
        }
    }

    private void DrawNext()
    {
        if (_engine is null) return;
        NextCanvas.Children.Clear();

        var shape = Tetromino.All[_engine.NextIndex];
        var cells = shape.Rotations[0];
        int n = shape.Size;

        // Centre the piece's filled bounding box within the preview canvas.
        int minR = n, minC = n, maxR = -1, maxC = -1;
        for (int r = 0; r < n; r++)
            for (int c = 0; c < n; c++)
                if (cells[r, c] != 0)
                {
                    minR = Math.Min(minR, r); maxR = Math.Max(maxR, r);
                    minC = Math.Min(minC, c); maxC = Math.Max(maxC, c);
                }

        double pieceW = (maxC - minC + 1) * NextCell;
        double pieceH = (maxR - minR + 1) * NextCell;
        double offX = (NextCanvas.Width - pieceW) / 2;
        double offY = (NextCanvas.Height - pieceH) / 2;

        for (int r = minR; r <= maxR; r++)
            for (int c = minC; c <= maxC; c++)
                if (cells[r, c] != 0)
                {
                    double x = offX + (c - minC) * NextCell;
                    double y = offY + (r - minR) * NextCell;
                    AddRect(NextCanvas, x, y, NextCell, CellBrushes[shape.Color], filled: true);
                }
    }

    private void AddCell(Canvas canvas, int col, int row, int size, Brush brush,
        bool filled, bool gem = false)
    {
        AddRect(canvas, col * size, row * size, size, brush, filled);
        if (gem)
        {
            double cx = col * size + size / 2.0;
            double cy = row * size + size / 2.0;
            double rad = size * 0.22;
            var diamond = new Polygon
            {
                Points = new PointCollection
                {
                    new Point(cx, cy - rad),
                    new Point(cx + rad, cy),
                    new Point(cx, cy + rad),
                    new Point(cx - rad, cy),
                },
                Fill = Brushes.White,
                Opacity = 0.85,
            };
            canvas.Children.Add(diamond);
        }
    }

    private static void AddRect(Canvas canvas, double x, double y, int size, Brush brush, bool filled)
    {
        var rect = new Rectangle
        {
            Width = size - 2,
            Height = size - 2,
            RadiusX = 5,
            RadiusY = 5,
            Fill = brush,
        };
        if (filled)
        {
            rect.Stroke = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));
            rect.StrokeThickness = 1;
        }
        Canvas.SetLeft(rect, x + 1);
        Canvas.SetTop(rect, y + 1);
        canvas.Children.Add(rect);
    }

    private static void AddGhost(Canvas canvas, int col, int row, int size, SolidColorBrush colorBrush)
    {
        var rect = new Rectangle
        {
            Width = size - 2,
            Height = size - 2,
            RadiusX = 5,
            RadiusY = 5,
            Fill = Brushes.Transparent,
            Stroke = colorBrush,
            StrokeThickness = 2,
            Opacity = 0.35,
        };
        Canvas.SetLeft(rect, col * size + 1);
        Canvas.SetTop(rect, row * size + 1);
        canvas.Children.Add(rect);
    }
}
