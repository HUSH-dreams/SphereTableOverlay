using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Threading;
using Serilog;

namespace DollOverlay;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static readonly ILogger Logger = Log.Logger;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Инициализация Serilog — файлы + Debug (для VS Output window)
        var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "doll_overlay_.log");
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
            .WriteTo.File(
                path: logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Debug()
            .CreateLogger();

        Log.Information("=== DollOverlay запущен ===");

        // 1. Глобальная обработка необработанных ошибок в UI-потоке
        DispatcherUnhandledException += (sender, args) =>
        {
            Log.Error(args.Exception, "Необработанная ошибка в UI-потоке");
            args.Handled = true; // Не даём приложению упасть
            ShowFatalError(args.Exception, "Критическая ошибка в UI-потоке");
        };

        // 2. Обработка необработанных ошибок в фоновых задачах (TaskScheduler)
        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            Log.Error(args.Exception, "Необработанная ошибка в фоновой задаче");
            args.SetObserved(); // Не даём приложению упасть
        };

        // 3. Обработка всех остальных необработанных исключений (фоновые потоки и т.д.)
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            if (ex != null)
            {
                Log.Error(ex, "Необработанная ошибка в AppDomain");
                if (args.IsTerminating)
                {
                    ShowFatalError(ex, "Критическая ошибка — приложение будет закрыто");
                    Log.Warning("Приложение будет аварийно завершено");
                }
            }
        };

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("=== DollOverlay остановлен ===");
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    private static void ShowFatalError(Exception ex, string message)
    {
        try
        {
            // Пытаемся показать MessageBox, но если UI не отвечает — просто логируем
            var safeMessage = $"{message}\n\n{ex.Message}";
            if (Application.Current?.Dispatcher.Thread.IsAlive == true)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(safeMessage, "Критическая ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }
        catch
        {
            // UI не доступен — просто логируем
            Log.Error(ex, "Не удалось показать MessageBox с ошибкой");
        }
    }
}
