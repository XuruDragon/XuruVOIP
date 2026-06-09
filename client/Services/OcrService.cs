using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms;
using Tesseract;
using XuruVoipClient.Models;

namespace XuruVoipClient.Services;

/// <summary>
/// Captures a region of a specific monitor, runs Tesseract OCR and parses the Star Citizen position line.
/// </summary>
public class OcrService : IDisposable
{
    // Regex: "Zone: Hangar XLTop Area18 854875740883 Pos: -4.98m -10.00m -114.03m"
    private static readonly Regex PosRegex = new(
        @"Zone:\s+(.+?)\s+Pos:\s+([-\d.]+)([a-zA-Z]*)\s+([-\d.]+)([a-zA-Z]*)\s+([-\d.]+)([a-zA-Z]*)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private TesseractEngine? _engine;
    private bool _disposed;

    public string LastRawText { get; private set; } = "";
    public PlayerPosition LastPosition { get; private set; } = new();

    public void Initialize(string tessDataDir)
    {
        _engine?.Dispose();
        Environment.SetEnvironmentVariable("TESSDATA_PREFIX", null);
        _engine = new TesseractEngine(tessDataDir, "eng", EngineMode.Default);
        _engine.SetVariable("tessedit_char_whitelist",
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-.: ");
    }

    /// <summary>
    /// Captures the given region on the given monitor (or game window client rect) and returns the parsed position.
    /// </summary>
    public PlayerPosition? Capture(int monitorIndex, System.Windows.Rect region, GameDetectionService.RECT? gameRect = null)
    {
        if (_engine == null) return null;

        // Absolute pixel coordinates on the monitor or game client area
        int absX;
        int absY;
        if (gameRect.HasValue)
        {
            absX = gameRect.Value.Left + (int)region.X;
            absY = gameRect.Value.Top + (int)region.Y;
        }
        else
        {
            var screens = Screen.AllScreens;
            if (monitorIndex >= screens.Length) monitorIndex = 0;
            var screen = screens[monitorIndex];
            absX = screen.Bounds.X + (int)region.X;
            absY = screen.Bounds.Y + (int)region.Y;
        }
        int w = Math.Max(1, (int)region.Width);
        int h = Math.Max(1, (int)region.Height);

        using var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.CopyFromScreen(absX, absY, 0, 0, new System.Drawing.Size(w, h), CopyPixelOperation.SourceCopy);

        // Pre-process: grayscale + contrast boost
        using var processed = Preprocess(bmp);

        using var pix = PixConverter.ToPix(processed);
        using var page = _engine.Process(pix, PageSegMode.Auto);
        LastRawText = page.GetText()?.Trim() ?? "";

        return TryParse(LastRawText);
    }

    private static Bitmap Preprocess(Bitmap src)
    {
        var gray = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(gray);

        // Greyscale colour matrix
        var cm = new System.Drawing.Imaging.ColorMatrix(new float[][]
        {
            new float[] { 0.299f, 0.299f, 0.299f, 0, 0 },
            new float[] { 0.587f, 0.587f, 0.587f, 0, 0 },
            new float[] { 0.114f, 0.114f, 0.114f, 0, 0 },
            new float[] { 0,      0,      0,      1, 0 },
            new float[] { 0,      0,      0,      0, 1 },
        });
        var attr = new ImageAttributes();
        attr.SetColorMatrix(cm);
        g.DrawImage(src, new Rectangle(0, 0, src.Width, src.Height),
            0, 0, src.Width, src.Height, GraphicsUnit.Pixel, attr);
        return gray;
    }

    private static readonly string[] SubZoneKeywords = new[]
    {
        "elevator", "elev", "transit", "carriage", "shuttle", "seat", "chair", "cockpit", "pilot", "turret", "ladder", "objectcontainer"
    };

    private static readonly string[] LargeZoneKeywords = new[]
    {
        "solarsystem", "system", "root", "ooc_", "stanton", "pyro", "nyx"
    };

    private static bool IsTooSpecific(string zone)
    {
        string lower = zone.ToLowerInvariant();
        foreach (var kw in SubZoneKeywords)
        {
            if (lower.Contains(kw)) return true;
        }
        return false;
    }

    private static bool IsTooLarge(string zone)
    {
        string lower = zone.ToLowerInvariant();
        foreach (var kw in LargeZoneKeywords)
        {
            if (lower.Contains(kw)) return true;
        }
        return false;
    }

    private PlayerPosition? TryParse(string text)
    {
        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var parsedPositions = new System.Collections.Generic.List<PlayerPosition>();

        foreach (var line in lines)
        {
            var m = PosRegex.Match(line);
            if (!m.Success) continue;

            static double ConvertUnit(string val, string unit)
            {
                if (!double.TryParse(val, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double d)) return 0;
                return unit.ToLower() switch
                {
                    "km" => d * 1000.0,
                    _ => d,  // m or empty = metres
                };
            }

            var pos = new PlayerPosition
            {
                Zone = m.Groups[1].Value.Trim(),
                X = ConvertUnit(m.Groups[2].Value, m.Groups[3].Value),
                Y = ConvertUnit(m.Groups[4].Value, m.Groups[5].Value),
                Z = ConvertUnit(m.Groups[6].Value, m.Groups[7].Value),
                TsCapture = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0
            };
            parsedPositions.Add(pos);
        }

        if (parsedPositions.Count == 0) return null;

        // Choose the best position
        PlayerPosition? bestPos = null;
        foreach (var pos in parsedPositions)
        {
            if (!IsTooSpecific(pos.Zone) && !IsTooLarge(pos.Zone))
            {
                bestPos = pos;
                break;
            }
        }

        if (bestPos == null)
        {
            foreach (var pos in parsedPositions)
            {
                if (!IsTooLarge(pos.Zone))
                {
                    bestPos = pos;
                    break;
                }
            }
        }

        if (bestPos == null)
        {
            bestPos = parsedPositions[0];
        }

        LastPosition = bestPos;
        return LastPosition;
    }

    public bool? ScanHelmetCompass(int monitorIndex, GameDetectionService.RECT? gameRect = null)
    {
        int sc_width;
        int sc_height;
        int startX;
        int startY;

        if (gameRect.HasValue)
        {
            sc_width = gameRect.Value.Right - gameRect.Value.Left;
            sc_height = gameRect.Value.Bottom - gameRect.Value.Top;
            startX = gameRect.Value.Left;
            startY = gameRect.Value.Top;
        }
        else
        {
            var screens = Screen.AllScreens;
            if (monitorIndex >= screens.Length) monitorIndex = 0;
            var screen = screens[monitorIndex];
            sc_width = screen.Bounds.Width;
            sc_height = screen.Bounds.Height;
            startX = screen.Bounds.X;
            startY = screen.Bounds.Y;
        }

        double sc_width_hud = Math.Min(sc_height * 16.0 / 9.0, sc_width);
        int region_w = (int)(sc_width_hud * 0.30);
        int region_h = (int)(sc_height * 0.05);
        int region_x = startX + (sc_width - region_w) / 2;
        int region_y = startY + (int)(sc_height * 0.01);

        using var bmp = new Bitmap(region_w, region_h, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            try
            {
                g.CopyFromScreen(region_x, region_y, 0, 0, new System.Drawing.Size(region_w, region_h), CopyPixelOperation.SourceCopy);
            }
            catch
            {
                return null;
            }
        }

        // Grayscale and count bright pixels
        int brightCount = 0;
        int totalCount = bmp.Width * bmp.Height;
        var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
        var bmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            int bytes = Math.Abs(bmpData.Stride) * bmp.Height;
            byte[] rgbValues = new byte[bytes];
            Marshal.Copy(bmpData.Scan0, rgbValues, 0, bytes);

            int stride = bmpData.Stride;
            for (int y = 0; y < bmp.Height; y++)
            {
                int rowOffset = y * stride;
                for (int x = 0; x < bmp.Width; x++)
                {
                    int offset = rowOffset + x * 4;
                    byte b = rgbValues[offset];
                    byte g = rgbValues[offset + 1];
                    byte r = rgbValues[offset + 2];
                    double gray = 0.299 * r + 0.587 * g + 0.114 * b;
                    if (gray > 100) brightCount++;
                }
            }
        }
        finally
        {
            bmp.UnlockBits(bmpData);
        }

        double ratio = (double)brightCount / totalCount;
        return ratio > 0.005; // > 0.5%
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _engine?.Dispose();
    }
}
