using System.Reflection;

namespace PizarrasPro;

public static class AppVersion
{
    // The project Version is the single source for UI and generated PDF metadata.
    public static string Display { get; } = typeof(AppVersion).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion.Split('+')[0];
    public static string Name => "Pizarra Pro";
    public static string Product => Name + " " + Display;
    public static string WindowTitle => Name;
}
