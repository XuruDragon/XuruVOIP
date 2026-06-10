using System;
using System.IO;

namespace XuruVoipClient.Services;

public static class LogService
{
    private static readonly string LogDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "XuruVoip");
    private static readonly string LogPath = Path.Combine(LogDir, "xuru_voip.log");
    private static readonly string CrashLogPath = Path.Combine(LogDir, "crash.log");
    private static readonly object LogLock = new();

    public static bool EnableGeneralLogs { get; set; } = false;

    internal static long MaxLogSizeBytes { get; set; } = 100 * 1024 * 1024; // 100 MB default

    public static void RotateLogs()
    {
        RotateLogsInternal(LogDir, LogPath);
    }

    internal static void RotateLogsInternal(string logDir, string logPath)
    {
        try
        {
            if (!File.Exists(logPath)) return;

            var fi = new FileInfo(logPath);
            DateTime lastWrite = fi.LastWriteTime;
            DateTime today = DateTime.Today;

            // Check if the date is anterior to the actual date (do not compare time)
            if (lastWrite.Date < today)
            {
                lock (LogLock)
                {
                    if (File.Exists(logPath))
                    {
                        var innerFi = new FileInfo(logPath);
                        if (innerFi.LastWriteTime.Date < today)
                        {
                            RotateActiveLog(logDir, logPath, innerFi.LastWriteTime);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Log rotation failed: {ex.Message}");
        }
    }

    internal static void RotateActiveLog(string logDir, string logPath, DateTime logDate)
    {
        try
        {
            Directory.CreateDirectory(logDir);
            string dateStr = logDate.ToString("yyyy-MM-dd");

            // Find next available file name: xuru_voip.yyyy-MM-dd.log, then xuru_voip.yyyy-MM-dd.1.log, xuru_voip.yyyy-MM-dd.2.log, etc.
            string rotatedPath = Path.Combine(logDir, $"xuru_voip.{dateStr}.log");
            int counter = 1;
            while (File.Exists(rotatedPath))
            {
                rotatedPath = Path.Combine(logDir, $"xuru_voip.{dateStr}.{counter}.log");
                counter++;
            }

            File.Move(logPath, rotatedPath);
            EnforceRotatedLogsLimit(logDir);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to rotate active log file: {ex.Message}");
        }
    }

    internal static (string DateStr, int Index) ParseLogFileName(string name)
    {
        // Name format is: xuru_voip.yyyy-MM-dd.log or xuru_voip.yyyy-MM-dd.counter.log
        if (name.StartsWith("xuru_voip.") && name.EndsWith(".log"))
        {
            string middle = name.Substring(10, name.Length - 14); // length of "xuru_voip." is 10, ".log" is 4
            string[] parts = middle.Split('.');
            if (parts.Length == 1)
            {
                return (parts[0], 0); // No counter, so index 0
            }
            if (parts.Length == 2 && int.TryParse(parts[1], out int index))
            {
                return (parts[0], index);
            }
        }
        return (name, 0);
    }

    internal static void EnforceRotatedLogsLimit(string logDir)
    {
        try
        {
            var logFiles = Directory.GetFiles(logDir, "xuru_voip.*.log");
            var rotatedLogsList = new System.Collections.Generic.List<FileInfo>();
            foreach (var file in logFiles)
            {
                var name = Path.GetFileName(file);
                if (name != "xuru_voip.log" && name != "crash.log")
                {
                    rotatedLogsList.Add(new FileInfo(file));
                }
            }

            // Sort chronologically using date name and index suffix to avoid timestamp caching issues
            rotatedLogsList.Sort((a, b) =>
            {
                var parsedA = ParseLogFileName(a.Name);
                var parsedB = ParseLogFileName(b.Name);

                int dateCompare = string.Compare(parsedA.DateStr, parsedB.DateStr, StringComparison.Ordinal);
                if (dateCompare != 0) return dateCompare;

                return parsedA.Index.CompareTo(parsedB.Index);
            });

            while (rotatedLogsList.Count > 5)
            {
                var oldest = rotatedLogsList[0];
                try
                {
                    oldest.Delete();
                }
                catch { /* Ignore deletion failure of a single file */ }
                rotatedLogsList.RemoveAt(0);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to enforce rotated logs limit: {ex.Message}");
        }
    }

    public static void Info(string message)
    {
        if (!EnableGeneralLogs) return;
        WriteLog("INFO", message);
    }

    public static void Error(string message, Exception? ex = null)
    {
        WriteLog("ERROR", $"{message}{(ex != null ? $"\nException: {ex.Message}\nStack: {ex.StackTrace}" : "")}");
    }

    public static void Crash(Exception ex)
    {
        try
        {
            Directory.CreateDirectory(LogDir);
            lock (LogLock)
            {
                var crashMsg = $"========================================\n" +
                               $"CRASH DETECTED: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\n" +
                               $"Exception: {ex.GetType().FullName} - {ex.Message}\n" +
                               $"Stack Trace:\n{ex.StackTrace}\n";
                if (ex.InnerException != null)
                {
                    crashMsg += $"Inner Exception: {ex.InnerException.GetType().FullName} - {ex.InnerException.Message}\n" +
                                $"Inner Stack Trace:\n{ex.InnerException.StackTrace}\n";
                }
                crashMsg += "========================================\n\n";

                File.AppendAllText(CrashLogPath, crashMsg);
                
                // Write crash to general log as well (regardless of EnableGeneralLogs setting to ensure a complete trace)
                File.AppendAllText(LogPath, crashMsg);
            }
        }
        catch { /* Ignore logging failures during crash */ }
    }

    private static void WriteLog(string level, string message)
    {
        try
        {
            Directory.CreateDirectory(LogDir);
            lock (LogLock)
            {
                if (File.Exists(LogPath))
                {
                    var fi = new FileInfo(LogPath);
                    if (fi.Length >= MaxLogSizeBytes)
                    {
                        RotateActiveLog(LogDir, LogPath, fi.LastWriteTime);
                    }
                }

                var formatted = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}\n";
                File.AppendAllText(LogPath, formatted);
            }
        }
        catch { /* Non-critical */ }
    }
}
