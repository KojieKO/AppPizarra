using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using SukiUI;
using SukiUI.Enums;
using Avalonia.Markup.Xaml.Styling;

namespace PizarrasPro;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        try { BuildAvaloniaApp().StartWithClassicDesktopLifetime(args); }
        catch (Exception ex) { AppLog.Write(ex.ToString()); throw; }
    }
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();
}

public sealed class App : Application
{
    public override void Initialize()
    {
        RequestedThemeVariant = ThemeVariant.Light;
        // SukiUI 6.1.1 already includes SimpleTheme and its DataGrid styles.
        Styles.Add(new SukiTheme { ThemeColor = SukiColor.Blue });
        Styles.Add(new StyleInclude(new Uri("avares://PizarrasPro/")) { Source = new Uri("avares://PizarrasPro/UiResources.axaml") });
    }
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) desktop.MainWindow = new MainWindow();
        base.OnFrameworkInitializationCompleted();
    }
}

public static class AppLog
{
    public static string Path => System.IO.Path.Combine(AppContext.BaseDirectory, "registros", "pizarras.log");
    private static readonly object Gate = new();
    public static void Write(string message)
    {
        lock (Gate) try
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
            if (File.Exists(Path) && new FileInfo(Path).Length > 2_000_000) File.Move(Path, Path + ".anterior", true);
            File.AppendAllText(Path, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}{Environment.NewLine}");
        }
        catch { /* A read-only USB must not cause recursive error reporting. */ }
    }
}
