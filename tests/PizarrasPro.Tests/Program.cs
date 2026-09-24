using System.IO.Compression;
using System.Text;
using System.Text.Json;
using PizarrasPro;
using SkiaSharp;

var testRoot = Path.Combine(Path.GetTempPath(), "PizarrasPro-tests-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(testRoot);
var failures = new List<string>(); var passed = 0;
void Check(bool condition, string description) { if (!condition) throw new Exception(description); }
async Task Test(string name, Func<Task> test)
{
    try { await test(); Console.WriteLine("PASS " + name); passed++; }
    catch (Exception ex) { failures.Add(name + ": " + ex); Console.WriteLine("FAIL " + name + ": " + ex.Message); }
}
Task Sync(Action action) { action(); return Task.CompletedTask; }
void Reject(Action action) { var threw = false; try { action(); } catch { threw = true; } Check(threw, "Se esperaba un rechazo."); }
List<ScheduleRow> Rows() => ScheduleRules.Validate([
    new() { Start = "10:20", End = "11:10", Classes = ["A", "A", "A", "A", "A"] },
    new() { Kind = "break", Start = "11:10", End = "11:40" },
    new() { Start = "11:40", End = "12:30", Classes = ["B", "B", "B", "B", "B"] },
    new() { Start = "12:30", End = "13:25", Classes = ["C", "C", "C", "C", "C"] },
    new() { Start = "13:25", End = "14:20", Classes = ["D", "D", "D", "D", "D"] }
]);
DateTime At(string time) => DateTime.Parse("2026-09-22T" + time);
byte[] Image(SKColor color, int w = 240, int h = 160)
{
    using var bitmap = new SKBitmap(w, h); using var canvas = new SKCanvas(bitmap); canvas.Clear(color); using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100); return data.ToArray();
}
void Wbh(string path, params (string Name, byte[] Bytes)[] entries)
{
    using var zip = ZipFile.Open(path, ZipArchiveMode.Create); foreach (var entry in entries) { using var stream = zip.CreateEntry(entry.Name).Open(); stream.Write(entry.Bytes); }
}
byte[] Page(object metadata, params object[] shapes) => Encoding.UTF8.GetBytes(string.Join("\n", new[] { metadata }.Concat(shapes).Select(x => JsonSerializer.Serialize(x))));
var identity = new[] { 1, 0, 0, 0, 1, 0, 0, 0, 1 };
var sample = Path.Combine(testRoot, "vector.wbh"); var samplePdf = Path.Combine(testRoot, "vector.pdf");
try
{
    await Test("horario: tolerancia exacta y recreo", () => Sync(() => { var rows = Rows(); Check(ScheduleRules.Choices(At("11:15:00"), rows)[0][1] == "A", "límite inclusivo"); Check(ScheduleRules.Choices(At("11:15:01"), rows)[0][1] == "Recreo", "fuera de tolerancia"); Check(ScheduleRules.Choices(At("11:40:00"), rows)[0][1] == "B", "clase prioritaria al recreo"); }));
    await Test("horario: ambigüedad entre clases", () => Sync(() => { var choices = ScheduleRules.Choices(At("12:32:00"), Rows()); Check(choices.Count == 2 && choices[0][1] == "B" && choices[1][1] == "C", "prioridad anterior"); }));
    await Test("horario: salida y fin de semana", () => Sync(() => { Check(ScheduleRules.Choices(At("14:25:00"), Rows())[0][1] == "D", "salida"); Check(ScheduleRules.Choices(At("14:25:01"), Rows()).Count == 0, "fuera horario"); Check(ScheduleRules.Choices(DateTime.Parse("2026-09-26T11:00:00"), Rows()).Count == 0, "sábado"); }));
    await Test("horario: validación", () => Sync(() => { foreach (var text in new[] { "24:00", "9:00", "10:60", "" }) Reject(() => ScheduleRules.Minutes(text)); var rows = Rows(); rows[1].Start = "11:09"; Reject(() => ScheduleRules.Validate(rows)); }));
    await Test("horario: migración siete días", () => Sync(() => { var rows = ScheduleRules.Validate([new() { Label = "Recreo", Start = "11:10", End = "11:40", Classes = ["", "", "", "", "", "", ""] }]); Check(rows[0].Kind == "break" && rows[0].Classes.SequenceEqual(Enumerable.Repeat("Recreo", 5)), "migración"); }));
    await Test("fecha de nombre y sufijo de colisión", () => Sync(() => { Check(FileService.DocumentDate("pdf_2026-09-22-11-05-00.pdf", DateTime.MinValue) == At("11:05:00"), "fecha"); Check(FileService.DocumentDate("2026-09-22-11-05-00 (2).wbh", DateTime.MinValue) == At("11:05:00"), "sufijo"); Check(FileService.DocumentDate("2026-99-22-11-05-00.pdf", At("12:00:00")) == At("12:00:00"), "fecha inválida"); }));
    await Test("renombrado restringido al patrón pdf_", () => Sync(() => { Check(FileService.RootName("pdf_2026-09-22-11-05-00.pdf") == "2026-09-22-11-05-00.pdf", "timestamp"); Check(FileService.RootName("pdf_apuntes.pdf") == "pdf_apuntes.pdf", "nombre manual"); }));
    await Test("configuración junto al ejecutable y traslado", () => Sync(() => { var first = Path.Combine(testRoot, "usb-a", "App"); var second = Path.Combine(testRoot, "usb-b", "App"); var a = new Configuration(first); a.Set("pdf_destination", a.Paths.Store(Path.Combine(first, "Documentos"))); a.SaveSchedule(Rows()); Directory.CreateDirectory(second); File.Copy(Path.Combine(first, "preferencias.json"), Path.Combine(second, "preferencias.json")); File.Copy(Path.Combine(first, "horario.json"), Path.Combine(second, "horario.json")); var b = new Configuration(second); Check(b.Destination == Path.Combine(second, "Documentos") && b.Schedule.Count == 5, "configuración relocatable"); }));
    await Test("preferencias dañadas se conservan", () => Sync(() => { var directory = Path.Combine(testRoot, "damaged"); Directory.CreateDirectory(directory); var path = Path.Combine(directory, "preferencias.json"); File.WriteAllText(path, "{dañado"); var c = new Configuration(directory); Reject(() => c.Set("x", "y")); Check(File.ReadAllText(path) == "{dañado", "sin sobrescritura"); }));
    await Test("asignación manual persistente", () => Sync(() => { var c = new Configuration(Path.Combine(testRoot, "assign")); var item = new DocumentItem { FullPath = Path.Combine(c.Paths.BaseDirectory, "a.pdf"), Timestamp = At("12:32:00") }; c.SetAssignment(item, ["2ª hora", "B"]); var b = new Configuration(c.Paths.BaseDirectory); Check(b.Assignment(item)![1] == "B", "recarga"); b.SetAssignment(item, null); Check(b.Assignment(item) is null, "automática"); }));
    await Test("copia verificada con colisión y fecha", async () => { var src = Path.Combine(testRoot, "a.pdf"); File.WriteAllBytes(src, Enumerable.Range(0, 8000).Select(i => (byte)i).ToArray()); File.SetLastWriteTimeUtc(src, new DateTime(2020, 1, 1)); var dest = Path.Combine(testRoot, "destination"); Directory.CreateDirectory(dest); File.WriteAllText(Path.Combine(dest, "a.pdf"), "existente"); var target = await FileService.Transfer(src, dest, false, false, default); Check(Path.GetFileName(target) == "a (1).pdf" && File.Exists(src), "colisión"); Check(FileService.Hash(src).SequenceEqual(FileService.Hash(target)), "hash"); Check(File.ReadAllText(Path.Combine(dest, "a.pdf")) == "existente", "sin reemplazo"); Check(File.GetLastWriteTimeUtc(src) == File.GetLastWriteTimeUtc(target), "fecha"); });
    await Test("movimiento elimina solo el origen copiado", async () => { var src = Path.Combine(testRoot, "move.pdf"); File.WriteAllText(src, "contenido"); var target = await FileService.Transfer(src, Path.Combine(testRoot, "destination"), true, false, default); Check(!File.Exists(src) && File.ReadAllText(target) == "contenido", "traslado"); });
    await Test("cancelación conserva originales", async () => { var src = Path.Combine(testRoot, "cancel.pdf"); File.WriteAllBytes(src, new byte[200000]); using var ct = new CancellationTokenSource(); ct.Cancel(); try { await FileService.Transfer(src, Path.Combine(testRoot, "cancel-destination"), true, false, ct.Token); throw new Exception("No cancelado"); } catch (OperationCanceledException) { } Check(File.Exists(src) && !Directory.GetFiles(Path.Combine(testRoot, "cancel-destination")).Any(), "origen conservado sin temporales"); });
    await Test("limpieza no sale de raíz y respeta PDF pendientes", () => Sync(() => { var root = Path.Combine(testRoot, "clean"); var folder = Path.Combine(root, "child"); Directory.CreateDirectory(folder); File.WriteAllText(Path.Combine(folder, "keep.pdf"), "pdf"); FileService.Cleanup(folder, root); Check(Directory.Exists(folder), "PDF preservado"); Reject(() => FileService.Cleanup(root, root)); Reject(() => FileService.Cleanup(testRoot, root)); }));
    await Test("limpieza explícita elimina auxiliares y WBH", () => Sync(() => { var root = Path.Combine(testRoot, "clean2"); var folder = Path.Combine(root, "child"); Directory.CreateDirectory(folder); File.WriteAllText(Path.Combine(folder, "aux.wbh"), "aux"); FileService.Cleanup(folder, root); Check(!Directory.Exists(folder) && Directory.Exists(root), "límites limpieza"); }));
    await Test("WBH vectorial: trazo e imagen transformada", () => Sync(() =>
    {
        Wbh(sample, ("page1.page", Page(new { backgroundPath = "bg.png" }, new { shapeType = "GeneralPen", mPaint = new { color = -65536, strokeWidth = 8 }, allPoints = new[] { new { x = 20, y = 20 }, new { x = 100, y = 20 } } }, new { shapeType = "InsertImage", shapeName = "red.png", mMatrixValue = new[] { 0, -1, 180, 1, 0, 60, 0, 0, 1 }, mScaleMatrix = 5, allPoints = new[] { new { x = 0, y = 0 }, new { x = 0, y = 20 }, new { x = 40, y = 0 }, new { x = 40, y = 20 } } })), ("bg.png", Image(SKColors.White)), ("insert_image/red.png", Image(SKColors.Blue, 40, 20)));
        var result = WbhConverter.Convert(sample, samplePdf); Check(result.Pages == 1 && result.PreviewPages == 0, "vector intacto");
    }));
    await Test("PDF nativo: contenido y coordenadas", async () => { var result = await PdfPreview.Render(samplePdf, 0, 240, 160, 1); using var bitmap = SKBitmap.Decode(result.Png); var sx = bitmap.Width / 240f; var sy = bitmap.Height / 160f; var red = bitmap.GetPixel((int)(60 * sx), (int)(20 * sy)); var blue = bitmap.GetPixel((int)(170 * sx), (int)(80 * sy)); Check(result.Count == 1 && red.Red > 200 && red.Green < 80 && blue.Blue > 200 && blue.Red < 80, $"color/geometry {red} {blue}"); });
    await Test("PDF nativo: zoom y liberación del archivo", async () => { var result = await PdfPreview.Render(samplePdf, 0, 240, 160, 2); using var bitmap = SKBitmap.Decode(result.Png); Check(bitmap.Width >= 480, "zoom"); var renamed = samplePdf + ".renamed"; File.Move(samplePdf, renamed); File.Move(renamed, samplePdf); });
    await Test("WBH: fallback sin perder formas y páginas ordenadas", async () => { var path = Path.Combine(testRoot, "fallback.wbh"); Wbh(path, ("page10.page", Page(new { mixBitmapPath = "blue.png" })), ("page2.page", Page(new { backgroundPath = "bg.png", mixBitmapPath = "red.png" }, new { shapeType = "Unknown" })), ("bg.png", Image(SKColors.White)), ("red.png", Image(SKColors.Red)), ("blue.png", Image(SKColors.Blue))); var pdf = Path.ChangeExtension(path, ".pdf"); var result = WbhConverter.Convert(path, pdf); Check(result.Pages == 2 && result.PreviewPages == 2, "fallback"); var first = await PdfPreview.Render(pdf, 0, 240, 160, 1); using var bitmap = SKBitmap.Decode(first.Png); Check(first.Count == 2 && bitmap.GetPixel(50, 50).Red > 200, "orden numérico"); var last = await PdfPreview.Render(pdf, 99, 240, 160, 1); using var blue = SKBitmap.Decode(last.Png); Check(last.Index == 1 && blue.GetPixel(50, 50).Blue > 200, "paginación"); });
    await Test("WBH: no generar salida incompleta", () => Sync(() => { var path = Path.Combine(testRoot, "bad.wbh"); Wbh(path, ("page1.page", Page(new { backgroundPath = "bg.png" }, new { shapeType = "Unknown" })), ("bg.png", Image(SKColors.White))); var pdf = Path.ChangeExtension(path, ".pdf"); Reject(() => WbhConverter.Convert(path, pdf)); Check(File.Exists(path) && !File.Exists(pdf) && !Directory.EnumerateFiles(testRoot, "bad.pdf*.tmp").Any(), "original intacto"); }));
    await Test("WBH: JSON de claves sin comillas", () => Sync(() => { var path = Path.Combine(testRoot, "bare.wbh"); Wbh(path, ("page1.page", Encoding.UTF8.GetBytes("{backgroundPath:\"bg.png\"}")), ("bg.png", Image(SKColors.White))); Check(WbhConverter.Convert(path, Path.ChangeExtension(path, ".pdf")).Pages == 1, "JSON Dahua"); }));
    await Test("PDF inválido informa error", async () => { var path = Path.Combine(testRoot, "invalid.pdf"); File.WriteAllText(path, "not pdf"); var rejected = false; try { await PdfPreview.Render(path, 0, 200, 200, 1); } catch { rejected = true; } Check(rejected, "rechazo inválido"); });
    await Test("escaneo PDF/WBH y subcarpetas", () => Sync(() => { var scan = FileService.Scan(testRoot); Check(scan.Any(x => x.Kind == "WBH") && scan.Any(x => x.FullPath.EndsWith("a (1).pdf")) && scan.All(x => x.Kind is "PDF" or "WBH"), "escaneo"); }));
    if (args.Length > 0)
    {
        var evidence = Path.GetFullPath(args[0]); Directory.CreateDirectory(evidence);
        File.Copy(sample, Path.Combine(evidence, "vector.wbh"), true); File.Copy(samplePdf, Path.Combine(evidence, "vector.pdf"), true);
        var render = await PdfPreview.Render(samplePdf, 0, 960, 640, 1); File.WriteAllBytes(Path.Combine(evidence, "vector.png"), render.Png);
        File.Copy(Path.Combine(testRoot, "fallback.pdf"), Path.Combine(evidence, "dos-paginas.pdf"), true);
        File.WriteAllText(Path.Combine(evidence, "resultado.txt"), $"{passed} pruebas correctas; {failures.Count} fallos.\n" + string.Join("\n", failures));
    }
}
finally
{
    var full = Path.GetFullPath(testRoot); var temp = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
    if (full.StartsWith(temp, StringComparison.OrdinalIgnoreCase) && Path.GetFileName(full).StartsWith("PizarrasPro-tests-")) Directory.Delete(full, true);
}
Console.WriteLine($"{passed} correctas; {failures.Count} fallos.");
Environment.ExitCode = failures.Count == 0 ? 0 : 1;
