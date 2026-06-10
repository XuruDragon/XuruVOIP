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
                Directory.CreateDirectory(logDir);

                // Rename to xuru_voip.yyyy-MM-dd.log using the date of the latest log entries
                string dateStr = lastWrite.ToString("yyyy-MM-dd");
                string rotatedPath = Path.Combine(logDir, $"xuru_voip.{dateStr}.log");

                lock (LogLock)
                {
                    if (File.Exists(rotatedPath))
                    {
                        File.Delete(rotatedPath);
                    }
                    File.Move(logPath, rotatedPath);
                }

                // Keep only the last 5 rotated files
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

                // Sort by Name (lexicographical sorting of yyyy-MM-dd yields chronological order)
                rotatedLogsList.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

                // If we have more than 5, delete the oldest ones (first in the list) until we have 5
                while (rotatedLogsList.Count > 5)
                {
                    var oldest = rotatedLogsList[0];
                    oldest.Delete();
                    rotatedLogsList.RemoveAt(0);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Log rotation failed: {ex.Message}");
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
                var formatted = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}\n";
                File.AppendAllText(LogPath, formatted);
            }
        }
        catch { /* Non-critical */ }
    }
}
