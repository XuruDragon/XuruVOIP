using Xunit;
using System;
using System.IO;
using System.Reflection;
using XuruVoipClient.Models;
using XuruVoipClient.Services;

namespace XuruVoipClient.Tests;

public class ConfigTests
{
    [Fact]
    public void AppConfig_DefaultLanguage_ShouldBeEmpty()
    {
        var config = new AppConfig();
        Assert.Equal("", config.Language);
    }

    [Fact]
    public void AppConfig_DefaultServerAddress_ShouldBeLocalhost()
    {
        var config = new AppConfig();
        Assert.Equal("127.0.0.1", config.ServerAddress);
    }

    [Fact]
    public void AppConfig_DefaultCustomGameLogPath_ShouldBeEmpty()
    {
        var config = new AppConfig();
        Assert.Equal("", config.CustomGameLogPath);
    }

    [Theory]
    [InlineData("ws://localhost:8888/", "127.0.0.1")]
    [InlineData("wss://my-server.com:443/voip", "my-server.com")]
    [InlineData("  192.168.1.100  ", "192.168.1.100")]
    [InlineData("localhost", "127.0.0.1")]
    [InlineData("10.0.0.5", "10.0.0.5")]
    public void ConfigService_Load_ShouldCleanServerAddressAndMigrateOcrHeight(string rawAddress, string expectedAddress)
    {
        // GIVEN: Setup temp file and override ConfigPath in ConfigService
        string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");
        string originalPath = ConfigService.ConfigPath;
        ConfigService.ConfigPath = tempFile;

        try
        {
            // Create a config json with a too-small OcrRegion height and the test address
            string json = $@"{{
                ""ServerAddress"": ""{rawAddress}"",
                ""OcrRegion"": {{
                    ""X"": 10,
                    ""Y"": 20,
                    ""Width"": 100,
                    ""Height"": 50
                }}
            }}";
            File.WriteAllText(tempFile, json);

            // WHEN
            var service = new ConfigService();
            service.Load();

            // THEN
            Assert.Equal(expectedAddress, service.Config.ServerAddress);
            Assert.Equal(200, service.Config.OcrRegion.Height); // Migrated from 50 to 200
        }
        finally
        {
            // CLEANUP
            ConfigService.ConfigPath = originalPath;
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
