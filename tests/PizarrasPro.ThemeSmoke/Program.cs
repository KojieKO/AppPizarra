using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PizarrasPro;
using SukiUI;
using SukiUI.Controls;
using SukiUI.Enums;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            var output = Path.GetFullPath(args.Length > 0 ? args[0] : "docs/validation/suki-01-theme.png");
            Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            AppBuilder.Configure<App>().UsePlatformDetect().SetupWithoutStarting();
            var theme = SukiTheme.GetInstance();
            Require(theme.ThemeColor == SukiColor.Blue && theme.ActiveBaseTheme == ThemeVariant.Light, "Light/Blue");
            Require(AppVersion.Display == "1.0.03", "visible version");
            var assembly = typeof(AppVersion).Assembly;
            Require(System.Reflection.CustomAttributeExtensions.GetCustomAttribute<System.Reflection.AssemblyProductAttribute>(assembly)?.Product == AppVersion.Name, "product metadata");
            Require(AppVersion.Product == "Pizarra Pro 1.0.03", "PDF producer metadata");

            // Borrow the real content without firing MainWindow.Opened/Closing:
            // no scans of removable drives, configuration writes or user documents.
            using var main = new MainWindow();
            Require(main.Title == "Pizarra Pro", "clean window title");
            var content = main.Content;
            main.Content = null;
            using var window = new SukiWindow
            {
                Title = main.Title, Content = content, Width = main.Width, Height = main.Height,
                BackgroundAnimationEnabled = false, ShowInTaskbar = false
            };
            RenderOptions.SetTextRenderingMode(window, RenderOptions.GetTextRenderingMode(main));
            var layout = args.Length > 1 ? args[1] : "initial";
            if (layout == "minimum") { window.Width = main.MinWidth; window.Height = main.MinHeight; }
            if (layout == "maximized") window.WindowState = WindowState.Maximized;
            window.Show();
            Exception? failure = null;
            using var cancellation = new CancellationTokenSource();
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(700) };
            timer.Tick += async (_, _) =>
            {
                timer.Stop();
                try
                {
                    if (layout == "workspace") await WorkspaceChecks.Run(main, window, output);
                    if (layout == "dialogs") await DialogChecks.Run(main, window, output);
                    if (layout == "appearance") await AppearanceChecks.Run(main, window, output);
                    var visuals = window.GetVisualDescendants().ToArray();
                    var grid = visuals.OfType<DataGrid>().Single();
                    Require(grid.Columns.Count == 6 && grid.Template is not null && grid.Bounds.Height > 100, "DataGrid template and layout");
                    Require(visuals.OfType<DataGridColumnHeader>().Count() >= 6, "DataGrid headers");
                    Require(visuals.OfType<Button>().Any(b => b.Template is not null), "button templates");
                    foreach (var key in new[] { "PizarrasSurfaceBrush", "PizarrasWorkspaceBrush", "PizarrasTextBrush", "PizarrasSecondaryTextBrush", "PizarrasBorderBrush", "PizarrasAccentBrush" })
                    {
                        Require(window.TryFindResource(key, out var value) && value is SolidColorBrush brush && brush.Color.A > 0, key);
                    }
                    var outer = (Grid)content!;
                    var header = (Grid)outer.Children[0];
                    Require(header.Children.OfType<TextBlock>().Any(t => t.Text == AppVersion.Name), "header name");
                    Require(!header.Children.OfType<TextBlock>().Any(t => t.Text?.Contains(AppVersion.Display) == true), "header without version");
                    var options = visuals.OfType<Button>().Single(b => Equals(b.Content, "Opciones ▾"));
                    Require(options.ContextMenu!.Items.OfType<MenuItem>().Any(m => Equals(m.Header, "Acerca de Pizarra Pro")), "About menu access");
                    {
                        var about = (Window)typeof(MainWindow).GetMethod("CreateAboutWindow", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!.Invoke(null, null)!;
                        var pending = about.ShowDialog<bool>(window);
                        await Task.Delay(180);
                        Require(about.Title == "Acerca de Pizarra Pro" && about.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "Pizarra Pro\nVersión 1.0.03"), "About actual version");
                        using var aboutBitmap = new RenderTargetBitmap(new PixelSize((int)about.Bounds.Width, (int)about.Bounds.Height), new Vector(96, 96));
                        aboutBitmap.Render(about); aboutBitmap.Save(Path.ChangeExtension(output, "about.png"));
                        about.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Content, "Aceptar")).RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                        Require(await pending && !about.IsVisible, "About closes");
                    }
                    var sections = visuals.OfType<TabControl>().Single();
                    Require(sections.ItemsView.Count == 3, "three main sections");
                    Require(sections.ItemsView.Cast<object>().Select(x => ((TabItem)x).Header?.ToString()).SequenceEqual(new[] { "Archivos", "Horario", "Opciones" }), "section labels");
                    var filesPage = (Grid)((TabItem)sections.ItemsView.Cast<object>().First()).Content!;
                    var body = filesPage.Children.OfType<Grid>().Single(g => Grid.GetRow(g) == 0);
                    var footer = filesPage.Children.Single(c => Grid.GetRow(c) == 1);
                    Require(body.Bounds.Bottom <= footer.Bounds.Top && body.Bounds.Height > 200, "body does not overlap footer");
                    foreach (var child in header.Children.Where(c => c.IsVisible))
                        Require(child.Bounds.Left >= 0 && child.Bounds.Right <= header.Bounds.Width + 1 && child.Bounds.Height > 0, "header fits: " + child.GetType().Name);
                    var viewer = visuals.OfType<ScrollViewer>().Single(v => v.Content is Image);
                    Require(viewer.Bounds.Width > 200 && viewer.Bounds.Height > 150, "preview visible");
                    Console.WriteLine($"Layout {layout}: window {window.Bounds.Size}; table {grid.Bounds.Size}; preview {viewer.Bounds.Size}; header {header.Bounds.Size}; no footer overlap.");
                    using var bitmap = new RenderTargetBitmap(new PixelSize((int)window.Bounds.Width, (int)window.Bounds.Height), new Vector(96, 96));
                    bitmap.Render(window);
                    bitmap.Save(output);
                    Console.WriteLine($"PASS SukiTheme Light/Blue, SukiWindow, six DataGrid headers, button templates, six theme brushes, version {AppVersion.Display}. Render: {output}");
                }
                catch (Exception ex) { failure = ex; }
                finally { window.Close(); cancellation.Cancel(); }
            };
            timer.Start();
            Dispatcher.UIThread.MainLoop(cancellation.Token);
            if (failure is not null) throw failure;
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    private static void Require(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException("Theme smoke failed: " + description);
    }
}
