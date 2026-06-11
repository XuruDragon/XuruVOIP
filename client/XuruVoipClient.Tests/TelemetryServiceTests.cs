using Xunit;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using XuruVoipClient.Services;
using XuruVoipClient.ViewModels;
using XuruVoipClient.Models;

namespace XuruVoipClient.Tests;

public class TelemetryServiceTests
{
    static TelemetryServiceTests()
    {
        UiTests.InitializeWpfApplication();
    }

    [StaFact]
    public async Task TelemetryService_ShouldBroadcastUdpPackets()
    {
        // GIVEN
        string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");
        string originalPath = ConfigService.ConfigPath;
        ConfigService.ConfigPath = tempFile;

        // Enable telemetry on a custom test port
        int testPort = 18895;
        var appConfig = new AppConfig
        {
            EnableTelemetry = true,
            TelemetryPort = testPort,
            Username = "TelemetryTester"
        };
        File.WriteAllText(tempFile, JsonSerializer.Serialize(appConfig));

        using var receivingUdpClient = new UdpClient(testPort);
        receivingUdpClient.Client.ReceiveTimeout = 2000; // 2s timeout

        try
        {
            await using var vm = new MainViewModel();
            Assert.Equal("TelemetryTester", vm.Config.Config.Username);

            // The TelemetryService is started inside vm.InitializeServicesAsync() if EnableTelemetry is true.
            // Let's listen for a broadcast
            var receiveTask = receivingUdpClient.ReceiveAsync();
            var completedTask = await Task.WhenAny(receiveTask, Task.Delay(1500));

            if (completedTask == receiveTask)
            {
                var result = receiveTask.Result;
                string jsonString = Encoding.UTF8.GetString(result.Buffer);
                
                using var doc = JsonDocument.Parse(jsonString);
                var root = doc.RootElement;

                Assert.Equal("TelemetryTester", root.GetProperty("Username").GetString());
                Assert.False(root.GetProperty("IsTransmittingProximity").GetBoolean());
                Assert.False(root.GetProperty("IsTransmittingRadio").GetBoolean());
                Assert.False(root.GetProperty("HelmetVisorDown").GetBoolean());
            }
            else
            {
                Assert.Fail("Timed out waiting for Telemetry UDP broadcast");
            }
        }
        finally
        {
            receivingUdpClient.Close();
            ConfigService.ConfigPath = originalPath;
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
