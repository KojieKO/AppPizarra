using Windows.Data.Pdf;
using Windows.Storage.Streams;

namespace PizarrasPro;

public record PreviewPage(byte[] Png, int Count, int Index);

public static class PdfPreview
{
    public static async Task<PreviewPage> Render(string path, int index, double width, double height, double zoom, CancellationToken cancellation = default)
    {
        // Snapshot first: the PDF is never held open by the visual panel.
        var bytes = await File.ReadAllBytesAsync(path, cancellation);
        using var source = new InMemoryRandomAccessStream();
        using (var writer = new DataWriter(source))
        {
            writer.WriteBytes(bytes); await writer.StoreAsync().AsTask(cancellation); writer.DetachStream();
        }
        source.Seek(0);
        var document = await PdfDocument.LoadFromStreamAsync(source).AsTask(cancellation);
        if (document.PageCount == 0) throw new InvalidDataException("El PDF no contiene páginas.");
        index = Math.Clamp(index, 0, (int)document.PageCount - 1);
        using var page = document.GetPage((uint)index);
        var scale = Math.Min(width / page.Size.Width, height / page.Size.Height) * zoom;
        scale = Math.Min(scale, Math.Sqrt(6_000_000 / (page.Size.Width * page.Size.Height)));
        var options = new PdfPageRenderOptions { DestinationWidth = (uint)Math.Max(1, page.Size.Width * scale), DestinationHeight = (uint)Math.Max(1, page.Size.Height * scale) };
        using var target = new InMemoryRandomAccessStream();
        await page.RenderToStreamAsync(target, options).AsTask(cancellation);
        target.Seek(0);
        using var reader = new DataReader(target);
        await reader.LoadAsync((uint)target.Size).AsTask(cancellation);
        var result = new byte[(int)target.Size]; reader.ReadBytes(result);
        return new(result, (int)document.PageCount, index);
    }
}
