using Xunit;
using XuruVoipClient.Services;

namespace XuruVoipClient.Tests;

public class UpdateServiceTests
{
    [Theory]
    [InlineData("v1.1.0", "1.0.0")]
    [InlineData("1.5.0", "v1.4.9")]
    [InlineData("v2.0.0.0", "v1.9.9.9")]
    [InlineData("v10.0", "9.0")]
    public void IsUpdateAvailable_NewerVersionAvailable_ReturnsTrue(string latest, string current)
    {
        // WHEN
        bool result = UpdateService.IsUpdateAvailable(latest, current);

        // THEN
        Assert.True(result);
    }

    [Theory]
    [InlineData("v1.0.0", "1.0.0")]
    [InlineData("1.0.0", "v1.0.0")]
    [InlineData("v0.9.5", "1.0.0")]
    [InlineData("1.2.3", "v1.2.4")]
    [InlineData("v1.0.0.0", "v1.0.0.0")]
    public void IsUpdateAvailable_OlderOrSameVersion_ReturnsFalse(string latest, string current)
    {
        // WHEN
        bool result = UpdateService.IsUpdateAvailable(latest, current);

        // THEN
        Assert.False(result);
    }

    [Theory]
    [InlineData("", "1.0.0")]
    [InlineData("v1.0.0", "")]
    [InlineData("abc", "1.0.0")]
    [InlineData("1.0.0", "xyz")]
    [InlineData(null, "1.0.0")]
    [InlineData("1.0.0", null)]
    public void IsUpdateAvailable_InvalidVersionStrings_ReturnsFalse(string? latest, string? current)
    {
        // WHEN
        bool result = UpdateService.IsUpdateAvailable(latest!, current!);

        // THEN
        Assert.False(result);
    }
}
