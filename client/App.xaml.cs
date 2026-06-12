using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using XuruVoipClient.Services;
using XuruVoipClient.ViewModels;

namespace XuruVoipClient;

public partial class App : Application
{
    public static MainViewModel ViewModel { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        // Register exception handlers first to catch any startup crashes
        AppDomain.CurrentDomain.UnhandledException += (s, args) => HandleCrash(args.ExceptionObject as Exception);
        DispatcherUnhandledException += (s, args) => { HandleCrash(args.Exception); args.Handled = true; };
        TaskScheduler.UnobservedTaskException += (s, args) => { HandleCrash(args.Exception); args.SetObserved(); };

        // Perform log rotation at application startup
        LogService.RotateLogs();

        // Register native DLL resolver for WebRtcVadSharp to support single-file publish
        NativeLibrary.SetDllImportResolver(typeof(WebRtcVadSharp.WebRtcVad).Assembly, (libraryName, assembly, searchPath) =>
        {
            if (libraryName == "WebRtcVad" || libraryName == "WebRtcVad.dll")
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                
                // 1. Try runtimes/win-x64/native/WebRtcVad.dll
                string nativePath = Path.Combine(baseDir, "runtimes", "win-x64", "native", "WebRtcVad.dll");
                if (File.Exists(nativePath))
                {
                    if (NativeLibrary.TryLoad(nativePath, out var handle))
                    {
                        return handle;
                    }
                }

                // 2. Try app root
                string rootPath = Path.Combine(baseDir, "WebRtcVad.dll");
                if (File.Exists(rootPath))
                {
                    if (NativeLibrary.TryLoad(rootPath, out var handle))
                    {
                        return handle;
                    }
                }
            }
            return IntPtr.Zero;
        });

        // Register native DLL resolver for Tesseract to support single-file publish
        NativeLibrary.SetDllImportResolver(typeof(Tesseract.TesseractEngine).Assembly, (libraryName, assembly, searchPath) =>
        {
            if (libraryName.StartsWith("tesseract", StringComparison.OrdinalIgnoreCase) ||
                libraryName.StartsWith("leptonica", StringComparison.OrdinalIgnoreCase) ||
                libraryName.Contains("lept", StringComparison.OrdinalIgnoreCase))
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string arch = Environment.Is64BitProcess ? "x64" : "x86";

                // 1. Try x64/x86 subfolder
                string nativePath = Path.Combine(baseDir, arch, libraryName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ? libraryName : $"{libraryName}.dll");
                if (File.Exists(nativePath))
                {
                    if (NativeLibrary.TryLoad(nativePath, out var handle))
                    {
                        return handle;
                    }
                }

                // 1b. In case libraryName doesn't end with suffix or is slightly different, try exact expected filenames
                if (libraryName.Contains("lept", StringComparison.OrdinalIgnoreCase))
                {
                    string leptPath = Path.Combine(baseDir, arch, "leptonica-1.82.0.dll");
                    if (File.Exists(leptPath))
                    {
                        if (NativeLibrary.TryLoad(leptPath, out var handle))
                        {
                            return handle;
                        }
                    }
                }
                else if (libraryName.StartsWith("tesseract", StringComparison.OrdinalIgnoreCase))
                {
                    string tessPath = Path.Combine(baseDir, arch, "tesseract50.dll");
                    if (File.Exists(tessPath))
                    {
                        if (NativeLibrary.TryLoad(tessPath, out var handle))
                        {
                            return handle;
                        }
                    }
                }

                // 2. Try app root directory
                string rootPath = Path.Combine(baseDir, libraryName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ? libraryName : $"{libraryName}.dll");
                if (File.Exists(rootPath))
                {
                    if (NativeLibrary.TryLoad(rootPath, out var handle))
                    {
                        return handle;
                    }
                }
            }
            return IntPtr.Zero;
        });

        try
        {
            base.OnStartup(e);

            // Initialize ViewModel (loads configurations and initializes services)
            ViewModel = new MainViewModel();
            LogService.EnableGeneralLogs = ViewModel.Config.Config.EnableGeneralLogs;

            // Initialize theme colors globally in WPF Client
            ThemeManager.ApplyTheme(ViewModel.Config.Config.HudTheme);

            // Load and apply language
            string lang = ViewModel.Config.Config.Language;
            if (string.IsNullOrEmpty(lang))
            {
                lang = GetSystemLanguage();
                ViewModel.Config.Config.Language = lang;
                ViewModel.SaveConfig();
            }
            SetLanguage(lang);
            ViewModel.RefreshLocalizedStrings();

            LogService.Info("Application startup sequence initiated.");

            // Show SplashWindow first
            var splash = new XuruVoipClient.Views.SplashWindow();
            splash.Show();

            // Wait for 5 seconds (allows splash screen animation to play)
            await Task.Delay(5000);

            // Show MainWindow
            var main = new MainWindow();
            Application.Current.MainWindow = main;
            main.Show();

            // Close the SplashWindow
            splash.Close();
            LogService.Info("Application startup sequence completed successfully.");
        }
        catch (Exception ex)
        {
            HandleCrash(ex);
        }
    }

    public static void SetLanguage(string langCode)
    {
        var app = (App)Application.Current;
        ResourceDictionary? oldDict = null;

        foreach (var dict in app.Resources.MergedDictionaries)
        {
            if (dict.Source != null && dict.Source.OriginalString.Contains("/Resources/Strings."))
            {
                oldDict = dict;
                break;
            }
        }

        var newDict = new ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/XuruVoipClient;component/Resources/Strings.{langCode}.xaml", UriKind.Absolute)
        };

        if (oldDict != null)
        {
            int index = app.Resources.MergedDictionaries.IndexOf(oldDict);
            app.Resources.MergedDictionaries[index] = newDict;
        }
        else
        {
            app.Resources.MergedDictionaries.Add(newDict);
        }
    }

    private static string GetSystemLanguage()
    {
        try
        {
            var culture = System.Globalization.CultureInfo.CurrentUICulture;
            string name = culture.Name;

            if (name.Equals("pt-BR", StringComparison.OrdinalIgnoreCase)) return "pt-BR";
            if (name.Equals("pt-PT", StringComparison.OrdinalIgnoreCase)) return "pt-PT";

            string twoLetter = culture.TwoLetterISOLanguageName.ToLowerInvariant();
            if (twoLetter == "en" || twoLetter == "fr" || twoLetter == "de" || twoLetter == "es" || twoLetter == "ja" || twoLetter == "zh")
            {
                return twoLetter;
            }

            if (twoLetter == "pt")
            {
                if (name.StartsWith("pt-BR", StringComparison.OrdinalIgnoreCase))
                    return "pt-BR";
                return "pt-PT";
            }
        }
        catch
        {
            // Fallback
        }
        return "en";
    }

    private int _isCrashing = 0;

    private void HandleCrash(Exception? ex)
    {
        if (ex == null) return;
        
        // Ensure HandleCrash only runs once to avoid multiple stacked dialogs
        if (System.Threading.Interlocked.Exchange(ref _isCrashing, 1) != 0) return;
        
        LogService.Crash(ex);

        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "XuruVoip", 
            "crash.log");

        try
        {
            MessageBox.Show(
                $"An unexpected crash occurred.\n\n" +
                $"Error: {ex.Message}\n\n" +
                $"A crash log has been saved to:\n{logPath}\n\n" +
                $"Please send this file to the developer to report this issue.", 
                "XuruVoip - Critical Error", 
                MessageBoxButton.OK, 
                MessageBoxImage.Error);
        }
        catch { /* Non-critical if UI is dead */ }

        Environment.Exit(1);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            LogService.Info("Application exiting.");
            if (ViewModel != null)
            {
                ViewModel.Disconnect();
                ViewModel.SaveConfig();
                await ViewModel.DisposeAsync();
            }
        }
        catch (Exception ex)
        {
            LogService.Error("Error during application exit", ex);
        }
        base.OnExit(e);
    }
}
