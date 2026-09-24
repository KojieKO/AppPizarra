using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Globalization;
using System.Text.RegularExpressions;

namespace PizarrasPro;

public sealed class PortablePaths(string baseDirectory)
{
    public string BaseDirectory { get; } = Path.GetFullPath(baseDirectory);
    public string Store(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "";
        path = Path.GetFullPath(path);
        var inside = path.StartsWith(BaseDirectory.TrimEnd('\\', '/') + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || FileService.Same(path, BaseDirectory);
        var sameUsb = string.Equals(Path.GetPathRoot(path), Path.GetPathRoot(BaseDirectory), StringComparison.OrdinalIgnoreCase) && new DriveInfo(Path.GetPathRoot(BaseDirectory)!).DriveType == DriveType.Removable;
        return inside || sameUsb
            ? Path.GetRelativePath(BaseDirectory, path) : path;
    }
    public string Resolve(string path) => string.IsNullOrWhiteSpace(path) ? "" : Path.GetFullPath(path, BaseDirectory);
}

public sealed class Configuration
{
    public static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    public PortablePaths Paths { get; }
    public JsonObject Preferences { get; private set; } = new();
    public List<ScheduleRow> Schedule { get; private set; } = [];
    public string? LoadError { get; private set; }
    private bool preferencesValid = true;
    public Configuration(string directory)
    {
        Paths = new(directory);
        try
        {
            var path = Path.Combine(directory, "preferencias.json");
            if (File.Exists(path)) Preferences = JsonNode.Parse(File.ReadAllText(path)) as JsonObject ?? throw new InvalidDataException("Preferencias no válidas.");
        }
        catch (Exception ex) { preferencesValid = false; LoadError = "No se cargaron las preferencias: " + ex.Message; }
        try
        {
            var path = Path.Combine(directory, "horario.json");
            if (File.Exists(path)) Schedule = ScheduleRules.Validate(JsonSerializer.Deserialize<List<ScheduleRow>>(File.ReadAllText(path), Json) ?? []);
        }
        catch (Exception ex) { LoadError = (LoadError + "\nNo se cargó el horario: " + ex.Message).Trim(); }
    }
    public string Text(string key) => Preferences[key]?.GetValue<string>() ?? "";
    public string Destination => Paths.Resolve(Text("pdf_destination"));
    public string Root => Paths.Resolve(Text("selected_root"));
    public void Set(string key, JsonNode? value)
    {
        if (!preferencesValid) throw new InvalidDataException("El archivo preferencias.json no es válido. Corrígelo o conserva una copia y retíralo antes de volver a iniciar la aplicación; no se sobrescribirá.");
        var next = (JsonObject)Preferences.DeepClone(); next[key] = value;
        AtomicWrite(Path.Combine(Paths.BaseDirectory, "preferencias.json"), next.ToJsonString(Json));
        Preferences = next;
    }
    public void SaveSchedule(List<ScheduleRow> rows)
    {
        rows = ScheduleRules.Validate(rows);
        AtomicWrite(Path.Combine(Paths.BaseDirectory, "horario.json"), JsonSerializer.Serialize(rows, Json));
        Schedule = rows;
    }
    public static void AtomicWrite(string path, string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllText(temporary, text); File.Move(temporary, path, true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    public string ClassKey(DocumentItem item) => Paths.Store(item.FullPath).ToLowerInvariant() + "|" + item.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
    public string[]? Assignment(DocumentItem item)
    {
        var overrides = Preferences["class_overrides"] as JsonObject;
        var legacy = item.FullPath.ToLowerInvariant() + "|" + item.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
        return (overrides?[ClassKey(item)] ?? overrides?[legacy])?.Deserialize<string[]>();
    }
    public void SetAssignment(DocumentItem item, string[]? assignment)
    {
        var values = Preferences["class_overrides"]?.DeepClone() as JsonObject ?? new();
        values.Remove(item.FullPath.ToLowerInvariant() + "|" + item.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture));
        if (assignment is null) values.Remove(ClassKey(item)); else values[ClassKey(item)] = JsonSerializer.SerializeToNode(assignment);
        Set("class_overrides", values);
    }
}

public sealed class ScheduleRow
{
    [JsonPropertyName("kind")] public string Kind { get; set; } = "lesson";
    [JsonPropertyName("start")] public string Start { get; set; } = "";
    [JsonPropertyName("end")] public string End { get; set; } = "";
    [JsonPropertyName("classes")] public string[] Classes { get; set; } = ["", "", "", "", ""];
    [JsonPropertyName("label")] public string Label { get; set; } = "";
}

public static class ScheduleRules
{
    public static int Minutes(string text)
    {
        if (!Regex.IsMatch(text ?? "", @"^\d{2}:\d{2}$") || !TimeOnly.TryParseExact(text, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var value))
            throw new InvalidDataException("Las horas deben tener formato HH:MM (00:00–23:59).");
        return value.Hour * 60 + value.Minute;
    }
    public static List<ScheduleRow> Validate(List<ScheduleRow> rows)
    {
        foreach (var row in rows)
        {
            if (row.Label.Equals("Recreo", StringComparison.OrdinalIgnoreCase)) row.Kind = "break";
            if (row.Kind is not ("lesson" or "break") || row.Classes is null || row.Classes.Length is not (5 or 7) || row.Classes.Any(x => x is null)) throw new InvalidDataException("Fila de horario no válida.");
            if (Minutes(row.Start) >= Minutes(row.End)) throw new InvalidDataException("El inicio debe ser anterior al final.");
            row.Classes = row.Kind == "break" ? Enumerable.Repeat("Recreo", 5).ToArray() : row.Classes.Take(5).Select(x => x.Trim()).ToArray();
        }
        rows = rows.OrderBy(x => x.Start).ToList();
        var lesson = 0;
        for (var i = 0; i < rows.Count; i++)
        {
            if (i > 0 && Minutes(rows[i - 1].End) > Minutes(rows[i].Start)) throw new InvalidDataException("Las horas no pueden solaparse.");
            rows[i].Label = rows[i].Kind == "break" ? "Recreo" : $"{++lesson}ª hora";
        }
        return rows;
    }
    public static List<string[]> Choices(DateTime time, List<ScheduleRow> rows)
    {
        var day = ((int)time.DayOfWeek + 6) % 7;
        if (day >= 5) return [];
        var seconds = time.TimeOfDay.TotalSeconds;
        var matches = rows.Where(r => !string.IsNullOrWhiteSpace(r.Classes[day]) && Minutes(r.Start) * 60 <= seconds && seconds <= Minutes(r.End) * 60 + 300).OrderBy(r => r.Start).ToList();
        var lessons = matches.Where(r => r.Kind != "break").ToList();
        return (lessons.Count > 0 ? lessons : matches).Select(r => new[] { r.Label, r.Classes[day] }).ToList();
    }
}
