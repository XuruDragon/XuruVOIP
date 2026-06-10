using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Whisper.net;

namespace XuruVoipClient.Services;

public class SpeechToTextService : IDisposable
{
    private static readonly string ModelDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "XuruVoip", "models");

    private readonly string _modelPath = Path.Combine(ModelDir, "ggml-tiny.bin");
    private const string ModelUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-tiny.bin";

    private WhisperFactory? _factory;
    private readonly object _factoryLock = new();
    private bool _isDisposed;

    // Download state properties
    public bool IsDownloading { get; private set; }
    public double DownloadProgress { get; private set; }
    public string DownloadStatusText { get; private set; } = "";

    public event Action<double, string>? DownloadProgressChanged;
    public event Action? DownloadCompleted;
    public event Action<string>? DownloadFailed;

    // Transcription output event: (playerName, text, channelType)
    public event Action<string, string, byte>? CaptionDecoded;

    public bool IsModelReady => File.Exists(_modelPath);

    public async Task EnsureModelDownloadedAsync()
    {
        if (IsModelReady)
        {
            LogService.Info("Whisper model is already present at: " + _modelPath);
            return;
        }

        if (IsDownloading) return;

        IsDownloading = true;
        DownloadProgress = 0;
        DownloadStatusText = "Initializing download...";
        DownloadProgressChanged?.Invoke(0, DownloadStatusText);

        try
        {
            Directory.CreateDirectory(ModelDir);
            LogService.Info($"Downloading Whisper STT model from: {ModelUrl}");

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
            using var response = await httpClient.GetAsync(ModelUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(_modelPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            long bytesReadTotal = 0;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                bytesReadTotal += bytesRead;

                if (totalBytes > 0)
                {
                    DownloadProgress = (double)bytesReadTotal / totalBytes * 100.0;
                    DownloadStatusText = $"Downloading: {DownloadProgress:F1}% ({bytesReadTotal / 1024 / 1024}MB / {totalBytes / 1024 / 1024}MB)";
                    DownloadProgressChanged?.Invoke(DownloadProgress, DownloadStatusText);
                }
            }

            LogService.Info("Whisper STT model download complete: " + _modelPath);
            IsDownloading = false;
            DownloadCompleted?.Invoke();
        }
        catch (Exception ex)
        {
            IsDownloading = false;
            DownloadStatusText = "Download failed";
            LogService.Error("Failed to download Whisper STT model", ex);
            
            try
            {
                if (File.Exists(_modelPath)) File.Delete(_modelPath);
            }
            catch { }

            DownloadFailed?.Invoke(ex.Message);
        }
    }

    private void InitializeFactory()
    {
        lock (_factoryLock)
        {
            if (_factory != null) return;

            if (!IsModelReady)
            {
                throw new FileNotFoundException("Whisper model file not found. Download it first.");
            }

            LogService.Info("Loading Whisper model into memory: " + _modelPath);
            _factory = WhisperFactory.FromPath(_modelPath);
        }
    }

    public void QueueTranscription(string playerName, float[] samples, byte channelType, string languageCode)
    {
        if (!IsModelReady) return;

        // Run transcription on a thread-pool background thread
        Task.Run(async () =>
        {
            try
            {
                if (_factory == null)
                {
                    InitializeFactory();
                }

                string whisperLang = GetWhisperLanguageCode(languageCode);
                
                using var processor = _factory!.CreateBuilder()
                    .WithLanguage(whisperLang)
                    .Build();

                var text = "";
                await foreach (var segment in processor.ProcessAsync(samples))
                {
                    text += segment.Text;
                }

                text = text.Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    LogService.Info($"STT [{playerName}] ({whisperLang}): \"{text}\"");
                    CaptionDecoded?.Invoke(playerName, text, channelType);
                }
            }
            catch (Exception ex)
            {
                LogService.Error($"Error transcribing audio for {playerName}", ex);
            }
        });
    }

    private string GetWhisperLanguageCode(string appLang)
    {
        if (string.IsNullOrEmpty(appLang)) return "en";
        appLang = appLang.ToLowerInvariant();
        if (appLang.StartsWith("pt")) return "pt";
        if (appLang.StartsWith("zh")) return "zh";
        return appLang;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        lock (_factoryLock)
        {
            _factory?.Dispose();
            _factory = null;
        }
    }
}
