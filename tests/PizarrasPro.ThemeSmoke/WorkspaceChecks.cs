using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using PizarrasPro;

internal static class WorkspaceChecks
{
    private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static T Get<T>(MainWindow main, string field) => (T)typeof(MainWindow).GetField(field, Flags)!.GetValue(main)!;
    private static void Set(MainWindow main, string field, object value) => typeof(MainWindow).GetField(field, Flags)!.SetValue(main, value);
    private static object? Call(MainWindow main, string method, params object[] args) => typeof(MainWindow).GetMethod(method, Flags)!.Invoke(main, args);
    private static Task Invoke(MainWindow main, string method, params object[] args) => (Task)Call(main, method, args)!;
    private static void Check(bool value, string label) { if (!value) throw new Exception(label); Console.WriteLine("PASS " + label); }
    private static async Task Wait(Func<bool> ready)
    {
        for (var i = 0; i < 200 && !ready(); i++) await Task.Delay(50);
        Check(ready(), "render completed");
    }
    public static async Task Run(MainWindow main, Window window, string output)
    {
        var folder = Path.Combine(Path.GetDirectoryName(output)!, "suki-03-functional");
        var table = Get<DataGrid>(main, "table");
        var image = Get<Image>(main, "previewImage");
        var previewText = Get<TextBlock>(main, "previewText");
        // All files and preferences belong exclusively to this synthetic test directory.
        Set(main, "config", new Configuration(Path.Combine(folder, "ui-config")));
        Set(main, "root", folder);
        await Invoke(main, "Refresh", true);
        var rows = table.ItemsSource.Cast<DocumentItem>().ToArray();
        var pdf = rows.Single(x => Path.GetFileName(x.FullPath) == "dos-paginas.pdf");
        var other = rows.Single(x => Path.GetFileName(x.FullPath) == "vector.pdf");
        var wbh = rows.Single(x => x.Kind == "WBH");
        table.Columns[0].Sort(System.ComponentModel.ListSortDirection.Descending);
        await Task.Delay(100);
        var visibleRows = table.GetVisualDescendants().OfType<DataGridRow>().Where(x => x.IsVisible).OrderBy(x => x.Bounds.Y).Select(x => ((DocumentItem)x.DataContext!).Name).ToArray();
        Check(visibleRows.SequenceEqual(visibleRows.OrderDescending(StringComparer.CurrentCulture)), "descending filename sort");
        table.Columns[0].Sort(System.ComponentModel.ListSortDirection.Ascending);
        table.SelectedItem = pdf;
        await Wait(() => image.Source is not null && Get<int>(main, "pageCount") == 2);
        Check(!Get<Button>(main, "previousPage").IsEnabled && Get<Button>(main, "nextPage").IsEnabled, "first page navigation");
        Get<Button>(main, "nextPage").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Wait(() => Get<TextBlock>(main, "pageText").Text!.Contains("2 / 2"));
        Check(!Get<Button>(main, "nextPage").IsEnabled, "last page navigation");
        var width = ((Bitmap)image.Source!).PixelSize.Width;
        Console.WriteLine($"PDF bitmap {((Bitmap)image.Source!).PixelSize}, DPI {((Bitmap)image.Source!).Dpi}, logical image {image.Bounds.Size}");
        Get<Button>(main, "zoomIn").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Wait(() => ((Bitmap)image.Source!).PixelSize.Width > width);
        Get<Button>(main, "fitPage").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Wait(() => ((Bitmap)image.Source!).PixelSize.Width == width);
        Check(Get<double>(main, "zoom") == 1, "fit resets zoom");
        var viewer = Get<ScrollViewer>(main, "previewScroll");
        viewer.RaiseEvent(new Avalonia.Input.PointerWheelEventArgs(viewer, new Avalonia.Input.Pointer(1, Avalonia.Input.PointerType.Mouse, true), viewer, default, 0, default, Avalonia.Input.KeyModifiers.Control, new Avalonia.Vector(0, 1)));
        await Wait(() => Get<double>(main, "zoom") > 1 && ((Bitmap)image.Source!).PixelSize.Width > width);
        await Invoke(main, "SetZoom", 1d);
        var body = Get<Grid>(main, "body");
        body.ColumnDefinitions[0].Width = new GridLength(2, GridUnitType.Star);
        body.ColumnDefinitions[2].Width = new GridLength(3, GridUnitType.Star);
        await Wait(() => ((Bitmap)image.Source!).PixelSize.Width != width);
        Check(viewer.Bounds.Width > 500, "resizing panel triggers fitted render");
        table.Columns[0].Width = new DataGridLength(285);
        await Task.Delay(100);
        Call(main, "SaveColumnWidths"); table.Columns[0].Width = new DataGridLength(180); Call(main, "LoadColumnWidths");
        Check(Math.Abs(table.Columns[0].Width.Value - 285) < 1, "column width persistence with isolated preferences");
        table.SelectedItem = other; table.SelectedItem = pdf; table.SelectedItem = wbh;
        await Task.Delay(400);
        Check(image.Source is null && Get<int>(main, "pageCount") == 0 && previewText.Text!.Contains("WBH"), "rapid selection discards obsolete PDF renders and explains WBH");
        table.SelectedItem = pdf; await Wait(() => image.Source is not null);
        table.SelectedItems.Add(other);
        Check(image.Source is null && previewText.Text!.Contains("Varios"), "multiple selection clears viewer");
        Call(main, "SelectContextRow", pdf);
        Check(table.SelectedItems.Count == 2, "right click within selection preserves multiple rows");
        Call(main, "SelectContextRow", wbh);
        Check(table.SelectedItems.Count == 1 && table.SelectedItem == wbh, "right click outside selection targets its document");
        table.ContextMenu!.Open(table); await Task.Delay(100);
        Check(table.ContextMenu.IsOpen && table.ContextMenu.Items.OfType<MenuItem>().Single(x => (string)x.Header! == "Convertir WBH").IsEnabled, "table menu opens with WBH command");
        table.ContextMenu.Close();
        table.ContextMenu.Open(table);
        table.SelectedItem = other;
        table.ContextMenu.Items.OfType<MenuItem>().Single(x => (string)x.Header! == "Copiar").RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Check(Get<string[]>(main, "clipboard").Length == 0, "menu refuses a changed document selection");
        table.ContextMenu.Close();
        table.SelectedItem = pdf; await Wait(() => image.Source is not null);
        var preview = viewer.GetVisualAncestors().OfType<Grid>().First();
        preview.ContextMenu!.Open(preview); await Task.Delay(100);
        Check(preview.ContextMenu.IsOpen && preview.ContextMenu.Items.OfType<MenuItem>().All(x => x.IsEnabled), "viewer menu targets the selected PDF");
        using (var capture = new RenderTargetBitmap(new Avalonia.PixelSize((int)preview.ContextMenu.Bounds.Width, (int)preview.ContextMenu.Bounds.Height), new Avalonia.Vector(96, 96)))
        {
            capture.Render(preview.ContextMenu); capture.Save(Path.Combine(Path.GetDirectoryName(output)!, "suki-03-menu.png"));
        }
        preview.ContextMenu.Close();
        Set(main, "busy", true); preview.ContextMenu.Open(preview); await Task.Delay(100);
        Check(preview.ContextMenu.Items.OfType<MenuItem>().All(x => !x.IsEnabled), "busy state disables viewer commands");
        preview.ContextMenu.Close(); Set(main, "busy", false);
        using (File.Open(pdf.FullPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
        Check(true, "displayed PDF is not held open");
        body.ColumnDefinitions[0].Width = new GridLength(3, GridUnitType.Star);
        body.ColumnDefinitions[2].Width = new GridLength(2, GridUnitType.Star);
        table.Focus(); await Task.Delay(350); await Invoke(main, "RenderPreview"); await Task.Delay(100);
        Check(image.Bounds.Width <= viewer.Bounds.Width && image.Bounds.Height <= viewer.Bounds.Height, "fitted PDF stays within viewport regardless of PNG DPI");
    }
}
