using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using PizarrasPro;

internal static class DialogChecks
{
    private static readonly BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private static void Check(bool value, string label) { if (!value) throw new Exception(label); Console.WriteLine("PASS " + label); }
    private static Button Button(Window win, string text) => win.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Content, text));
    private static void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
    private static async Task Capture(Window win, string output, string name)
    {
        await Task.Delay(180);
        foreach (var button in win.GetVisualDescendants().OfType<Button>().Where(b => b.IsVisible && b.Content is string && !Equals(b.Content, "Quitar") && b.Bounds.Width > 0))
        {
            var point = button.TranslatePoint(default, win);
            Check(point is not null && point.Value.X >= 0 && point.Value.X + button.Bounds.Width <= win.Bounds.Width + 1, name + " button fits: " + button.Content);
        }
        using var bitmap = new RenderTargetBitmap(new PixelSize((int)win.Bounds.Width, (int)win.Bounds.Height), new Vector(96,96));
        bitmap.Render(win); bitmap.Save(Path.Combine(Path.GetDirectoryName(output)!, "suki-04-" + name + ".png"));
    }
    public static async Task Run(MainWindow main, Window owner, string output)
    {
        var folder = Path.Combine(Path.GetDirectoryName(output)!, "suki-04-synthetic"); Directory.CreateDirectory(folder);
        var config = new Configuration(folder);
        config.SaveSchedule([new() { Start = "08:30", End = "09:25", Classes = ["Matemáticas con nombre largo", "Lengua", "Inglés", "Historia", "Física"] }, new() { Kind = "break", Start = "09:25", End = "09:45" }]);
        typeof(MainWindow).GetField("config", Flags)!.SetValue(main, config);
        var schedule = (Window)typeof(MainWindow).GetMethod("CreateScheduleWindow", Flags)!.Invoke(main, null)!;
        var pending = schedule.ShowDialog(owner); await Capture(schedule, output, "schedule");
        var first = schedule.GetVisualDescendants().OfType<TextBox>().First(); first.Text = "29:99";
        Click(Button(schedule, "Guardar horario"));
        Check(schedule.IsVisible && config.Schedule[0].Start == "08:30", "invalid HH:MM does not save or close");
        schedule.Width = schedule.MinWidth; schedule.Height = schedule.MinHeight;
        await Capture(schedule, output, "schedule-minimum");
        Click(Button(schedule, "Cancelar")); await pending;
        Check(config.Schedule[0].Start == "08:30" && config.Schedule[1].Kind == "break", "cancel preserves schedule and structural break");
        var cloud = (Window)typeof(MainWindow).GetMethod("CreateCloudWindow", Flags)!.Invoke(main, null)!;
        pending = cloud.ShowDialog(owner); cloud.Width = cloud.MinWidth; cloud.Height = cloud.MinHeight;
        await Capture(cloud, output, "cloud-microsoft");
        cloud.GetVisualDescendants().OfType<TabControl>().Single().SelectedIndex = 1;
        await Capture(cloud, output, "cloud-google");
        Click(Button(cloud, "Cerrar")); await pending;
        object?[] folderArgs = [new CloudDrive("OneDrive"), null];
        var picker = (Window)typeof(MainWindow).GetMethod("CreateCloudFolderWindow", Flags)!.Invoke(null, folderArgs)!;
        // The production caller loads remote folders on Opened; this fixture supplies local rows only.
        pending = picker.ShowDialog(owner); picker.Width = picker.MinWidth; picker.Height = picker.MinHeight;
        await Task.Delay(100);
        picker.GetVisualDescendants().OfType<ListBox>().Single().ItemsSource = new[] { new CloudFolder("synthetic", "Carpeta sintética con nombre largo para comprobar su presentación") };
        await Capture(picker, output, "folder");
        Click(Button(picker, "Cancelar")); await pending;
        var helper = typeof(MainWindow).Assembly.GetType("PizarrasPro.SecondaryUi")!;
        var method = helper.GetMethod("Message")!;
        foreach (var kind in new[] { "delete", "class", "error" })
        {
            var extra = kind == "class" ? new ComboBox { ItemsSource = new[] { "Automática (clase anterior)", "Clase alternativa" }, SelectedIndex = 0, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch } : null;
            var win = (Window)method.Invoke(null, ["Prueba " + kind, kind == "class" ? "pdf_2026-09-23_09-25_nombre_sintético_para_cambio_de_clase.pdf" : string.Join("\n", Enumerable.Repeat("Archivo sintético con nombre largo: no se modificarán documentos reales.", 24)), kind != "error", extra, kind == "delete" ? "Eliminar" : "Guardar", kind == "delete"])!;
            var result = win.ShowDialog<bool>(owner); win.Width = win.MinWidth; win.Height = win.MinHeight;
            await Capture(win, output, kind);
            if (kind == "delete") Check(Button(win, "Cancelar").IsFocused && !Button(win, "Eliminar").IsDefault, "destructive default is safe");
            if (kind == "class")
            {
                extra!.SelectedIndex = 1;
                Click(Button(win, "Guardar"));
                Check(await result && extra.SelectedIndex == 1, "class selection accepted");
                continue;
            }
            win.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape });
            Check(!await result, "Escape cancels " + kind);
        }
        Console.WriteLine("PASS modal windows, synthetic schedule validation, cancellation, long messages; no cloud authentication or uploads.");
    }
}
