using System;
using System.IO;
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

        try
        {
            base.OnStartup(e);

            // Initialize ViewModel (loads configurations and initializes services)
            ViewModel = new MainViewModel();
            LogService.EnableGeneralLogs = ViewModel.Config.Config.EnableGeneralLogs;

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
