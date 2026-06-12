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
            Assert.Equal("RSI", root.GetProperty("hudTheme").GetString());
            Assert.Equal("TopLeft", root.GetProperty("overlayPosition").GetString());
            Assert.True(root.GetProperty("hudShowRadar").GetBoolean());
            Assert.True(root.GetProperty("hudShowActiveSpeakers").GetBoolean());
            Assert.True(root.GetProperty("hudShowChannel").GetBoolean());
            Assert.Equal("Military", root.GetProperty("pttChimeType").GetString());
            
            // Map telemetry should be null/disabled by default
            Assert.False(root.GetProperty("enableCompanionMap").GetBoolean());
            Assert.Equal(JsonValueKind.Null, root.GetProperty("localPos").ValueKind);
            Assert.Equal(JsonValueKind.Null, root.GetProperty("heading").ValueKind);
            Assert.Equal(JsonValueKind.Null, root.GetProperty("remotePositions").ValueKind);

            // Toggle EnableCompanionMap to true and verify
            vm.Config.Config.EnableCompanionMap = true;
            var statusResponseMap = await client.GetAsync("http://localhost:8891/api/status");
            Assert.True(statusResponseMap.IsSuccessStatusCode);
            string statusJsonMap = await statusResponseMap.Content.ReadAsStringAsync();
            using var docMap = JsonDocument.Parse(statusJsonMap);
            var rootMap = docMap.RootElement;

            Assert.True(rootMap.GetProperty("enableCompanionMap").GetBoolean());
            Assert.Equal(JsonValueKind.Object, rootMap.GetProperty("heading").ValueKind);
            Assert.Equal(0, rootMap.GetProperty("heading").GetProperty("x").GetDouble());
            Assert.Equal(1, rootMap.GetProperty("heading").GetProperty("y").GetDouble());
            Assert.Equal(JsonValueKind.Object, rootMap.GetProperty("remotePositions").ValueKind);

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

            // 4. Test POST HUD customization actions
            var themePayload = new { action = "set_hud_theme", theme = "Anvil" };
            var themeContent = new StringContent(JsonSerializer.Serialize(themePayload), Encoding.UTF8, "application/json");
            var themeRes = await client.PostAsync("http://localhost:8891/api/action", themeContent);
            Assert.True(themeRes.IsSuccessStatusCode);

            var posPayload = new { action = "set_hud_position", position = "BottomRight" };
            var posContent = new StringContent(JsonSerializer.Serialize(posPayload), Encoding.UTF8, "application/json");
            var posRes = await client.PostAsync("http://localhost:8891/api/action", posContent);
            Assert.True(posRes.IsSuccessStatusCode);

            var radarPayload = new { action = "toggle_hud_radar" };
            var radarContent = new StringContent(JsonSerializer.Serialize(radarPayload), Encoding.UTF8, "application/json");
            var radarRes = await client.PostAsync("http://localhost:8891/api/action", radarContent);
            Assert.True(radarRes.IsSuccessStatusCode);

            var spkPayload = new { action = "toggle_hud_speakers" };
            var spkContent = new StringContent(JsonSerializer.Serialize(spkPayload), Encoding.UTF8, "application/json");
            var spkRes = await client.PostAsync("http://localhost:8891/api/action", spkContent);
            Assert.True(spkRes.IsSuccessStatusCode);

            var chanPayload = new { action = "toggle_hud_channel" };
            var chanContent = new StringContent(JsonSerializer.Serialize(chanPayload), Encoding.UTF8, "application/json");
            var chanRes = await client.PostAsync("http://localhost:8891/api/action", chanContent);
            Assert.True(chanRes.IsSuccessStatusCode);

            var chimePayload = new { action = "set_chime_type", type = "Industrial" };
            var chimeContent = new StringContent(JsonSerializer.Serialize(chimePayload), Encoding.UTF8, "application/json");
            var chimeRes = await client.PostAsync("http://localhost:8891/api/action", chimeContent);
            Assert.True(chimeRes.IsSuccessStatusCode);

            // Wait a brief moment for dispatcher to update

            // Verify they are updated on ViewModel config
            Assert.Equal("Anvil", vm.Config.Config.HudTheme);
            Assert.Equal("BottomRight", vm.Config.Config.OverlayPosition);
            Assert.False(vm.Config.Config.HudShowRadar);
            Assert.False(vm.Config.Config.HudShowActiveSpeakers);
            Assert.False(vm.Config.Config.HudShowChannel);
            Assert.Equal("Industrial", vm.Config.Config.PttChimeType);

            // 5. Verify status GET reflects the modified settings
            var statusResponse3 = await client.GetAsync("http://localhost:8891/api/status");
            Assert.True(statusResponse3.IsSuccessStatusCode);
            string statusJson3 = await statusResponse3.Content.ReadAsStringAsync();
            using var doc3 = JsonDocument.Parse(statusJson3);
            var root3 = doc3.RootElement;
            Assert.Equal("Anvil", root3.GetProperty("hudTheme").GetString());
            Assert.Equal("BottomRight", root3.GetProperty("overlayPosition").GetString());
            Assert.False(root3.GetProperty("hudShowRadar").GetBoolean());
            Assert.False(root3.GetProperty("hudShowActiveSpeakers").GetBoolean());
            Assert.False(root3.GetProperty("hudShowChannel").GetBoolean());
            Assert.Equal("Industrial", root3.GetProperty("pttChimeType").GetString());

            // 6. Test POST toggle_voice_commands
            bool initialVoiceCommands = vm.Config.Config.EnableVoiceCommands;
            var voiceCmdPayload = new { action = "toggle_voice_commands" };
            var voiceCmdContent = new StringContent(JsonSerializer.Serialize(voiceCmdPayload), Encoding.UTF8, "application/json");
            var voiceCmdRes = await client.PostAsync("http://localhost:8891/api/action", voiceCmdContent);
            Assert.True(voiceCmdRes.IsSuccessStatusCode);

            // 7. Test POST cycle_hud_theme
            var cycleThemePayload = new { action = "cycle_hud_theme" };
            var cycleThemeContent = new StringContent(JsonSerializer.Serialize(cycleThemePayload), Encoding.UTF8, "application/json");
            var cycleThemeRes = await client.PostAsync("http://localhost:8891/api/action", cycleThemeContent);
            Assert.True(cycleThemeRes.IsSuccessStatusCode);

            // Wait a brief moment for dispatcher to update
            await Task.Delay(150);

            // Verify they are updated on ViewModel config
            Assert.Equal("Drake", vm.Config.Config.HudTheme);
            Assert.Equal(!initialVoiceCommands, vm.Config.Config.EnableVoiceCommands);

            // 8. Verify status GET reflects the modified settings
            var statusResponse4 = await client.GetAsync("http://localhost:8891/api/status");
            Assert.True(statusResponse4.IsSuccessStatusCode);
            string statusJson4 = await statusResponse4.Content.ReadAsStringAsync();
            using var doc4 = JsonDocument.Parse(statusJson4);
            var root4 = doc4.RootElement;
            Assert.Equal("Drake", root4.GetProperty("hudTheme").GetString());
            Assert.Equal(!initialVoiceCommands, root4.GetProperty("voiceCommandsEnabled").GetBoolean());
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
