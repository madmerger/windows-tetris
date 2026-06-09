using System.Windows;
using System.Windows.Media;

namespace WindowsTetris.Theme;

/// <summary>
/// Applies a <see cref="ThemeDefinition"/> by pushing colour resources into
/// <see cref="Application.Current.Resources"/>. Because the XAML bindings use
/// <c>DynamicResource</c>, the UI updates immediately.
/// </summary>
public static class ThemeManager
{
    public static AppTheme CurrentTheme { get; private set; } = AppTheme.Dark;

    /// <summary>Raised after a theme switch so the window can rebuild any
    /// code-side brushes and re-render.</summary>
    public static event Action? ThemeChanged;

    public static void Apply(AppTheme theme)
    {
        CurrentTheme = theme;
        var def = ThemeDefinition.Get(theme);
        var res = Application.Current.Resources;

        static SolidColorBrush Brush(string hex)
        {
            var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            b.Freeze();
            return b;
        }

        static Color Col(string hex) => (Color)ColorConverter.ConvertFromString(hex);

        // Board
        res["BoardBackColor"] = Col(def.BoardBack);
        res["EmptyCellBrush"] = Brush(def.EmptyCell);
        res["GridLineBrush"] = Brush(def.GridLine);

        // UI chrome
        res["AccentBrush"] = Brush(def.Accent);
        res["PanelBrush"] = Brush(def.Panel);
        res["LabelBrush"] = Brush(def.Label);
        res["StatValueForeground"] = Brush(def.StatValue);
        res["ControlsTextBrush"] = Brush(def.ControlsText);
        res["CardBorderBrush"] = Brush(def.CardBorder);

        // Board border / glow
        res["BoardBorderBrush"] = Brush(def.BoardBorder);
        res["BoardBackBrush"] = Brush(def.BoardBack);
        res["BoardGlowColor"] = Col(def.BoardGlow);

        // Overlays
        res["OverlayBgBrush"] = Brush(def.OverlayBg);
        res["MessageBgBrush"] = Brush(def.MessageBg);
        res["MessageBorderBrush"] = Brush(def.MessageBorder);

        // Background gradients
        var bgBrush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
        };
        bgBrush.GradientStops.Add(new GradientStop(Col(def.BgGradientStart), 0));
        bgBrush.GradientStops.Add(new GradientStop(Col(def.BgGradientMid), 0.5));
        bgBrush.GradientStops.Add(new GradientStop(Col(def.BgGradientEnd), 1));
        bgBrush.Freeze();
        res["BgGradientBrush"] = bgBrush;

        var menuBrush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
        };
        menuBrush.GradientStops.Add(new GradientStop(Col(def.MenuGradientStart), 0));
        menuBrush.GradientStops.Add(new GradientStop(Col(def.MenuGradientEnd), 1));
        menuBrush.Freeze();
        res["MenuGradientBrush"] = menuBrush;

        res["MenuHintBrush"] = Brush(def.MenuHint);

        // WPF FluentTheme mode
        if (Application.Current.MainWindow is Window w)
        {
            w.ThemeMode = def.IsDark ? ThemeMode.Dark : ThemeMode.Light;
        }

        ThemeChanged?.Invoke();
    }

    public static void CycleNext()
    {
        var values = Enum.GetValues<AppTheme>();
        int next = ((int)CurrentTheme + 1) % values.Length;
        Apply(values[next]);
    }

    /// <summary>
    /// Builds the array of frozen brushes for tetromino / board cell rendering
    /// from the current theme definition.
    /// </summary>
    public static SolidColorBrush[] BuildCellBrushes()
    {
        var def = ThemeDefinition.Get(CurrentTheme);
        var brushes = new SolidColorBrush[def.CellColors.Length];
        for (int i = 0; i < def.CellColors.Length; i++)
        {
            var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(def.CellColors[i]));
            b.Freeze();
            brushes[i] = b;
        }
        return brushes;
    }

    /// <summary>
    /// Returns the cell stroke brush for filled pieces in the current theme.
    /// </summary>
    public static SolidColorBrush BuildCellStrokeBrush()
    {
        var def = ThemeDefinition.Get(CurrentTheme);
        var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(def.CellStroke));
        b.Freeze();
        return b;
    }
}
