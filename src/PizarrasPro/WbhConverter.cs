using System.IO.Compression;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using SkiaSharp;

namespace PizarrasPro;

public record ConversionResult(string Path, int Pages, int PreviewPages);

public static class WbhConverter
{
    private static double Number(JsonNode? node, double fallback = 0) => node is null ? fallback : node.GetValue<double>();
    private static string Text(JsonNode? node) => node?.GetValue<string>() ?? "";
    private static JsonObject Record(string line)
    {
        try { return JsonNode.Parse(line)!.AsObject(); }
        catch (System.Text.Json.JsonException) { return JsonNode.Parse(Regex.Replace(line, @"([{,]\s*)([A-Za-z_]\w*)\s*:", "$1\"$2\":"))!.AsObject(); }
    }
    private static byte[] Read(ZipArchiveEntry entry)
    {
        if (entry.Length > 256_000_000) throw new InvalidDataException("Una imagen o página WBH supera el tamaño admitido (256 MB).");
        using var stream = entry.Open(); using var output = new MemoryStream(); stream.CopyTo(output); return output.ToArray();
    }
    private static SKBitmap Bitmap(ZipArchiveEntry entry) => SKBitmap.Decode(Read(entry)) ?? throw new InvalidDataException("Imagen WBH no válida: " + entry.FullName);
    private static SKPoint Transform(JsonNode point, JsonArray? matrix, double scale)
    {
        var x = Number(point["x"]) * scale; var y = Number(point["y"]) * scale;
        return matrix is { Count: >= 6 }
            ? new((float)(Number(matrix[0]) * x + Number(matrix[1]) * y + Number(matrix[2])), (float)(Number(matrix[3]) * x + Number(matrix[4]) * y + Number(matrix[5])))
            : new((float)x, (float)y);
    }
    private static void DrawShape(SKCanvas canvas, JsonObject shape, ZipArchive archive)
    {
        var type = Text(shape["shapeType"] ?? shape["shape_type"]);
        var points = shape["allPoints"] as JsonArray ?? [];
        var matrix = shape["mMatrixValue"] as JsonArray;
        if (type == "GeneralPen")
        {
            if (points.Count == 0) return;
            var color = unchecked((uint)(long)Number(shape["mPaint"]?["color"], -16777216));
            if ((color >> 24) == 0) return;
            using var paint = new SKPaint { Color = new SKColor((byte)(color >> 16), (byte)(color >> 8), (byte)color), StrokeWidth = (float)Number(shape["mPaint"]?["strokeWidth"], 1), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round };
            var scale = Number(shape["mScaleMatrix"], 1); if (scale == 0) scale = 1;
            using var path = new SKPath(); path.MoveTo(Transform(points[0]!, matrix, scale));
            foreach (var point in points.Skip(1)) path.LineTo(Transform(point!, matrix, scale));
            canvas.DrawPath(path, paint); return;
        }
        if (type != "InsertImage") throw new InvalidDataException("Forma no compatible: " + type);
        var filename = "insert_image/" + Text(shape["shapeName"]).Replace('\\', '/').Split('/').Last();
        var entry = archive.Entries.SingleOrDefault(e => e.FullName.Replace('\\', '/') == filename) ?? throw new InvalidDataException("No se encuentra " + filename);
        if (points.Count < 4 || matrix?.Count != 9 || Number(matrix[6]) != 0 || Number(matrix[7]) != 0 || Number(matrix[8]) != 1) throw new InvalidDataException("Geometría de imagen no compatible.");
        var tl = Transform(points[0]!, matrix, 1); var bl = Transform(points[1]!, matrix, 1); var tr = Transform(points[2]!, matrix, 1);
        var a = tr.X - tl.X; var b = tr.Y - tl.Y; var c = bl.X - tl.X; var d = bl.Y - tl.Y;
        if (Math.Abs(a * d - b * c) < 1e-9) throw new InvalidDataException("Imagen sin superficie.");
        using var bitmap = Bitmap(entry);
        var transform = new SKMatrix { ScaleX = a / bitmap.Width, SkewX = c / bitmap.Height, TransX = tl.X, SkewY = b / bitmap.Width, ScaleY = d / bitmap.Height, TransY = tl.Y, Persp2 = 1 };
        canvas.Save(); canvas.Concat(in transform); canvas.DrawBitmap(bitmap, 0, 0); canvas.Restore();
    }
    public static ConversionResult Convert(string source, string destination)
    {
        if (!Path.GetExtension(source).Equals(".wbh", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Selecciona un WBH.");
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        var temp = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        var pages = 0; var previews = 0;
        try
        {
            using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write))
            using (var document = SKDocument.CreatePdf(stream, new SKDocumentPdfMetadata { Producer = AppVersion.Product, Title = Path.GetFileNameWithoutExtension(source) }))
            using (var archive = ZipFile.OpenRead(source))
            {
                var entries = archive.Entries.Where(e => e.FullName.EndsWith(".page", StringComparison.OrdinalIgnoreCase)).OrderBy(e => { var m = Regex.Match(e.Name, @"(\d+)(?=\.page$)", RegexOptions.IgnoreCase); return m.Success ? long.Parse(m.Value) : long.MaxValue; }).ThenBy(e => e.FullName).ToList();
                if (entries.Count == 0) throw new InvalidDataException("El WBH no contiene páginas .page.");
                foreach (var entry in entries)
                {
                    var records = System.Text.Encoding.UTF8.GetString(Read(entry)).TrimStart('\uFEFF').Split('\n').Where(s => !string.IsNullOrWhiteSpace(s)).Select(Record).ToList();
                    if (records.Count == 0) continue;
                    var metadata = records[0]; SKBitmap? background = null;
                    try { var bg = archive.GetEntry(Text(metadata["backgroundPath"])); if (bg is not null) background = Bitmap(bg); } catch { }
                    using (background)
                    using (var recorder = new SKPictureRecorder())
                    {
                        SKPicture? vectors = null;
                        if (background is not null)
                        {
                            try
                            {
                                var canvas = recorder.BeginRecording(new SKRect(0, 0, background.Width, background.Height));
                                foreach (var shape in records.Skip(1)) DrawShape(canvas, shape, archive);
                                vectors = recorder.EndRecording();
                            }
                            catch (Exception ex) when (ex is not OutOfMemoryException) { AppLog.Write("Página WBH usa vista compuesta: " + ex.Message); }
                        }
                        using (vectors)
                        {
                            if (background is not null && vectors is not null)
                            {
                                var canvas = document.BeginPage(background.Width, background.Height); canvas.Clear(SKColors.White); canvas.DrawBitmap(background, 0, 0); canvas.DrawPicture(vectors); document.EndPage();
                            }
                            else
                            {
                                var previewEntry = archive.GetEntry(Text(metadata["mixBitmapPath"])) ?? throw new InvalidDataException($"La página {entry.Name} contiene elementos no compatibles y no tiene vista compuesta.");
                                using var preview = Bitmap(previewEntry);
                                var w = background?.Width ?? preview.Width; var h = background?.Height ?? preview.Height;
                                var canvas = document.BeginPage(w, h); canvas.Clear(SKColors.White); canvas.DrawBitmap(preview, new SKRect(0, 0, w, h)); document.EndPage(); previews++;
                            }
                        }
                    }
                    pages++;
                }
                if (pages == 0) throw new InvalidDataException("El WBH no contiene páginas utilizables.");
                document.Close();
            }
            var bytes = File.ReadAllBytes(temp);
            if (!System.Text.Encoding.ASCII.GetString(bytes.AsSpan(0, Math.Min(5, bytes.Length))).StartsWith("%PDF-") || !System.Text.Encoding.ASCII.GetString(bytes.AsSpan(Math.Max(0, bytes.Length - 1024))).Contains("%%EOF")) throw new InvalidDataException("El PDF no se ha generado correctamente.");
            File.Move(temp, destination, false);
            return new(destination, pages, previews);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
