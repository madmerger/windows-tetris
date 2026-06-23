namespace WindowsTetris.Game;

public enum GameState
{
    Playing,
    GameOver,
    StageClear,
}

/// <summary>
/// Result of locking a piece, returned so the UI can react (sounds, flashes).
/// </summary>
public readonly struct LockResult
{
    public int LinesCleared { get; init; }
    public int[] ClearedRows { get; init; }
    public bool GameOver { get; init; }
    public bool StageClear { get; init; }
}

/// <summary>
/// Pure game logic for the Tetris board - no UI, no timers, no audio. Mirrors
/// the feature set of the original Unity project: Infinite/Stage modes, scoring,
/// next-piece preview, ghost piece, soft/hard drop, levels and gem clearing.
/// </summary>
public sealed class GameEngine
{
    public const int Width = 10;
    public const int Height = 20;

    private static readonly int[] LineScores = { 0, 40, 100, 300, 1200 };

    private readonly int[,] _board = new int[Height, Width];
    private readonly Queue<int> _queue = new();
    private readonly Random _rng;
    private readonly int[]? _script;
    private int _scriptPos;

    public GameMode Mode { get; private set; }
    public GameState State { get; private set; }
    public int Score { get; private set; }
    public int Lines { get; private set; }
    public int CurrentStage { get; private set; }
    public int GemsRemaining { get; private set; }
    public ActivePiece Current { get; private set; }

    public int Level => Lines / 20 + 1;
    public int NextIndex
    {
        get
        {
            EnsureQueue();
            return _queue.Peek();
        }
    }

    /// <param name="script">
    /// Optional fixed sequence of tetromino indices (0-6) used instead of the
    /// random 7-bag. Cycled indefinitely. Intended for deterministic tests.
    /// </param>
    public GameEngine(GameMode mode, int stage = 0, int? seed = null, int[]? script = null)
    {
        _rng = seed is { } s ? new Random(s) : new Random();
        _script = script is { Length: > 0 } ? script : null;
        Mode = mode;
        CurrentStage = stage;
        Reset();
    }

    /// <summary>Starts a fresh game on the current stage (keeps mode/stage).</summary>
    public void Reset()
    {
        Array.Clear(_board);
        _queue.Clear();
        Score = 0;
        Lines = 0;
        GemsRemaining = 0;
        State = GameState.Playing;

        if (Mode == GameMode.Stage)
        {
            LoadStage(CurrentStage);
        }

        SpawnNext();
    }

    /// <summary>Advances to the next stage layout (Stage mode) and continues.</summary>
    public void AdvanceStage()
    {
        CurrentStage++;
        Array.Clear(_board);
        GemsRemaining = 0;
        State = GameState.Playing;
        LoadStage(CurrentStage);
        SpawnNext();
    }

    private void LoadStage(int stage)
    {
        var layout = Stages.Get(stage);
        for (int r = 0; r < Height; r++)
        {
            for (int c = 0; c < Width; c++)
            {
                int v = layout[r, c];
                _board[r, c] = v;
                if (v == CellType.Gem)
                {
                    GemsRemaining++;
                }
            }
        }
    }

    public int CellAt(int row, int col) => _board[row, col];

    // ----- Randomiser (7-bag) -------------------------------------------------

    private void EnsureQueue()
    {
        if (_script is not null)
        {
            while (_queue.Count < 7)
            {
                _queue.Enqueue(_script[_scriptPos]);
                _scriptPos = (_scriptPos + 1) % _script.Length;
            }
            return;
        }

        while (_queue.Count < 7)
        {
            var bag = new[] { 0, 1, 2, 3, 4, 5, 6 };
            for (int i = bag.Length - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (bag[i], bag[j]) = (bag[j], bag[i]);
            }
            foreach (var b in bag)
            {
                _queue.Enqueue(b);
            }
        }
    }

    private int DrawNext()
    {
        EnsureQueue();
        return _queue.Dequeue();
    }

    private void SpawnNext()
    {
        int index = DrawNext();
        EnsureQueue();
        var shape = Tetromino.All[index];
        var piece = new ActivePiece
        {
            Index = index,
            Rotation = 0,
            Row = 0,
            Col = (Width - shape.Size) / 2,
        };

        if (Collides(piece))
        {
            Current = piece;
            State = GameState.GameOver;
        }
        else
        {
            Current = piece;
        }
    }

    // ----- Collision ----------------------------------------------------------

    private bool Collides(ActivePiece piece)
    {
        var cells = piece.Cells;
        int n = piece.Shape.Size;
        for (int r = 0; r < n; r++)
        {
            for (int c = 0; c < n; c++)
            {
                if (cells[r, c] == 0) continue;
                int br = piece.Row + r;
                int bc = piece.Col + c;
                if (bc < 0 || bc >= Width || br >= Height) return true;
                if (br >= 0 && _board[br, bc] != CellType.Empty) return true;
            }
        }
        return false;
    }

    // ----- Player actions -----------------------------------------------------

    public bool MoveLeft() => TryMove(0, -1);
    public bool MoveRight() => TryMove(0, 1);

    private bool TryMove(int dRow, int dCol)
    {
        if (State != GameState.Playing) return false;
        var p = Current;
        p.Row += dRow;
        p.Col += dCol;
        if (Collides(p)) return false;
        Current = p;
        return true;
    }

    /// <summary>Rotates clockwise with a small set of wall-kick offsets.</summary>
    public bool Rotate()
    {
        if (State != GameState.Playing) return false;
        var p = Current;
        p.Rotation = (p.Rotation + 1) % 4;

        ReadOnlySpan<int> kicks = stackalloc int[] { 0, -1, 1, -2, 2 };
        foreach (var dc in kicks)
        {
            var test = p;
            test.Col += dc;
            if (!Collides(test))
            {
                Current = test;
                return true;
            }
        }
        // Try nudging up once (helps the I-piece against the floor).
        var up = p;
        up.Row -= 1;
        if (!Collides(up))
        {
            Current = up;
            return true;
        }
        return false;
    }

    /// <summary>Steps the piece down one cell; locks it when it can't fall.</summary>
    public LockResult? Tick() => SoftStep(addScore: false);

    /// <summary>Player-driven soft drop: one cell down, scoring one point.</summary>
    public LockResult? SoftDrop() => SoftStep(addScore: true);

    private LockResult? SoftStep(bool addScore)
    {
        if (State != GameState.Playing) return null;
        if (TryMove(1, 0))
        {
            if (addScore) Score += 1;
            return null;
        }
        return Lock();
    }

    /// <summary>Drops the piece to the floor instantly and locks it.</summary>
    public LockResult? HardDrop()
    {
        if (State != GameState.Playing) return null;
        int distance = 0;
        while (TryMove(1, 0)) distance++;
        Score += distance * 2;
        return Lock();
    }

    public int GhostRow()
    {
        var p = Current;
        while (true)
        {
            var test = p;
            test.Row += 1;
            if (Collides(test)) break;
            p = test;
        }
        return p.Row;
    }

    // ----- Locking & line clears ---------------------------------------------

    private LockResult Lock()
    {
        var p = Current;
        var cells = p.Cells;
        int n = p.Shape.Size;
        bool aboveTop = false;

        for (int r = 0; r < n; r++)
        {
            for (int c = 0; c < n; c++)
            {
                if (cells[r, c] == 0) continue;
                int br = p.Row + r;
                int bc = p.Col + c;
                if (br < 0)
                {
                    aboveTop = true;
                    continue;
                }
                _board[br, bc] = p.Color;
            }
        }

        if (aboveTop)
        {
            State = GameState.GameOver;
            return new LockResult { GameOver = true };
        }

        var cleared = ClearLines();
        if (cleared.Count > 0)
        {
            Score += LineScores[cleared.Count] * Level;
            Lines += cleared.Count;
        }

        if (Mode == GameMode.Stage)
        {
            GemsRemaining = CountGems();
            if (GemsRemaining == 0)
            {
                State = GameState.StageClear;
                return new LockResult
                {
                    LinesCleared = cleared.Count,
                    ClearedRows = cleared.ToArray(),
                    StageClear = true,
                };
            }
        }

        SpawnNext();
        return new LockResult
        {
            LinesCleared = cleared.Count,
            ClearedRows = cleared.ToArray(),
            GameOver = State == GameState.GameOver,
        };
    }

    private List<int> ClearLines()
    {
        var fullRows = new List<int>();
        for (int r = 0; r < Height; r++)
        {
            bool full = true;
            for (int c = 0; c < Width; c++)
            {
                if (_board[r, c] == CellType.Empty)
                {
                    full = false;
                    break;
                }
            }
            if (full) fullRows.Add(r);
        }

        foreach (var row in fullRows)
        {
            for (int r = row; r > 0; r--)
            {
                for (int c = 0; c < Width; c++)
                {
                    _board[r, c] = _board[r - 1, c];
                }
            }
            for (int c = 0; c < Width; c++)
            {
                _board[0, c] = CellType.Empty;
            }
        }
        return fullRows;
    }

    private int CountGems()
    {
        int count = 0;
        for (int r = 0; r < Height; r++)
            for (int c = 0; c < Width; c++)
                if (_board[r, c] == CellType.Gem)
                    count++;
        return count;
    }
}
