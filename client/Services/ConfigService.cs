using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using XuruVoipClient.Models;

namespace XuruVoipClient.Services;

public class ConfigService
{
    private static readonly string ConfigDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "XuruVoip");

    private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public AppConfig Config { get; private set; } = new();

    public void Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                Config = JsonSerializer.Deserialize<AppConfig>(json, JsonOpts) ?? new AppConfig();
                
                // Clean up ServerAddress if it contains scheme/port or is localhost
                if (!string.IsNullOrEmpty(Config.ServerAddress))
                {
                    string addr = Config.ServerAddress.Trim();
                    if (addr.StartsWith("ws://", StringComparison.OrdinalIgnoreCase))
                        addr = addr.Substring(5);
                    else if (addr.StartsWith("wss://", StringComparison.OrdinalIgnoreCase))
                        addr = addr.Substring(6);

                    int colonIndex = addr.IndexOf(':');
                    if (colonIndex != -1)
                        addr = addr.Substring(0, colonIndex);
                    int slashIndex = addr.IndexOf('/');
                    if (slashIndex != -1)
                        addr = addr.Substring(0, slashIndex);

                    if (addr.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                        addr = "127.0.0.1";

                    Config.ServerAddress = addr;
                }


                // Migration logic for old OcrRegion height
                if (Config.OcrRegion.Height < 150)
                {
                    Config.OcrRegion = new System.Windows.Rect(Config.OcrRegion.X, Config.OcrRegion.Y, Config.OcrRegion.Width, 200);
                }
                
                LogService.Info("Configuration loaded successfully from: " + ConfigPath);
            }
            else
            {
                Config = new AppConfig();
                // Auto-generate HWID on first launch
                Config.Hwid = GetOrCreateHwid();
                Save();
                LogService.Info("No configuration found. Created a default configuration at: " + ConfigPath);
            }
        }
        catch (Exception ex)
        {
            Config = new AppConfig();
            LogService.Error("Failed to load configuration", ex);
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(ConfigDir);
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(Config, JsonOpts));
            LogService.Info("Configuration saved successfully to: " + ConfigPath);
        }
        catch (Exception ex)
        {
            LogService.Error("Failed to save configuration", ex);
        }
    }

    private static string GetOrCreateHwid()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine
                .OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
            return key?.GetValue("MachineGuid") as string
                ?? Guid.NewGuid().ToString();
        }
        catch (Exception ex)
        {
            LogService.Error("Failed to get MachineGuid from Registry, falling back to new Guid", ex);
            return Guid.NewGuid().ToString();
        }
    }

    /// <summary>
    /// Extracts embedded eng.traineddata to %AppData%\XuruVoip\tessdata\ if not already present.
    /// </summary>
    public static string EnsureTessdata()
    {
        var tessDir = Path.Combine(ConfigDir, "tessdata");
        var tessFile = Path.Combine(tessDir, "eng.traineddata");

        if (!File.Exists(tessFile))
        {
            try
            {
                Directory.CreateDirectory(tessDir);

                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                using var stream = asm.GetManifestResourceStream("tessdata/eng.traineddata");
                if (stream == null)
                    throw new FileNotFoundException("Embedded tessdata not found. Rebuild the project with eng.traineddata in Resources/tessdata/.");

                using var fs = File.Create(tessFile);
                stream.CopyTo(fs);
                LogService.Info("Successfully extracted embedded tessdata eng.traineddata to: " + tessFile);
            }
            catch (Exception ex)
            {
                LogService.Error("Failed to extract embedded tessdata", ex);
                throw;
            }
        }

        return tessDir; // Tesseract 5.x takes the tessdata folder itself
    }
}
