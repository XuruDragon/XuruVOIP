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
