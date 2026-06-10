using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using XuruVoipClient.Models;

namespace XuruVoipClient.Services;

public class GameDetectionService : IDisposable
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private Process? _gameProcess;
    private FileStream? _logStream;
    private StreamReader? _logReader;
    private long _lastLogSize = 0;
    private Task? _watcherTask;
    private CancellationTokenSource? _cts;

    public RECT? GetGameClientRectInScreenCoords()
    {
        lock (_lock)
        {
            if (_gameProcess == null || _gameProcess.HasExited)
            {
                _gameProcess = GetGameProcess();
            }
            if (_gameProcess == null) return null;

            IntPtr hWnd = _gameProcess.MainWindowHandle;
            if (hWnd == IntPtr.Zero) return null;

            if (GetClientRect(hWnd, out RECT rect))
            {
                var pt = new POINT { X = 0, Y = 0 };
                if (ClientToScreen(hWnd, ref pt))
                {
                    return new RECT
                    {
                        Left = pt.X,
                        Top = pt.Y,
                        Right = pt.X + rect.Right,
                        Bottom = pt.Y + rect.Bottom
                    };
                }
            }
            return null;
        }
    }

    private readonly object _lock = new();

    public event Action<bool>? HelmetStateChanged; // (helmetOn)
    public event Action<PlayerPosition>? PositionReceived;
    public event Action<bool>? GameFocusChanged; // (isFocused)
    public event Action<bool>? GameRunningChanged; // (isRunning)
    public event Action<double>? GForceReceived;
    public event Action<double>? ExertionReceived;

    private static readonly Regex HelmetRegex = new(
        @"<AttachmentReceived>\s+Player\[(?<player>[^\]]+)\]\s+Attachment\[(?<att>[^\]]+)\]\s+Status\[(?<status>[^\]]+)\]\s+Port\[(?<port>[^\]]+)\]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex PosRegex = new(
        @"Zone:\s+(.+?)\s+Pos:\s+([-\d.]+)([a-zA-Z]*)\s+([-\d.]+)([a-zA-Z]*)\s+([-\d.]+)([a-zA-Z]*)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex GForceRegex = new(
        @"g-force:?\s*(?<gval>[\d.]+)\s*g",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex StaminaRegex = new(
        @"stamina:?\s*(?<val>[\d.]+)\s*%?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public bool IsGameRunning => _gameProcess != null && !_gameProcess.HasExited;
    public bool IsGameFocused { get; private set; }
    public string? CustomGameLogPath { get; set; }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _watcherTask = WatchLoopAsync(_cts.Token);
    }

    public bool CheckIfGameFocused()
    {
        lock (_lock)
        {
            if (_gameProcess == null || _gameProcess.HasExited)
            {
                _gameProcess = GetGameProcess();
                if (_gameProcess == null)
                {
                    if (IsGameFocused)
                    {
                        IsGameFocused = false;
                        GameFocusChanged?.Invoke(false);
                    }
                    return false;
                }
            }

            IntPtr fgWindow = GetForegroundWindow();
            if (fgWindow == IntPtr.Zero)
            {
                if (IsGameFocused)
                {
                    IsGameFocused = false;
                    GameFocusChanged?.Invoke(false);
                }
                return false;
            }

            GetWindowThreadProcessId(fgWindow, out uint pid);
            bool focused = (pid == _gameProcess.Id);
            if (focused != IsGameFocused)
            {
                IsGameFocused = focused;
                GameFocusChanged?.Invoke(focused);
            }
            return IsGameFocused;
        }
    }

    private Process? GetGameProcess()
    {
        var processes = Process.GetProcessesByName("StarCitizen");
        if (processes.Length > 0)
        {
            return processes[0];
        }
        return null;
    }

    private async Task WatchLoopAsync(CancellationToken ct)
    {
        string? lastLogPath = null;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                bool runningBefore = IsGameRunning;
                bool currentlyRunning = false;

                lock (_lock)
                {
                    if (_gameProcess == null || _gameProcess.HasExited)
                    {
                        _gameProcess = GetGameProcess();
                    }
                    currentlyRunning = _gameProcess != null && !_gameProcess.HasExited;
                }

                if (runningBefore != currentlyRunning)
                {
                    GameRunningChanged?.Invoke(currentlyRunning);
                    if (!currentlyRunning)
                    {
                        CloseLogFile();
                        lastLogPath = null;
                    }
                }

                if (currentlyRunning)
                {
                    // Check focus status
                    CheckIfGameFocused();

                    // Watch Game.log
                    string? logPath = GetGameLogPath();
                    if (logPath != null && logPath != lastLogPath)
                    {
                        OpenLogFile(logPath);
                        lastLogPath = logPath;
                    }

                    if (_logReader != null)
                    {
                        ReadLogChanges();
                    }
                }

                await Task.Delay(1000, ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception)
            {
                await Task.Delay(2000, ct);
            }
        }
    }

    private string? GetGameLogPath()
    {
        if (!string.IsNullOrWhiteSpace(CustomGameLogPath) && File.Exists(CustomGameLogPath))
        {
            return CustomGameLogPath;
        }

        lock (_lock)
        {
            if (_gameProcess == null || _gameProcess.HasExited) return null;
            try
            {
                var exePath = _gameProcess.MainModule?.FileName;
                if (exePath == null) return null;
                // StarCitizen.exe is in .../<VERSION>/Bin64/StarCitizen.exe
                var bin64Dir = Path.GetDirectoryName(exePath);
                if (bin64Dir == null) return null;
                var versionDir = Path.GetDirectoryName(bin64Dir);
                if (versionDir == null) return null;
                var logPath = Path.Combine(versionDir, "Game.log");
                if (File.Exists(logPath))
                {
                    return logPath;
                }
            }
            catch { }
            return null;
        }
    }

    private void OpenLogFile(string path)
    {
        CloseLogFile();
        try
        {
            _logStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            _logReader = new StreamReader(_logStream, System.Text.Encoding.UTF8);
            _logStream.Seek(0, SeekOrigin.End); // Start from the end of the log to capture only live entries
            _lastLogSize = _logStream.Length;
        }
        catch { }
    }

    private void CloseLogFile()
    {
        _logReader?.Dispose();
        _logReader = null;
        _logStream?.Dispose();
        _logStream = null;
    }

    private void ReadLogChanges()
    {
        if (_logStream == null || _logReader == null) return;
        try
        {
            long currentSize = _logStream.Length;
            if (currentSize < _lastLogSize)
            {
                // File was recreated or truncated
                _logStream.Seek(0, SeekOrigin.Begin);
                _lastLogSize = currentSize;
            }

            string? line;
            while ((line = _logReader.ReadLine()) != null)
            {
                ProcessLogLine(line);
            }

            _lastLogSize = _logStream.Position;
        }
        catch { }
    }

    private void ProcessLogLine(string line)
    {
        // 1. Check for position log line (GRTPR)
        var posMatch = PosRegex.Match(line);
        if (posMatch.Success)
        {
            static double ConvertUnit(string val, string unit)
            {
                if (!double.TryParse(val, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double d)) return 0;
                return unit.ToLower() switch
                {
                    "km" => d * 1000.0,
                    _ => d,
                };
            }

            var pos = new PlayerPosition
            {
                Zone = posMatch.Groups[1].Value.Trim(),
                X = ConvertUnit(posMatch.Groups[2].Value, posMatch.Groups[3].Value),
                Y = ConvertUnit(posMatch.Groups[4].Value, posMatch.Groups[5].Value),
                Z = ConvertUnit(posMatch.Groups[6].Value, posMatch.Groups[7].Value),
                TsCapture = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0
            };

            PositionReceived?.Invoke(pos);
            return;
        }

        // 2. Check for helmet attachment log line
        var match = HelmetRegex.Match(line);
        if (match.Success)
        {
            string att = match.Groups["att"].Value.ToLower();
            if (att.Contains("helmet"))
            {
                string port = match.Groups["port"].Value;
                if (!att.StartsWith("fp_visor") && !port.ToLower().Contains("visor"))
                {
                    bool helmetOn = (port == "Armor_Helmet");
                    HelmetStateChanged?.Invoke(helmetOn);
                    return;
                }
            }
        }

        // 3. Check for G-Force warning/blackout/redout/level
        if (line.Contains("g-force", StringComparison.OrdinalIgnoreCase) || line.Contains("gforce", StringComparison.OrdinalIgnoreCase))
        {
            var gMatch = GForceRegex.Match(line);
            if (gMatch.Success && double.TryParse(gMatch.Groups["gval"].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double gVal))
            {
                double intensity = Math.Clamp((gVal - 3.0) / 6.0, 0.0, 1.0);
                GForceReceived?.Invoke(intensity);
            }
            else if (line.Contains("blackout", StringComparison.OrdinalIgnoreCase) || line.Contains("redout", StringComparison.OrdinalIgnoreCase) || line.Contains("severe", StringComparison.OrdinalIgnoreCase) || line.Contains("critical", StringComparison.OrdinalIgnoreCase))
            {
                GForceReceived?.Invoke(1.0);
            }
            else if (line.Contains("warning", StringComparison.OrdinalIgnoreCase))
            {
                GForceReceived?.Invoke(0.5);
            }
            else if (line.Contains("normal", StringComparison.OrdinalIgnoreCase) || line.Contains("recovery", StringComparison.OrdinalIgnoreCase))
            {
                GForceReceived?.Invoke(0.0);
            }
        }

        // 4. Check for Stamina / Exertion / Panting
        if (line.Contains("stamina", StringComparison.OrdinalIgnoreCase) || line.Contains("exertion", StringComparison.OrdinalIgnoreCase) || line.Contains("exhausted", StringComparison.OrdinalIgnoreCase))
        {
            var sMatch = StaminaRegex.Match(line);
            if (sMatch.Success && double.TryParse(sMatch.Groups["val"].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double val))
            {
                if (val > 1.0) val /= 100.0; // Assume percentage if > 1.0
                double exertion = Math.Clamp(1.0 - val, 0.0, 1.0);
                ExertionReceived?.Invoke(exertion);
            }
            else if (line.Contains("depleted", StringComparison.OrdinalIgnoreCase) || line.Contains("exhausted", StringComparison.OrdinalIgnoreCase))
            {
                ExertionReceived?.Invoke(1.0);
            }
            else if (line.Contains("low", StringComparison.OrdinalIgnoreCase) || line.Contains("warning", StringComparison.OrdinalIgnoreCase))
            {
                ExertionReceived?.Invoke(0.5);
            }
            else if (line.Contains("normal", StringComparison.OrdinalIgnoreCase) || line.Contains("recovered", StringComparison.OrdinalIgnoreCase))
            {
                ExertionReceived?.Invoke(0.0);
            }
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        CloseLogFile();
        _gameProcess?.Dispose();
    }
}
