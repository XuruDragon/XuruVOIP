using System;
using System.Windows;
using System.Windows.Media;

namespace XuruVoipClient.Services;

public static class ThemeManager
{
    public static void ApplyTheme(string themeName)
    {
        Color accentColor;
        Color accentGlowColor;
        Color bgDeep;
        Color bgSurface;
        Color bgElevated;
        Color hudBg;

        switch (themeName)
        {
            case "Anvil":
                accentColor = Color.FromRgb(0xFF, 0x17, 0x44); // Crimson Red
                accentGlowColor = Color.FromRgb(0xB7, 0x00, 0x1E);
                bgDeep = Color.FromRgb(0x10, 0x0C, 0x0D);
                bgSurface = Color.FromRgb(0x1B, 0x13, 0x15);
                bgElevated = Color.FromRgb(0x25, 0x1A, 0x1D);
                hudBg = Color.FromArgb(0xCC, 0x10, 0x0C, 0x0D);
                break;
            case "Drake":
                accentColor = Color.FromRgb(0xFF, 0x73, 0x00); // Amber / Rust Orange
                accentGlowColor = Color.FromRgb(0xC2, 0x41, 0x00);
                bgDeep = Color.FromRgb(0x0F, 0x0D, 0x0C);
                bgSurface = Color.FromRgb(0x1A, 0x15, 0x13);
                bgElevated = Color.FromRgb(0x24, 0x1C, 0x19);
                hudBg = Color.FromArgb(0xCC, 0x0F, 0x0D, 0x0C);
                break;
            case "RSI":
                accentColor = Color.FromRgb(0x00, 0x66, 0xFF); // Cobalt Blue
                accentGlowColor = Color.FromRgb(0x00, 0x33, 0xB3);
                bgDeep = Color.FromRgb(0x07, 0x0B, 0x12);
                bgSurface = Color.FromRgb(0x0F, 0x14, 0x20);
                bgElevated = Color.FromRgb(0x16, 0x1D, 0x2E);
                hudBg = Color.FromArgb(0xCC, 0x07, 0x0B, 0x12);
                break;
            case "Origin":
                accentColor = Color.FromRgb(0x00, 0xE5, 0xFF); // Luxury Ice Blue
                accentGlowColor = Color.FromRgb(0x00, 0x7B, 0x99);
                bgDeep = Color.FromRgb(0x09, 0x0E, 0x10);
                bgSurface = Color.FromRgb(0x11, 0x1A, 0x1E);
                bgElevated = Color.FromRgb(0x19, 0x25, 0x2A);
                hudBg = Color.FromArgb(0xCC, 0x09, 0x0E, 0x10);
                break;
            default: // Aegis / Default
                accentColor = Color.FromRgb(0x00, 0xE6, 0x76); // Milspec Green
                accentGlowColor = Color.FromRgb(0x00, 0x8E, 0x3C);
                bgDeep = Color.FromRgb(0x0A, 0x0F, 0x0D);
                bgSurface = Color.FromRgb(0x12, 0x1A, 0x15);
                bgElevated = Color.FromRgb(0x19, 0x24, 0x1E);
                hudBg = Color.FromArgb(0xCC, 0x0A, 0x0F, 0x0D);
                break;
        }

        // Update raw Color resources so gradients can bind to them dynamically
        Application.Current.Resources["AccentColor"] = accentColor;
        Application.Current.Resources["AccentGlowColor"] = accentGlowColor;
        Application.Current.Resources["BgDeepColor"] = bgDeep;
        Application.Current.Resources["BgSurfaceColor"] = bgSurface;
        Application.Current.Resources["BgElevatedColor"] = bgElevated;
        Application.Current.Resources["HudBackgroundColor"] = hudBg;

        // Recreate the SolidColorBrushes so DynamicResource references of the brushes themselves update
        var accentBrush = new SolidColorBrush(accentColor);
        accentBrush.Freeze(); // Freeze for performance and thread-safety in WPF
        Application.Current.Resources["Accent"] = accentBrush;

        var accentGlowBrush = new SolidColorBrush(accentGlowColor);
        accentGlowBrush.Freeze();
        Application.Current.Resources["AccentGlow"] = accentGlowBrush;

        // Update the AccentGradient LinearGradientBrush
        var accentGradient = new LinearGradientBrush(accentColor, accentGlowColor, new Point(0, 0), new Point(1, 1));
        accentGradient.Freeze();
        Application.Current.Resources["AccentGradient"] = accentGradient;

        var bgDeepBrush = new SolidColorBrush(bgDeep);
        bgDeepBrush.Freeze();
        Application.Current.Resources["BgDeep"] = bgDeepBrush;

        var bgSurfaceBrush = new SolidColorBrush(bgSurface);
        bgSurfaceBrush.Freeze();
        Application.Current.Resources["BgSurface"] = bgSurfaceBrush;

        var bgElevatedBrush = new SolidColorBrush(bgElevated);
        bgElevatedBrush.Freeze();
        Application.Current.Resources["BgElevated"] = bgElevatedBrush;

        var hudBgBrush = new SolidColorBrush(hudBg);
        hudBgBrush.Freeze();
        Application.Current.Resources["HudBackground"] = hudBgBrush;
    }
}
