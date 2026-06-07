using WindowsTetris.Game;
using Xunit;

namespace WindowsTetris.Tests;

public class GameEngineTests
{
    private const int I = 0, O = 1, T = 2, S = 3, Z = 4, J = 5, L = 6;

    [Fact]
    public void NewInfiniteGame_StartsClean()
    {
        var e = new GameEngine(GameMode.Infinite);
        Assert.Equal(GameState.Playing, e.State);
        Assert.Equal(0, e.Score);
        Assert.Equal(0, e.Lines);
        Assert.Equal(1, e.Level);
    }

    [Fact]
    public void StageMode_LoadsStageOneGems()
    {
        var e = new GameEngine(GameMode.Stage);
        // Stage 1 places two gems on the bottom row.
        Assert.Equal(2, e.GemsRemaining);
        Assert.Equal(0, e.CurrentStage);
    }

    [Fact]
    public void FullBottomRow_ClearsLineAndScores()
    {
        // Feed I, I, O so the bottom row can be completely filled.
        var e = new GameEngine(GameMode.Infinite, script: new[] { I, I, O });

        // First I-piece -> columns 0..3
        for (int i = 0; i < 3; i++) e.MoveLeft();
        e.HardDrop();

        // Second I-piece -> columns 4..7
        e.MoveRight();
        e.HardDrop();

        // O-piece -> columns 8..9
        for (int i = 0; i < 4; i++) e.MoveRight();
        e.HardDrop();

        Assert.Equal(1, e.Lines);
        Assert.True(e.Score >= 40, $"score was {e.Score}");
        Assert.Equal(GameState.Playing, e.State);
    }

    [Fact]
    public void StackingToTop_EndsGame()
    {
        // Keep dropping O-pieces in the spawn column; eventually it tops out.
        var e = new GameEngine(GameMode.Infinite, script: new[] { O });
        for (int i = 0; i < 20 && e.State == GameState.Playing; i++)
        {
            e.HardDrop();
        }
        Assert.Equal(GameState.GameOver, e.State);
    }

    [Fact]
    public void HardDrop_LocksImmediately_AndSpawnsNext()
    {
        var e = new GameEngine(GameMode.Infinite, script: new[] { T });
        int before = e.NextIndex;
        e.HardDrop();
        // A new piece is active and the board has some filled cells now.
        int filled = 0;
        for (int r = 0; r < GameEngine.Height; r++)
            for (int c = 0; c < GameEngine.Width; c++)
                if (e.CellAt(r, c) != CellType.Empty) filled++;
        Assert.Equal(4, filled);
        Assert.Equal(T, before);
    }

    [Fact]
    public void MovingPastWall_IsBlocked()
    {
        var e = new GameEngine(GameMode.Infinite, script: new[] { O });
        // Spawn column is 4; push hard against the left wall.
        for (int i = 0; i < 10; i++) e.MoveLeft();
        // Find leftmost filled column of the active piece via a hard drop.
        e.HardDrop();
        bool leftColumnUsed = false;
        for (int r = 0; r < GameEngine.Height; r++)
            if (e.CellAt(r, 0) != CellType.Empty) leftColumnUsed = true;
        Assert.True(leftColumnUsed);
    }
}

public class TetrominoTests
{
    [Fact]
    public void SevenPieces_AreDefined()
    {
        Assert.Equal(7, Tetromino.All.Length);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void EachPiece_HasFourRotations_WithCells(int index)
    {
        var piece = Tetromino.All[index];
        Assert.Equal(4, piece.Rotations.Length);
        foreach (var rot in piece.Rotations)
        {
            int cells = 0;
            foreach (var v in rot) if (v != 0) cells++;
            Assert.Equal(4, cells); // every tetromino has four blocks
        }
        Assert.InRange(piece.Color, 1, 7);
    }
}

public class StagesTests
{
    [Fact]
    public void AllStages_Are20x10()
    {
        for (int s = 0; s < Stages.Count; s++)
        {
            var board = Stages.Get(s);
            Assert.Equal(20, board.GetLength(0));
            Assert.Equal(10, board.GetLength(1));
        }
    }

    [Fact]
    public void RawValues_MapToWallAndGem()
    {
        var board = Stages.Get(0);
        int gems = 0;
        foreach (var v in board)
        {
            Assert.True(v is CellType.Empty or CellType.Wall or CellType.Gem);
            if (v == CellType.Gem) gems++;
        }
        Assert.Equal(2, gems);
    }
}
