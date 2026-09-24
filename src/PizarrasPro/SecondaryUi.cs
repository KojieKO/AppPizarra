using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using SukiUI.Controls;

namespace PizarrasPro;

internal static class SecondaryUi
{
    public static SukiWindow Window(string title, double width, double height, double minWidth, double minHeight)
    {
        var window = new SukiWindow
        {
            Title = title, Width = width, Height = height, MinWidth = minWidth, MinHeight = minHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, BackgroundAnimationEnabled = false
        };
        RenderOptions.SetTextRenderingMode(window, TextRenderingMode.Antialias);
        window.KeyDown += (_, e) => { if (e.Key == Key.Escape) { window.Close(); e.Handled = true; } };
        return window;
    }

    public static Button CloseButton(Window window, string text = "Cancelar")
    {
        var button = new Button { Content = text, IsCancel = true };
        button.Click += (_, _) => window.Close();
        return button;
    }

    public static Window Message(string title, string message, bool confirm, Control? extra, string action, bool destructive)
    {
        var window = Window(title, 590, 360, 440, 280);
        var layout = new Grid { RowDefinitions = new RowDefinitions("*,Auto"), Margin = new Thickness(24), RowSpacing = 16 };
        var body = new StackPanel { Spacing = 16 };
        if (destructive)
        {
            var warning = new TextBlock { Text = "Esta acción elimina archivos originales.", TextWrapping = TextWrapping.Wrap, FontWeight = FontWeight.SemiBold };
            warning[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("PizarrasDangerBrush");
            body.Children.Add(warning);
        }
        body.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap });
        if (extra is not null) body.Children.Add(extra);
        layout.Children.Add(new ScrollViewer { Content = body, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled });
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8 };
        Button? cancel = null;
        if (confirm) { cancel = CloseButton(window); buttons.Children.Add(cancel); }
        var accept = new Button { Content = confirm ? action : "Aceptar", IsDefault = !destructive, IsCancel = !confirm };
        accept.Click += (_, _) => window.Close(true);
        buttons.Children.Add(accept);
        Grid.SetRow(buttons, 1); layout.Children.Add(buttons); window.Content = layout;
        window.Opened += (_, _) => { if (confirm) cancel!.Focus(); else accept.Focus(); };
        return window;
    }
}
