using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using System.Text.Json.Nodes;
using SukiUI;

namespace PizarrasPro;

public enum AppearanceMode { Light, Dark, System }

// One application-wide choice; PDF bitmaps are deliberately outside this service.
public sealed class Appearance : IDisposable
{
    private readonly Configuration config;
    private readonly IPlatformSettings? platform;
    private bool disposed;
    public AppearanceMode Mode { get; private set; }
    public Appearance(Configuration config, IPlatformSettings? platform)
    {
        this.config = config;
        this.platform = platform;
        Mode = ReadMode(config);
        if (platform is not null) platform.ColorValuesChanged += SystemChanged;
        Apply();
    }
    public static AppearanceMode ReadMode(Configuration config) =>
        config.Preferences["appearance"] is JsonValue value && value.TryGetValue<string>(out var text)
            ? text switch { "dark" => AppearanceMode.Dark, "system" => AppearanceMode.System, _ => AppearanceMode.Light }
            : AppearanceMode.Light;

    public void Select(AppearanceMode mode)
    {
        if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        // Commit first: a read-only/corrupt configuration must not pretend to save.
        config.Set("appearance", mode switch { AppearanceMode.Dark => "dark", AppearanceMode.System => "system", _ => "light" });
        Mode = mode;
        Apply();
    }
    private void SystemChanged(object? sender, PlatformColorValues values) => Dispatcher.UIThread.Post(() =>
    {
        if (!disposed && Mode == AppearanceMode.System) Apply();
    });
    private void Apply()
    {
        var dark = Mode == AppearanceMode.Dark || Mode == AppearanceMode.System && platform?.GetColorValues().ThemeVariant == PlatformThemeVariant.Dark;
        var variant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
        SukiTheme.GetInstance().ChangeBaseTheme(variant);
        Application.Current!.RequestedThemeVariant = variant;
    }
    public void Dispose()
    {
        disposed = true;
        if (platform is not null) platform.ColorValuesChanged -= SystemChanged;
    }
}

// Reusable in the future Options page, as well as the current modal window.
public sealed class AppearanceControl : StackPanel
{
    public AppearanceControl(Appearance appearance)
    {
        Spacing = 12;
        Children.Add(new TextBlock { Text = "Apariencia", FontSize = 18 });
        var selector = new ComboBox
        {
            ItemsSource = new[] { "Luminoso", "Oscuro", "Según el sistema" },
            SelectedIndex = (int)appearance.Mode, HorizontalAlignment = HorizontalAlignment.Stretch,
            MinWidth = 230
        };
        Avalonia.Automation.AutomationProperties.SetName(selector, "Apariencia");
        Children.Add(selector);
        Children.Add(new TextBlock { Text = "Los cambios se aplican al instante. Los PDF conservan sus colores originales.", TextWrapping = Avalonia.Media.TextWrapping.Wrap });
        var error = new TextBlock { TextWrapping = Avalonia.Media.TextWrapping.Wrap, IsVisible = false };
        Children.Add(error);
        var restoring = false;
        selector.SelectionChanged += (_, _) =>
        {
            if (restoring || selector.SelectedIndex < 0) return;
            try { appearance.Select((AppearanceMode)selector.SelectedIndex); error.IsVisible = false; }
            catch (Exception ex)
            {
                restoring = true; selector.SelectedIndex = (int)appearance.Mode; restoring = false;
                error.Text = "No se pudo guardar la apariencia: " + ex.Message; error.IsVisible = true;
            }
        };
    }
}
