namespace WindowsTetris.Theme;

public enum AppTheme
{
    Dark,
    Light,
    Retro,
}

/// <summary>
/// Immutable colour palette for a single UI theme. Every colour the renderer
/// and XAML resource system needs is declared here so themes are self-contained.
/// </summary>
public sealed class ThemeDefinition
{
    public required string Name { get; init; }

    // Window / root background gradient
    public required string BgGradientStart { get; init; }
    public required string BgGradientMid { get; init; }
    public required string BgGradientEnd { get; init; }

    // Board
    public required string BoardBack { get; init; }
    public required string EmptyCell { get; init; }
    public required string GridLine { get; init; }
    public required string BoardBorder { get; init; }
    public required string BoardGlow { get; init; }

    // UI chrome
    public required string Accent { get; init; }
    public required string Panel { get; init; }
    public required string Label { get; init; }
    public required string CardBorder { get; init; }
    public required string StatValue { get; init; }
    public required string ControlsText { get; init; }

    // Overlays
    public required string OverlayBg { get; init; }
    public required string MessageBg { get; init; }
    public required string MessageBorder { get; init; }

    // Menu
    public required string MenuGradientStart { get; init; }
    public required string MenuGradientEnd { get; init; }
    public required string MenuHint { get; init; }

    // Tetromino colours (indices 0-9 matching CellType)
    public required string[] CellColors { get; init; }

    // Cell rendering
    public required string CellStroke { get; init; }

    // ThemeMode for WPF FluentTheme
    public required bool IsDark { get; init; }

    // ---- Built-in palettes ---------------------------------------------------

    public static ThemeDefinition Get(AppTheme theme) => theme switch
    {
        AppTheme.Dark => Dark,
        AppTheme.Light => Light,
        AppTheme.Retro => Retro,
        _ => Dark,
    };

    public static readonly ThemeDefinition Dark = new()
    {
        Name = "Dark",
        BgGradientStart = "#0A0E18",
        BgGradientMid = "#0F1626",
        BgGradientEnd = "#0A0E18",
        BoardBack = "#0B0F1A",
        EmptyCell = "#161C2B",
        GridLine = "#1F273B",
        BoardBorder = "#243049",
        BoardGlow = "#0022D3EE",
        Accent = "#22D3EE",
        Panel = "#11172480",
        Label = "#8B96AD",
        CardBorder = "#243049",
        StatValue = "#FFFFFF",
        ControlsText = "#9AA6BE",
        OverlayBg = "#99060912",
        MessageBg = "#11172A",
        MessageBorder = "#243049",
        MenuGradientStart = "#0A0E18",
        MenuGradientEnd = "#101A30",
        MenuHint = "#8693AD",
        CellColors = new[]
        {
            "#161C2B", // 0 empty
            "#22D3EE", // 1 I  cyan
            "#FACC15", // 2 O  yellow
            "#C084FC", // 3 T  purple
            "#4ADE80", // 4 S  green
            "#F87171", // 5 Z  red
            "#60A5FA", // 6 J  blue
            "#FB923C", // 7 L  orange
            "#64748B", // 8 wall slate
            "#FFC83D", // 9 gem gold
        },
        CellStroke = "#3CFFFFFF",
        IsDark = true,
    };

    public static readonly ThemeDefinition Light = new()
    {
        Name = "Light",
        BgGradientStart = "#E8EDF5",
        BgGradientMid = "#F0F4FA",
        BgGradientEnd = "#E8EDF5",
        BoardBack = "#FFFFFF",
        EmptyCell = "#E8ECF2",
        GridLine = "#D0D7E2",
        BoardBorder = "#CBD5E1",
        BoardGlow = "#002563EB",
        Accent = "#2563EB",
        Panel = "#FFFFFF80",
        Label = "#64748B",
        CardBorder = "#CBD5E1",
        StatValue = "#1E293B",
        ControlsText = "#64748B",
        OverlayBg = "#AAFFFFFF",
        MessageBg = "#F1F5F9",
        MessageBorder = "#CBD5E1",
        MenuGradientStart = "#E8EDF5",
        MenuGradientEnd = "#DDE5F0",
        MenuHint = "#64748B",
        CellColors = new[]
        {
            "#E8ECF2", // 0 empty
            "#0891B2", // 1 I  teal
            "#CA8A04", // 2 O  amber
            "#9333EA", // 3 T  purple
            "#16A34A", // 4 S  green
            "#DC2626", // 5 Z  red
            "#2563EB", // 6 J  blue
            "#EA580C", // 7 L  orange
            "#94A3B8", // 8 wall slate
            "#D97706", // 9 gem amber
        },
        CellStroke = "#30000000",
        IsDark = false,
    };

    public static readonly ThemeDefinition Retro = new()
    {
        Name = "Retro",
        BgGradientStart = "#0F380F",
        BgGradientMid = "#306230",
        BgGradientEnd = "#0F380F",
        BoardBack = "#0F380F",
        EmptyCell = "#1A4A1A",
        GridLine = "#2B5A2B",
        BoardBorder = "#306230",
        BoardGlow = "#009BBC0F",
        Accent = "#9BBC0F",
        Panel = "#0F380F80",
        Label = "#8BAC0F",
        CardBorder = "#306230",
        StatValue = "#9BBC0F",
        ControlsText = "#8BAC0F",
        OverlayBg = "#AA0F380F",
        MessageBg = "#0F380F",
        MessageBorder = "#306230",
        MenuGradientStart = "#0F380F",
        MenuGradientEnd = "#306230",
        MenuHint = "#8BAC0F",
        CellColors = new[]
        {
            "#1A4A1A", // 0 empty
            "#9BBC0F", // 1 I  lightest green
            "#8BAC0F", // 2 O
            "#7B9C0F", // 3 T
            "#6B8C0F", // 4 S
            "#5B7C0F", // 5 Z
            "#4B6C0F", // 6 J
            "#3B5C0F", // 7 L
            "#306230", // 8 wall
            "#CDDE3F", // 9 gem
        },
        CellStroke = "#3C9BBC0F",
        IsDark = true,
    };
}
