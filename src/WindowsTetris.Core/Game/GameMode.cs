namespace WindowsTetris.Game;

/// <summary>
/// The two ways to play, mirroring the original Unity Tetris project.
/// </summary>
public enum GameMode
{
    /// <summary>Classic endless play.</summary>
    Infinite,

    /// <summary>Pre-built layouts where every gem must be cleared to advance.</summary>
    Stage,
}
