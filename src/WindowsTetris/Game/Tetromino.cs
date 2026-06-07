namespace WindowsTetris.Game;

/// <summary>
/// Identifiers for every cell value that can live on the board.
/// 1-7 are the seven tetromino colours, 8 is an immovable stage wall block and
/// 9 is a gem (the orange blocks that must be cleared in Stage mode).
/// </summary>
public static class CellType
{
    public const int Empty = 0;
    public const int I = 1;
    public const int O = 2;
    public const int T = 3;
    public const int S = 4;
    public const int Z = 5;
    public const int J = 6;
    public const int L = 7;
    public const int Wall = 8;
    public const int Gem = 9;
}

/// <summary>
/// Immutable definition of a tetromino: its colour code plus the four rotation
/// states. Each rotation is a square matrix where a non-zero value marks a
/// filled cell. Rotations are pre-computed so gameplay never allocates.
/// </summary>
public sealed class Tetromino
{
    public int Color { get; }
    public int Size { get; }

    /// <summary>Rotations[state][row, col] - 1 when the cell is filled.</summary>
    public int[][,] Rotations { get; }

    private Tetromino(int color, int[,] spawn)
    {
        Color = color;
        Size = spawn.GetLength(0);
        Rotations = new int[4][,];
        Rotations[0] = spawn;
        for (int i = 1; i < 4; i++)
        {
            Rotations[i] = RotateClockwise(Rotations[i - 1]);
        }
    }

    private static int[,] RotateClockwise(int[,] src)
    {
        int n = src.GetLength(0);
        var dst = new int[n, n];
        for (int r = 0; r < n; r++)
        {
            for (int c = 0; c < n; c++)
            {
                dst[c, n - 1 - r] = src[r, c];
            }
        }
        return dst;
    }

    /// <summary>The seven standard tetrominoes, indexed 0-6.</summary>
    public static readonly Tetromino[] All =
    {
        // I
        new(CellType.I, new[,]
        {
            { 0, 0, 0, 0 },
            { 1, 1, 1, 1 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
        }),
        // O
        new(CellType.O, new[,]
        {
            { 1, 1 },
            { 1, 1 },
        }),
        // T
        new(CellType.T, new[,]
        {
            { 0, 1, 0 },
            { 1, 1, 1 },
            { 0, 0, 0 },
        }),
        // S
        new(CellType.S, new[,]
        {
            { 0, 1, 1 },
            { 1, 1, 0 },
            { 0, 0, 0 },
        }),
        // Z
        new(CellType.Z, new[,]
        {
            { 1, 1, 0 },
            { 0, 1, 1 },
            { 0, 0, 0 },
        }),
        // J
        new(CellType.J, new[,]
        {
            { 1, 0, 0 },
            { 1, 1, 1 },
            { 0, 0, 0 },
        }),
        // L
        new(CellType.L, new[,]
        {
            { 0, 0, 1 },
            { 1, 1, 1 },
            { 0, 0, 0 },
        }),
    };
}

/// <summary>A tetromino placed on the board: which piece, where, and its rotation.</summary>
public struct ActivePiece
{
    public int Index;      // index into Tetromino.All
    public int Row;        // board row of the matrix top-left
    public int Col;        // board column of the matrix top-left
    public int Rotation;   // 0-3

    public readonly Tetromino Shape => Tetromino.All[Index];
    public readonly int Color => Shape.Color;
    public readonly int[,] Cells => Shape.Rotations[Rotation];
}
