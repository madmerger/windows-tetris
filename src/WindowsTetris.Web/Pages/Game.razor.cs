using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using WindowsTetris.Game;

namespace WindowsTetris.Web.Pages;

public partial class Game : ComponentBase, IDisposable
{
    private const int CellSize = 28;
    private const int NextCell = 24;

    [Inject] private IJSRuntime JS { get; set; } = default!;

    private ElementReference _containerRef;
    private GameEngine? _engine;
    private System.Timers.Timer? _gravityTimer;
    private readonly Stopwatch _clock = new();
    private System.Timers.Timer? _clockTimer;

    private bool _paused;
    private bool _countingDown;
    private bool _muted;
    private bool _audioInitialized;

    // UI state
    private bool _showMenu = true;
    private bool _showMessage;
    private bool _showStagePanel;
    private string _modeText = "INFINITE MODE";
    private string _scoreText = "0";
    private string _levelText = "1";
    private string _linesText = "0";
    private string _timeText = "00:00";
    private string _stageText = "1";
    private string _gemsText = "0";
    private string _messageTitle = "";
    private string _messageSub = "";
    private string _pauseLabel = "Pause (P)";
    private string _muteLabel = "Mute (M)";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JS.InvokeVoidAsync("TetrisCanvas.initCanvas", "board-canvas", CellSize);
            await JS.InvokeVoidAsync("TetrisCanvas.initCanvas", "next-canvas", NextCell);
            await _containerRef.FocusAsync();
        }
    }

    private async Task EnsureAudio()
    {
        if (_audioInitialized) return;
        _audioInitialized = true;
        await JS.InvokeVoidAsync("TetrisAudio.init");
    }

    private async void StartGame(GameMode mode)
    {
        await EnsureAudio();

        _engine = new GameEngine(mode);
        _paused = false;
        _countingDown = false;

        _modeText = mode == GameMode.Stage ? "STAGE MODE" : "INFINITE MODE";
        _showStagePanel = mode == GameMode.Stage;
        _showMenu = false;
        _showMessage = false;
        _pauseLabel = "Pause (P)";

        _clock.Restart();
        StartClockTimer();

        await JS.InvokeVoidAsync("TetrisAudio.playEffect", "start");
        await JS.InvokeVoidAsync("TetrisAudio.playBgm");

        RestartGravity();
        await Render();
        StateHasChanged();
        await _containerRef.FocusAsync();
    }

    private void RestartGravity()
    {
        if (_engine is null) return;
        double ms = Math.Max(50, 800 * Math.Pow(0.85, _engine.Level - 1));

        _gravityTimer?.Stop();
        _gravityTimer?.Dispose();
        _gravityTimer = new System.Timers.Timer(ms);
        _gravityTimer.Elapsed += async (_, _) => await OnGravityTick();
        _gravityTimer.AutoReset = true;
        _gravityTimer.Start();
    }

    private void StartClockTimer()
    {
        _clockTimer?.Stop();
        _clockTimer?.Dispose();
        _clockTimer = new System.Timers.Timer(500);
        _clockTimer.Elapsed += (_, _) =>
        {
            if (_engine is null || _paused || _countingDown) return;
            UpdateTimeText();
            InvokeAsync(StateHasChanged);
        };
        _clockTimer.AutoReset = true;
        _clockTimer.Start();
    }

    private async Task OnGravityTick()
    {
        if (_engine is null || _paused || _countingDown) return;
        if (_engine.State != GameState.Playing) return;

        var result = _engine.Tick();
        await HandleResult(result);
        await InvokeAsync(async () =>
        {
            await Render();
            StateHasChanged();
        });
    }

    private async Task HandleResult(LockResult? result)
    {
        if (_engine is null) return;
        RestartGravity();

        if (result is not { } r) return;

        if (r.LinesCleared > 0)
            await JS.InvokeVoidAsync("TetrisAudio.playEffect", "blip");

        if (r.GameOver)
        {
            await EndGame();
        }
        else if (r.StageClear)
        {
            _ = BeginStageClear();
        }
    }

    private async Task EndGame()
    {
        _gravityTimer?.Stop();
        _clock.Stop();
        _clockTimer?.Stop();
        await JS.InvokeVoidAsync("TetrisAudio.stopBgm");
        ShowMessageOverlay("GAME OVER", "Press R to play again");
        await InvokeAsync(StateHasChanged);
    }

    private async Task BeginStageClear()
    {
        _gravityTimer?.Stop();
        _countingDown = true;
        await JS.InvokeVoidAsync("TetrisAudio.stopBgm");
        await JS.InvokeVoidAsync("TetrisAudio.playEffect", "clear");
        await InvokeAsync(async () =>
        {
            await Render();
            StateHasChanged();
        });

        var expected = _engine;

        ShowMessageOverlay("STAGE CLEAR", "Get ready...");
        await InvokeAsync(StateHasChanged);
        await Task.Delay(900);
        if (!StillCounting(expected)) return;

        for (int n = 3; n >= 1; n--)
        {
            _messageTitle = n.ToString();
            _messageSub = "";
            await InvokeAsync(StateHasChanged);
            await Task.Delay(600);
            if (!StillCounting(expected)) return;
        }

        expected!.AdvanceStage();
        _showMessage = false;
        _countingDown = false;
        await JS.InvokeVoidAsync("TetrisAudio.playBgm");
        RestartGravity();
        await InvokeAsync(async () =>
        {
            await Render();
            StateHasChanged();
        });
    }

    private bool StillCounting(GameEngine? expected) =>
        _countingDown && ReferenceEquals(_engine, expected);

    private void ShowMessageOverlay(string title, string sub)
    {
        _messageTitle = title;
        _messageSub = sub;
        _showMessage = true;
    }

    private async void TogglePause()
    {
        if (_engine is null || _countingDown) return;
        if (_engine.State != GameState.Playing) return;

        _paused = !_paused;
        if (_paused)
        {
            _gravityTimer?.Stop();
            _clock.Stop();
            _clockTimer?.Stop();
            await JS.InvokeVoidAsync("TetrisAudio.pauseBgm");
            ShowMessageOverlay("PAUSED", "Press P to resume");
            _pauseLabel = "Resume (P)";
        }
        else
        {
            _showMessage = false;
            _clock.Start();
            StartClockTimer();
            await JS.InvokeVoidAsync("TetrisAudio.resumeBgm");
            RestartGravity();
            _pauseLabel = "Pause (P)";
            await _containerRef.FocusAsync();
        }
        StateHasChanged();
    }

    private async void ToggleMute()
    {
        await EnsureAudio();
        _muted = !_muted;
        await JS.InvokeVoidAsync("TetrisAudio.setMuted", _muted);
        _muteLabel = _muted ? "Unmute (M)" : "Mute (M)";
        StateHasChanged();
    }

    private async Task OnKeyDown(KeyboardEventArgs e)
    {
        if (_showMenu)
        {
            if (e.Key == "Enter")
            {
                StartGame(GameMode.Infinite);
            }
            return;
        }

        switch (e.Key)
        {
            case "p":
            case "P":
                TogglePause();
                return;
            case "m":
            case "M":
                ToggleMute();
                return;
            case "r":
            case "R":
                if (_engine is not null) StartGame(_engine.Mode);
                return;
        }

        if (_engine is null || _paused || _countingDown ||
            _engine.State != GameState.Playing)
        {
            return;
        }

        switch (e.Key)
        {
            case "ArrowLeft":
                _engine.MoveLeft();
                break;
            case "ArrowRight":
                _engine.MoveRight();
                break;
            case "ArrowUp":
            case "x":
            case "X":
                _engine.Rotate();
                break;
            case "ArrowDown":
                await HandleResult(_engine.SoftDrop());
                break;
            case " ":
                await HandleResult(_engine.HardDrop());
                break;
            default:
                return;
        }

        await Render();
    }

    private void OnRestart()
    {
        if (_engine is not null) StartGame(_engine.Mode);
    }

    private async void OnMenu()
    {
        _gravityTimer?.Stop();
        _clock.Stop();
        _clockTimer?.Stop();
        await JS.InvokeVoidAsync("TetrisAudio.stopBgm");
        _paused = false;
        _countingDown = false;
        _pauseLabel = "Pause (P)";
        _showMessage = false;
        _showMenu = true;
        StateHasChanged();
    }

    private void UpdateStats()
    {
        if (_engine is null) return;
        _scoreText = _engine.Score.ToString("N0");
        _levelText = _engine.Level.ToString();
        _linesText = _engine.Lines.ToString();
        _stageText = (_engine.CurrentStage + 1).ToString();
        _gemsText = _engine.GemsRemaining.ToString();
        UpdateTimeText();
    }

    private void UpdateTimeText()
    {
        var t = _clock.Elapsed;
        _timeText = $"{(int)t.TotalMinutes:00}:{t.Seconds:00}";
    }

    private async Task Render()
    {
        if (_engine is null) return;

        UpdateStats();

        // Board data: flatten 2D board to 1D array for JS interop
        int w = GameEngine.Width;
        int h = GameEngine.Height;
        var boardFlat = new int[h * w];
        for (int r = 0; r < h; r++)
            for (int c = 0; c < w; c++)
                boardFlat[r * w + c] = _engine.CellAt(r, c);

        var piece = _engine.Current;
        int activeSize = piece.Shape.Size;
        var activeCellsFlat = FlattenCells(piece.Cells, activeSize);

        int ghostRow = _engine.GhostRow();
        var ghostCellsFlat = activeCellsFlat;
        int ghostSize = activeSize;

        bool isPlaying = _engine.State == GameState.Playing;

        await JS.InvokeVoidAsync("TetrisCanvas.drawBoard",
            "board-canvas", boardFlat, w, h, CellSize,
            activeCellsFlat, piece.Row, piece.Col, piece.Color,
            ghostCellsFlat, ghostRow, piece.Col, piece.Color, ghostSize,
            activeSize, isPlaying);

        // Draw next piece
        var nextShape = Tetromino.All[_engine.NextIndex];
        var nextCells = nextShape.Rotations[0];
        int nextSize = nextShape.Size;
        var nextFlat = FlattenCells(nextCells, nextSize);

        await JS.InvokeVoidAsync("TetrisCanvas.drawNext",
            "next-canvas", nextFlat, nextSize, nextShape.Color, NextCell);
    }

    private static int[] FlattenCells(int[,] cells, int size)
    {
        var flat = new int[size * size];
        for (int r = 0; r < size; r++)
            for (int c = 0; c < size; c++)
                flat[r * size + c] = cells[r, c];
        return flat;
    }

    public void Dispose()
    {
        _gravityTimer?.Stop();
        _gravityTimer?.Dispose();
        _clockTimer?.Stop();
        _clockTimer?.Dispose();
    }
}
