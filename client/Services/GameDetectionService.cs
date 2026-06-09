using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

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

    // Regex for helmet attachment:
    // <AttachmentReceived> Player[name] Attachment[..., helmet, ...] Status[...] Port[Armor_Helmet]
    private static readonly Regex HelmetRegex = new(
        @"<AttachmentReceived>\s+Player\[(?<player>[^\]]+)\]\s+Attachment\[(?<att>[^\]]+)\]\s+Status\[(?<status>[^\]]+)\]\s+Port\[(?<port>[^\]]+)\]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public event Action<bool>? HelmetStateChanged; // (helmetOn)
    public event Action<bool>? GameFocusChanged; // (isFocused)
    public event Action<bool>? GameRunningChanged; // (isRunning)

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
        var match = HelmetRegex.Match(line);
        if (!match.Success) return;

        string att = match.Groups["att"].Value.ToLower();
        if (!att.Contains("helmet")) return;

        string port = match.Groups["port"].Value;
        if (att.StartsWith("fp_visor") || port.ToLower().Contains("visor")) return;

        bool helmetOn = (port == "Armor_Helmet");
        HelmetStateChanged?.Invoke(helmetOn);
    }

    public void Dispose()
    {
        _cts?.Cancel();
        CloseLogFile();
        _gameProcess?.Dispose();
    }
}
