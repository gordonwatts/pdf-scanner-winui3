using System.Diagnostics;
using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.UI.Xaml;

namespace IndicoPenSpike;

public partial class App : Application
{
    private static readonly StartupLogger Logger = new();
    private Window? _window;

    public App()
    {
        UnhandledException += App_UnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Logger.WriteStartupBanner(args);

        try
        {
            _window = new MainWindow();
            _window.Activate();

            if (IsSmokeTestRequested(args) && _window is MainWindow mainWindow)
            {
                mainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    try
                    {
                        mainWindow.RunSmokeTest();
                    }
                    finally
                    {
                        mainWindow.Close();
                        Exit();
                    }
                });
            }
        }
        catch (Exception ex)
        {
            Logger.WriteException("OnLaunched", ex);
            throw;
        }
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        Logger.WriteException("WinUI UnhandledException", e.Exception);
    }

    private void CurrentDomain_UnhandledException(object? sender, System.UnhandledExceptionEventArgs e)
    {
        Logger.WriteObject("AppDomain.CurrentDomain.UnhandledException", e.ExceptionObject);
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Logger.WriteException("TaskScheduler.UnobservedTaskException", e.Exception);
        e.SetObserved();
    }

    private static bool IsSmokeTestRequested(LaunchActivatedEventArgs args)
    {
        if (args.Arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains("--smoke-test"))
        {
            return true;
        }

        return Environment.GetCommandLineArgs().Contains("--smoke-test");
    }
}

internal sealed class StartupLogger
{
    private readonly object _gate = new();
    private readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "IndicoPenSpike",
        "startup.log");

    public void WriteStartupBanner(LaunchActivatedEventArgs args)
    {
        var entry = new StringBuilder();
        entry.AppendLine("==== Startup ====");
        entry.AppendLine($"Timestamp (UTC): {DateTimeOffset.UtcNow:O}");
        entry.AppendLine($"Args: {args.Arguments}");
        entry.AppendLine($"ProcessArchitecture: {RuntimeInformation.ProcessArchitecture}");
        entry.AppendLine($"OSArchitecture: {RuntimeInformation.OSArchitecture}");
        entry.AppendLine($"FrameworkDescription: {RuntimeInformation.FrameworkDescription}");
        entry.AppendLine($"AppContext.BaseDirectory: {AppContext.BaseDirectory}");
        entry.AppendLine($"CurrentDirectory: {Environment.CurrentDirectory}");
        entry.AppendLine($"Assembly: {Assembly.GetExecutingAssembly().Location}");
        Write(entry.ToString());
    }

    public void WriteException(string source, Exception exception)
    {
        var entry = new StringBuilder();
        entry.AppendLine($"==== {source} ====");
        entry.AppendLine($"Timestamp (UTC): {DateTimeOffset.UtcNow:O}");
        entry.AppendLine(exception.ToString());
        entry.AppendLine("-- Exception Details --");
        entry.AppendLine($"Type: {exception.GetType().FullName}");
        entry.AppendLine($"HResult: 0x{exception.HResult:X8}");
        entry.AppendLine($"Source: {exception.Source}");
        DumpPublicProperties(entry, exception);
        DumpExceptionData(entry, exception);
        Write(entry.ToString());
    }

    public void WriteObject(string source, object? value)
    {
        var entry = new StringBuilder();
        entry.AppendLine($"==== {source} ====");
        entry.AppendLine($"Timestamp (UTC): {DateTimeOffset.UtcNow:O}");
        entry.AppendLine(value?.ToString() ?? "<null>");
        Write(entry.ToString());
    }

    private void Write(string text)
    {
        try
        {
            lock (_gate)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_logPath)!);
                File.AppendAllText(_logPath, text + Environment.NewLine);
            }
        }
        catch
        {
            Debug.WriteLine(text);
        }
    }

    private static void DumpPublicProperties(StringBuilder entry, Exception exception)
    {
        var type = exception.GetType();
        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.GetIndexParameters().Length != 0 || !property.CanRead)
            {
                continue;
            }

            object? value;
            try
            {
                value = property.GetValue(exception);
            }
            catch (Exception readException)
            {
                value = $"<error: {readException.GetType().Name}>";
            }

            if (value is null)
            {
                continue;
            }

            if (value is Exception nestedException)
            {
                entry.AppendLine($"{property.Name}: {nestedException.GetType().FullName}: {nestedException.Message}");
                continue;
            }

            entry.AppendLine($"{property.Name}: {value}");
        }
    }

    private static void DumpExceptionData(StringBuilder entry, Exception exception)
    {
        if (exception.Data.Count == 0)
        {
            return;
        }

        entry.AppendLine("-- Exception Data --");
        foreach (DictionaryEntry item in exception.Data)
        {
            entry.AppendLine($"{item.Key}: {item.Value}");
        }
    }
}
