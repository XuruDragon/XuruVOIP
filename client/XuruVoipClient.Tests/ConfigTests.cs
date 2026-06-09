using Xunit;
using XuruVoipClient.Models;

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
}
