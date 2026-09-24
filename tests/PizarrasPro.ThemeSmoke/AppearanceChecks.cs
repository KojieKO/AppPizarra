using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.VisualTree;
using PizarrasPro;
using SukiUI;

internal static class AppearanceChecks
{
    public class PlatformStub : DispatchProxy
    {
        public PlatformColorValues Values = new() { ThemeVariant = PlatformThemeVariant.Light };
        public EventHandler<PlatformColorValues>? Changed;
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            switch (method!.Name)
            {
                case "GetColorValues": return Values;
                case "add_ColorValuesChanged": Changed += (EventHandler<PlatformColorValues>)args![0]!; return null;
                case "remove_ColorValuesChanged": Changed -= (EventHandler<PlatformColorValues>)args![0]!; return null;
                default: throw new NotSupportedException(method.Name);
            }
        }
        public void Set(PlatformThemeVariant value) { Values = new() { ThemeVariant = value }; Changed?.Invoke(this, Values); }
    }
    private static void Check(bool value, string label) { if (!value) throw new Exception(label); Console.WriteLine("PASS " + label); }
    private static void Capture(Window window, string path)
    {
        using var bitmap = new RenderTargetBitmap(new PixelSize((int)window.Bounds.Width, (int)window.Bounds.Height), new Vector(96, 96));
        bitmap.Render(window); bitmap.Save(path);
    }
    private static double Luminance(Color c)
    {
        double Linear(byte b) { var v = b / 255d; return v <= .04045 ? v / 12.92 : Math.Pow((v + .055) / 1.055, 2.4); }
        return .2126 * Linear(c.R) + .7152 * Linear(c.G) + .0722 * Linear(c.B);
    }
    private static void Contrast(Window window, string foreground, string background)
    {
        window.TryFindResource(foreground, window.ActualThemeVariant, out var fg); window.TryFindResource(background, window.ActualThemeVariant, out var bg);
        Color ColorOf(object? value) => value is SolidColorBrush brush ? brush.Color : (Color)value!;
        var a = Luminance(ColorOf(fg)); var b = Luminance(ColorOf(bg));
        var ratio = (Math.Max(a, b) + .05) / (Math.Min(a, b) + .05);
        Check(ratio >= 4.5, $"contrast {foreground}/{background}: {ratio:F2}:1");
    }
    public static async Task Run(MainWindow main, Window owner, string output)
    {
        var folder = Path.Combine(Path.GetTempPath(), "PizarrasAppearance-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            File.WriteAllText(Path.Combine(folder, "preferencias.json"), "{\"unrelated\":{\"keep\":42}}");
            var config = new Configuration(folder);
            typeof(MainWindow).GetField("config", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(main, config);
            var pdfPath = Path.Combine(folder, "colors.pdf");
            using (var pdf = SkiaSharp.SKDocument.CreatePdf(pdfPath))
            {
                var canvas = pdf.BeginPage(200, 200); canvas.Clear(SkiaSharp.SKColors.White);
                using var paint = new SkiaSharp.SKPaint { Color = SkiaSharp.SKColors.Red };
                canvas.DrawRect(20, 20, 60, 60, paint); paint.Color = SkiaSharp.SKColors.Blue;
                canvas.DrawRect(100, 100, 60, 60, paint); pdf.EndPage(); pdf.Close();
            }
            var originalPdf = File.ReadAllBytes(pdfPath);
            var originalPreview = await PdfPreview.Render(pdfPath, 0, 240, 240, 1);
            var image = (Image)owner.GetVisualDescendants().OfType<ScrollViewer>().Single(v => v.Content is Image).Content!;
            using var preview = new Bitmap(new MemoryStream(originalPreview.Png));
            image.Source = preview;
            var platform = DispatchProxy.Create<IPlatformSettings, PlatformStub>();
            var stub = (PlatformStub)platform;
            using var appearance = new Appearance(config, platform);
            Check(appearance.Mode == AppearanceMode.Light, "legacy preferences remain light");
            var control = new AppearanceControl(appearance);
            var helper = typeof(MainWindow).Assembly.GetType("PizarrasPro.SecondaryUi")!;
            var dialog = (Window)helper.GetMethod("Message")!.Invoke(null, ["Apariencia", "", false, control, "Aceptar", false])!;
            var pending = dialog.ShowDialog<bool>(owner);
            await Task.Delay(180);
            var selector = control.Children.OfType<ComboBox>().Single();
            foreach (var mode in new[] { AppearanceMode.Dark, AppearanceMode.Light, AppearanceMode.System })
            {
                selector.SelectedIndex = (int)mode;
                await Task.Delay(750);
                var expected = mode == AppearanceMode.Dark ? ThemeVariant.Dark : ThemeVariant.Light;
                Check(owner.ActualThemeVariant == expected && dialog.ActualThemeVariant == expected && SukiTheme.GetInstance().ActiveBaseTheme == expected, "immediate owner/dialog theme " + mode);
                Check(Appearance.ReadMode(new Configuration(folder)) == mode, "persist " + mode);
                using (var restarted = new Appearance(new Configuration(folder), platform))
                    Check(owner.ActualThemeVariant == expected, "startup " + mode);
                Contrast(owner, "PizarrasTextBrush", "PizarrasSurfaceBrush");
                Contrast(owner, "PizarrasSecondaryTextBrush", "PizarrasSurfaceBrush");
                Contrast(owner, "PizarrasSecondaryTextBrush", "PizarrasWorkspaceBrush");
                var rendered = await PdfPreview.Render(pdfPath, 0, 240, 240, 1);
                Check(ReferenceEquals(image.Source, preview) && originalPreview.Png.SequenceEqual(rendered.Png) && originalPdf.SequenceEqual(File.ReadAllBytes(pdfPath)), "PDF and rendered colors unchanged " + mode);
                Capture(dialog, Path.ChangeExtension(output, mode + ".dialog.png"));
                Capture(owner, Path.ChangeExtension(output, mode + ".png"));
                foreach (var factory in new[] { "CreateAboutWindow", "CreateScheduleWindow", "CreateCloudWindow" })
                {
                    var method = typeof(MainWindow).GetMethod(factory, BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)!;
                    var secondary = (Window)method.Invoke(method.IsStatic ? null : main, null)!;
                    var secondaryPending = secondary.ShowDialog(dialog); await Task.Delay(180);
                    Check(secondary.ActualThemeVariant == expected, factory + " " + mode);
                    Capture(secondary, Path.ChangeExtension(output, mode + "." + factory + ".png"));
                    secondary.Close(); await secondaryPending;
                }
            }
            stub.Set(PlatformThemeVariant.Dark); await Task.Delay(180);
            Check(owner.ActualThemeVariant == ThemeVariant.Dark && dialog.ActualThemeVariant == ThemeVariant.Dark, "system change follows while open");
            using (var restarted = new Appearance(new Configuration(folder), platform))
                Check(restarted.Mode == AppearanceMode.System && owner.ActualThemeVariant == ThemeVariant.Dark, "startup resolves saved system mode");
            stub.Set(PlatformThemeVariant.Light); await Task.Delay(180);
            Check(owner.ActualThemeVariant == ThemeVariant.Light, "system changes back");
            selector.SelectedIndex = (int)AppearanceMode.Dark;
            stub.Set(PlatformThemeVariant.Light); await Task.Delay(180);
            Check(owner.ActualThemeVariant == ThemeVariant.Dark, "explicit choice ignores system changes");
            Check(new Configuration(folder).Preferences["unrelated"]!["keep"]!.GetValue<int>() == 42, "preserve unrelated preferences");
            dialog.Close(true); await pending;
            image.Source = null;
            appearance.Select(AppearanceMode.Light);
            appearance.Dispose(); Check(stub.Changed is null, "system subscription released");
            File.WriteAllText(Path.Combine(folder, "preferencias.json"), "broken");
            using var invalid = new Appearance(new Configuration(folder), platform);
            var failed = false;
            try { invalid.Select(AppearanceMode.Dark); } catch (InvalidDataException) { failed = true; }
            Check(failed && invalid.Mode == AppearanceMode.Light && File.ReadAllText(Path.Combine(folder, "preferencias.json")) == "broken", "failed save keeps theme and corrupt preferences intact");
        }
        finally { Directory.Delete(folder, true); }
    }
}
