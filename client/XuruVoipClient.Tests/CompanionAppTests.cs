using Xunit;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using XuruVoipClient.Services;
using XuruVoipClient.ViewModels;
using XuruVoipClient.Models;

namespace XuruVoipClient.Tests;

public class CompanionAppTests
{
    static CompanionAppTests()
    {
        UiTests.InitializeWpfApplication();
    }

    [Fact]
    public void TestConfigServiceDirectly()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");
        string originalPath = ConfigService.ConfigPath;
        ConfigService.ConfigPath = tempFile;

        var appConfig = new AppConfig
        {
            EnableCompanionApp = true,
            Username = "TestPlayer"
        };

        File.WriteAllText(tempFile, JsonSerializer.Serialize(appConfig));

        try
        {
            var configService = new ConfigService();
            configService.Load();
            Assert.Equal("TestPlayer", configService.Config.Username);
        }
        finally
        {
            ConfigService.ConfigPath = originalPath;
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [StaFact]
    public async Task CompanionAppService_ShouldServeStatusAndExecuteActions()
    {
        // GIVEN
        string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");
        string originalPath = ConfigService.ConfigPath;
        ConfigService.ConfigPath = tempFile;

        string json = "{\"EnableCompanionApp\": true, \"Username\": \"TestPlayer\"}";
        File.WriteAllText(tempFile, json);

        try
        {
            await using var vm = new MainViewModel();
            Assert.Equal("TestPlayer", vm.Config.Config.Username);

            // Since CompanionAppService is started in vm constructor (because EnableCompanionApp is true by default),
            // we can just make HTTP requests to the listening server immediately!
            using var client = new HttpClient();

            // 1. Test GET /api/status
            var statusResponse = await client.GetAsync("http://localhost:8891/api/status");
            Assert.True(statusResponse.IsSuccessStatusCode);

            string statusJson = await statusResponse.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(statusJson);
            var root = doc.RootElement;

            Assert.Equal("TestPlayer", root.GetProperty("username").GetString());
            Assert.False(root.GetProperty("micProximityMuted").GetBoolean());

            // 2. Test POST /api/action - Toggle proximity mute
            var actionPayload = new { action = "toggle_proximity_mute" };
            var content = new StringContent(JsonSerializer.Serialize(actionPayload), Encoding.UTF8, "application/json");

            var actionResponse = await client.PostAsync("http://localhost:8891/api/action", content);
            Assert.True(actionResponse.IsSuccessStatusCode);

            // Wait a brief moment for dispatcher to update
            await Task.Delay(150);

            // Verify proximity mute was toggled on ViewModel
            Assert.True(vm.MicProximityMuted);

            // 3. Test GET /api/status shows the updated state
            var statusResponse2 = await client.GetAsync("http://localhost:8891/api/status");
            Assert.True(statusResponse2.IsSuccessStatusCode);
            string statusJson2 = await statusResponse2.Content.ReadAsStringAsync();
            using var doc2 = JsonDocument.Parse(statusJson2);
            Assert.True(doc2.RootElement.GetProperty("micProximityMuted").GetBoolean());
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
