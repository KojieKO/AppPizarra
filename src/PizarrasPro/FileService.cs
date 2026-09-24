using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace PizarrasPro;

public sealed class DocumentItem
{
    public string FullPath { get; init; } = "";
    public string Name => Path.GetFileName(FullPath);
    public string Kind => Path.GetExtension(FullPath).TrimStart('.').ToUpperInvariant();
    public long Bytes { get; init; }
    public string Size => Bytes == 0 ? "0 KB (Error)" : $"{Bytes / 1024:N0} KB";
    public DateTime Timestamp { get; init; }
    public string Date => Timestamp.ToString("dd/MM/yyyy");
    public string Time => Timestamp.ToString("HH:mm");
    public string Class { get; set; } = "—";
    public string Slot { get; set; } = "";
}

public static class FileService
{
    private static readonly EnumerationOptions ScanOptions = new() { RecurseSubdirectories = true, IgnoreInaccessible = true, AttributesToSkip = FileAttributes.ReparsePoint };
    public static DateTime DocumentDate(string path, DateTime fallback)
    {
        var match = Regex.Match(Path.GetFileName(path), @"^(?:pdf_)?(\d{4}-\d{2}-\d{2}-\d{2}-\d{2}-\d{2})(?: \(\d+\))?\.(?:pdf|wbh)$", RegexOptions.IgnoreCase);
        return match.Success && DateTime.TryParseExact(match.Groups[1].Value, "yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : fallback;
    }
    public static List<DocumentItem> Scan(string root)
    {
        if (!Directory.Exists(root)) return [];
        var result = new List<DocumentItem>();
        foreach (var path in Directory.EnumerateFiles(root, "*", ScanOptions))
        {
            if (Path.GetExtension(path).ToLowerInvariant() is not (".pdf" or ".wbh")) continue;
            try
            {
                var info = new FileInfo(path);
                result.Add(new() { FullPath = info.FullName, Bytes = info.Length, Timestamp = DocumentDate(path, info.LastWriteTime) });
            }
            catch (IOException ex) { AppLog.Write(ex.Message); }
        }
        return result.OrderByDescending(x => x.Timestamp).ToList();
    }
    public static List<string> DetectRoots() => DriveInfo.GetDrives().Where(x => x.IsReady).SelectMany(d => new[] { Path.Combine(d.RootDirectory.FullName, "Archivo de pizarra"), Path.Combine(d.RootDirectory.FullName, "Archivos de Pizarra") }).Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    public static bool RootMoveAllowed(string root)
    {
        if (string.IsNullOrWhiteSpace(root)) return false;
        return new DriveInfo(Path.GetPathRoot(Path.GetFullPath(root))!).DriveType == DriveType.Removable || root.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(p => p.Equals("Archivo de pizarra", StringComparison.OrdinalIgnoreCase) || p.Equals("Archivos de pizarra", StringComparison.OrdinalIgnoreCase));
    }
    public static bool Same(string a, string b) => !string.IsNullOrEmpty(a) && !string.IsNullOrEmpty(b) && Path.GetFullPath(a).TrimEnd('\\', '/').Equals(Path.GetFullPath(b).TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase);
    public static string RootName(string path)
    {
        var name = Path.GetFileName(path);
        return Regex.IsMatch(name, @"^pdf_\d{4}-\d{2}-\d{2}-\d{2}-\d{2}-\d{2}\.pdf$", RegexOptions.IgnoreCase) ? name[4..] : name;
    }
    public static string Unique(string directory, string name)
    {
        var candidate = Path.Combine(directory, Path.GetFileName(name));
        var i = 1;
        while (File.Exists(candidate) || Directory.Exists(candidate)) candidate = Path.Combine(directory, $"{Path.GetFileNameWithoutExtension(name)} ({i++}){Path.GetExtension(name)}");
        return candidate;
    }
    public static byte[] Hash(string path) { using var stream = File.OpenRead(path); return SHA256.HashData(stream); }
    public static async Task<string> Transfer(string source, string destination, bool move, bool rename, CancellationToken cancellation)
    {
        if (Same(Path.GetDirectoryName(source)!, destination)) return source;
        Directory.CreateDirectory(destination);
        var target = Unique(destination, rename ? RootName(source) : Path.GetFileName(source));
        var temp = Path.Combine(destination, ".pizarras-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            // Lock the source against writers while copying and verifying across USB/volume boundaries.
            await using (var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true))
            {
                var timestamp = File.GetLastWriteTimeUtc(source);
                byte[] hash;
                await using (var output = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
                { await input.CopyToAsync(output, cancellation); output.Flush(true); }
                input.Position = 0; hash = await SHA256.HashDataAsync(input, cancellation);
                if (!hash.SequenceEqual(Hash(temp))) throw new IOException("La copia no coincide con el original. Se conserva el origen.");
                File.SetLastWriteTimeUtc(temp, timestamp);
                cancellation.ThrowIfCancellationRequested();
                File.Move(temp, target, false);
            }
            if (move)
            {
                // Detect a writer changing the original after releasing the read handle.
                if (!Hash(source).SequenceEqual(Hash(target))) throw new IOException("El original cambió. Se conservan ambos archivos.");
                File.Delete(source);
            }
            return target;
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
    public static void Cleanup(string folder, string root)
    {
        folder = Path.GetFullPath(folder); root = Path.GetFullPath(root).TrimEnd('\\', '/');
        if (!folder.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new IOException("La limpieza debe estar dentro de la carpeta de origen.");
        if (!Directory.Exists(folder)) return;
        // Reject links at every ancestor and in the subtree before recursive deletion.
        for (var current = new DirectoryInfo(folder); current is not null; current = current.Parent)
        {
            if (current.Attributes.HasFlag(FileAttributes.ReparsePoint)) throw new IOException("No se limpia una carpeta enlazada.");
            if (Same(current.FullName, root)) break;
        }
        var all = Directory.EnumerateFileSystemEntries(folder, "*", new EnumerationOptions { RecurseSubdirectories = true, AttributesToSkip = 0, IgnoreInaccessible = false }).ToList();
        if (all.Any(p => File.GetAttributes(p).HasFlag(FileAttributes.ReparsePoint))) throw new IOException("No se limpian carpetas con enlaces.");
        if (all.Any(p => Path.GetExtension(p).Equals(".pdf", StringComparison.OrdinalIgnoreCase))) return;
        // This legacy option is exposed only behind an explicit destructive-operation confirmation.
        Directory.Delete(folder, true);
        var parent = Path.GetDirectoryName(folder);
        while (parent is not null && !Same(parent, root) && !Directory.EnumerateFileSystemEntries(parent).Any()) { Directory.Delete(parent); parent = Path.GetDirectoryName(parent); }
    }
}
