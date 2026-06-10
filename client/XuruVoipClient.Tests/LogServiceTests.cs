using Xunit;
using System;
using System.IO;
using XuruVoipClient.Services;

namespace XuruVoipClient.Tests;

public class LogServiceTests
{
    [Fact]
    public void LogService_RotateLogs_ShouldRotateWhenOlderAndPruneTo5()
    {
        // GIVEN: Set up a temporary directory to act as the log folder
        string tempDir = Path.Combine(Path.GetTempPath(), "XuruVoipTests_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        string testLogPath = Path.Combine(tempDir, "xuru_voip.log");

        try
        {
            // Scenario 1: No file exists, RotateLogs should do nothing and not fail
            LogService.RotateLogsInternal(tempDir, testLogPath);
            Assert.False(File.Exists(testLogPath));

            // Scenario 2: Active file exists but last write date is today, should NOT rotate
            File.WriteAllText(testLogPath, "Today's logs");
            File.SetLastWriteTime(testLogPath, DateTime.Today);
            LogService.RotateLogsInternal(tempDir, testLogPath);
            Assert.True(File.Exists(testLogPath));
            Assert.Empty(Directory.GetFiles(tempDir, "xuru_voip.*.log"));

            // Scenario 3: Active file exists and last write date is yesterday, should rotate
            File.WriteAllText(testLogPath, "Yesterday's logs");
            DateTime yesterday = DateTime.Today.AddDays(-1);
            File.SetLastWriteTime(testLogPath, yesterday);

            LogService.RotateLogsInternal(tempDir, testLogPath);
            Assert.False(File.Exists(testLogPath)); // Rotated away

            string rotatedName = $"xuru_voip.{yesterday:yyyy-MM-dd}.log";
            string rotatedPath = Path.Combine(tempDir, rotatedName);
            Assert.True(File.Exists(rotatedPath));
            Assert.Equal("Yesterday's logs", File.ReadAllText(rotatedPath));

            // Scenario 4: Having more than 5 rotated logs should prune to the last 5
            // Let's create 6 rotated log files from consecutive days in the past
            for (int i = 1; i <= 6; i++)
            {
                DateTime pastDate = DateTime.Today.AddDays(-10 + i);
                string pastRotatedPath = Path.Combine(tempDir, $"xuru_voip.{pastDate:yyyy-MM-dd}.log");
                File.WriteAllText(pastRotatedPath, $"Log from {pastDate:yyyy-MM-dd}");
                File.SetLastWriteTime(pastRotatedPath, pastDate);
            }

            // Create active log file from yesterday again to trigger rotation
            File.WriteAllText(testLogPath, "Trigger log");
            File.SetLastWriteTime(testLogPath, yesterday);

            LogService.RotateLogsInternal(tempDir, testLogPath);

            // Check files in the directory
            var files = Directory.GetFiles(tempDir, "xuru_voip.*.log");
            // There should be exactly 5 rotated files
            Assert.Equal(5, files.Length);

            // With 8 rotated files (6 past files + 1 from Scenario 3 + 1 from Scenario 4),
            // pruning down to 5 will delete the three oldest files (i = 1, i = 2, and i = 3)
            DateTime oldestPermitted = DateTime.Today.AddDays(-10 + 4); // i = 4
            string oldestPermittedPath = Path.Combine(tempDir, $"xuru_voip.{oldestPermitted:yyyy-MM-dd}.log");
            Assert.True(File.Exists(oldestPermittedPath));

            // i = 1 should be pruned
            DateTime prunedDate1 = DateTime.Today.AddDays(-10 + 1); // i = 1
            string prunedPath1 = Path.Combine(tempDir, $"xuru_voip.{prunedDate1:yyyy-MM-dd}.log");
            Assert.False(File.Exists(prunedPath1));

            // i = 2 should also be pruned
            DateTime prunedDate2 = DateTime.Today.AddDays(-10 + 2); // i = 2
            string prunedPath2 = Path.Combine(tempDir, $"xuru_voip.{prunedDate2:yyyy-MM-dd}.log");
            Assert.False(File.Exists(prunedPath2));

            // i = 3 should also be pruned
            DateTime prunedDate3 = DateTime.Today.AddDays(-10 + 3); // i = 3
            string prunedPath3 = Path.Combine(tempDir, $"xuru_voip.{prunedDate3:yyyy-MM-dd}.log");
            Assert.False(File.Exists(prunedPath3));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void LogService_RotateActiveLog_ShouldHandleSizeBasedRotationAndSuffixes()
    {
        // GIVEN: Set up a temporary directory to act as the log folder
        string tempDir = Path.Combine(Path.GetTempPath(), "XuruVoipSizeTests_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        string testLogPath = Path.Combine(tempDir, "xuru_voip.log");
        DateTime testDate = new DateTime(2026, 6, 10);

        try
        {
            // First rotation: should create xuru_voip.2026-06-10.log
            File.WriteAllText(testLogPath, "First chunk");
            LogService.RotateActiveLog(tempDir, testLogPath, testDate);
            Assert.False(File.Exists(testLogPath));
            
            string rotatedName1 = "xuru_voip.2026-06-10.log";
            string rotatedPath1 = Path.Combine(tempDir, rotatedName1);
            Assert.True(File.Exists(rotatedPath1));
            Assert.Equal("First chunk", File.ReadAllText(rotatedPath1));

            // Second rotation on the same day: should create xuru_voip.2026-06-10.1.log
            File.WriteAllText(testLogPath, "Second chunk");
            LogService.RotateActiveLog(tempDir, testLogPath, testDate);
            Assert.False(File.Exists(testLogPath));

            string rotatedName2 = "xuru_voip.2026-06-10.1.log";
            string rotatedPath2 = Path.Combine(tempDir, rotatedName2);
            Assert.True(File.Exists(rotatedPath2));
            Assert.Equal("Second chunk", File.ReadAllText(rotatedPath2));

            // Third rotation on the same day: should create xuru_voip.2026-06-10.2.log
            File.WriteAllText(testLogPath, "Third chunk");
            LogService.RotateActiveLog(tempDir, testLogPath, testDate);
            Assert.False(File.Exists(testLogPath));

            string rotatedName3 = "xuru_voip.2026-06-10.2.log";
            string rotatedPath3 = Path.Combine(tempDir, rotatedName3);
            Assert.True(File.Exists(rotatedPath3));
            Assert.Equal("Third chunk", File.ReadAllText(rotatedPath3));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
