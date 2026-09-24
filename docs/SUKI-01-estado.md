# SUKI-01 · Base SukiUI · Completada

Fecha: 23/09/2026. Alcance cerrado: **solo fase 1/5** de `PLAN-1.0.02-SukiUI.md`. No se han iniciado fases 2–5 ni publicado un ZIP 1.0.02.

## Integración final

- `SukiUI` **6.1.1** estable, tag/commit oficial `71b826597c621c116b56b81c34ae91ba98899ad3`.
- `Avalonia.Desktop` **11.3.14**, `Avalonia.Controls.DataGrid` **11.3.13**, como en las dependencias y ejemplo oficiales de SukiUI 6.1.1. El resto de Avalonia se resuelve en 11.3.14 salvo DataGrid. Se conserva .NET 10 / Windows 10.0.19041.
- `SkiaSharp` y `SkiaSharp.NativeAssets.Linux` **3.119.4** explícitos: versión estable compatible con el mínimo preliminar 3.119.3-preview.1.1 que declara SukiUI. Los paquetes resueltos no contienen versiones preliminares. El motor Windows nativo resuelve 3.119.4. Cambio de `Concat(ref transform)` a `Concat(in transform)` conforme a esta API, sin alterar la transformación.
- Referencias fijadas en csproj y dependencias transitivas registradas en `packages.lock.json`; restauración con `--locked-mode` comprobada. Auditoría NuGet habilitada, sin advertencias durante restauración.
- `App.Initialize`: SukiTheme real, variante Light y color Blue explícito. Se eliminan FluentTheme y la inclusión Fluent de DataGrid. SukiTheme 6.1.1 incluye SimpleTheme y `Theme/DataGridStyle.axaml`; no se duplica esta inclusión.
- `MainWindow` hereda `SukiWindow`. Se elimina el fondo fijo de la ventana que ocultaba el tema; se conserva composición y lógica. Suavizado de texto Antialias explícito: el primer RenderTargetBitmap con LCD/subpíxel produjo glifos defectuosos; el render final con Antialias se inspeccionó y es legible. No se atribuye el defecto a una causa interna no demostrada.
- `UiResources.axaml`: seis pinceles semánticos enlazados dinámicamente a SukiUI (superficie, zona de trabajo, texto, texto secundario, borde y acento), espaciado, relleno de botón, radio y fuente Segoe UI común. Los recursos quedan disponibles para las siguientes fases; no se ha rediseñado toda la ventana.
- Versión única en csproj: **1.0.02**. `AppVersion` lee AssemblyInformationalVersion y proporciona título, cabecera y Producer de PDF. Versión numérica normalizada por .NET/NuGet: 1.0.2; versión visible: 1.0.02.

## Compatibilidad y fuentes

- [Paquete estable SukiUI 6.1.1](https://www.nuget.org/packages/SukiUI/6.1.1).
- [Código exacto del tag](https://github.com/kikipoulet/SukiUI/tree/6.1.1): se inspeccionaron `SukiUI/Theme/Index.axaml`, `Index.axaml.cs`, `Theme/DataGridStyle.axaml`, `ColorTheme/Light.axaml`, `Controls/SukiWindow.axaml.cs`, `Directory.Packages.props` y el nuspec descargado.
- [Arranque oficial](https://kikipoulet.github.io/SukiUI/documentation/getting-started/launch.html): SukiTheme, color explícito y SukiWindow.
- [Índice oficial SukiUI.DataGrid](https://api.nuget.org/v3-flatcontainer/sukiui.datagrid/index.json): solo nightly 7.0.2 al consultarlo. No se incorpora; el DataGrid incluido en 6.1.1 cubre esta fase.
- [SukiUI 7.0.1](https://www.nuget.org/packages/SukiUI/7.0.1) requiere Avalonia 12.0.3: descartado para evitar el salto mayor.
- [Versiones SkiaSharp](https://api.nuget.org/v3-flatcontainer/skiasharp/index.json) y [soporte SkiaSharp 3 en Avalonia](https://github.com/AvaloniaUI/Avalonia/issues/15503). La compatibilidad se contrastó con pruebas locales, no solo con mínimos de NuGet.
- Licencia SukiUI: **MIT, copyright (c) 2022 kikipoulet**. Texto conservado en `docs/licenses/SukiUI-6.1.1-LICENSE.txt`, obtenido del tag oficial. Incorporar este aviso al portable en fase 5, junto con los demás avisos de dependencias. Fuentes descargadas únicamente para inspección en `.tools/suki-reference`.

## Validación realizada

1. Restauración normal y restauración bloqueada correctas. Registro: `validation/suki-01-restore.txt`.
2. Compilación Release final: **0 errores y 0 advertencias**. Registro: `validation/suki-01-build.txt`.
3. Batería funcional existente: **23 correctas, 0 fallos**, con datos sintéticos. Incluye horario/recreo, configuración transportable, colisiones, copia/movimiento, cancelación, WBH, render nativo PDF, zoom y liberación de archivos. Registro: `validation/suki-01-tests.txt`; muestras en `validation/suki-01-functional`.
4. Inspección del PDF generado: `/Producer (Pizarras Pro 1.0.02)`.
5. Prueba reproducible `tests/PizarrasPro.ThemeSmoke`: inicializa el App real con plataforma Windows/Skia, construye MainWindow y aloja su contenido real en una SukiWindow de prueba. Comprueba Light/Blue, versión, plantillas de botones, seis cabeceras DataGrid, altura de tabla y seis pinceles no transparentes; genera `validation/suki-01-theme.png`, inspeccionada visualmente. Registro: `validation/suki-01-theme.txt`. Sin excepción de recursos, XAML ni renderizador.
6. Huellas SHA-256 antes/después: ZIP, ejecutable, preferencias y horario del portable **1.0.01 intactos**. Registros `validation/suki-01-artifacts-before.json` y `suki-01-artifacts-after.json`.
7. Comparación de contenido del ZIP antiguo con la carpeta `1.0.01-fixed`: ejecutable y horario coinciden; **preferencias distintas**. Conservar las preferencias actuales de la carpeta para fase 5; no restaurar ciegamente las del ZIP. Registro `validation/suki-01-old-zip-comparison.json`.

## Límites reales

- Se ha comprobado carga nativa del tema y render de la composición, no una sesión interactiva completa de la aplicación. La prueba de tema evita deliberadamente los eventos Opened/Closing de MainWindow: no escanea unidades ni escribe preferencias. La SukiWindow de prueba sí se abre, renderiza y cierra.
- La tabla se comprobó vacía, sin interacciones manuales. No se certifican selección/ordenación, maximizado/tamaño mínimo, menús, diálogos, navegación del visor ni nube; corresponden a fases posteriores.
- A 1280×780 algunos encabezados de tabla aparecen abreviados. Quedan pendientes densidad, anchos y cabecera adaptable. Persisten colores locales de paneles y estilos de ventanas secundarias previstos para fases 2–4. No confundir esta captura de base con el diseño final.
- No se han usado cuentas reales ni subido documentos. No se han alterado históricos Python ni datos publicados. Los binarios de `bin/.../win-x64` pueden seguir siendo 1.0.01: no se republicó; usar el build sin RID para esta validación.
- La primera restauración fue interrumpida por límite de uso del revisor automático. Tras «termina el trabajo», la nueva restauración fue autorizada y completada; no queda un bloqueo de permisos.

## Archivos de implementación

`src/PizarrasPro/PizarrasPro.csproj`, `packages.lock.json`, `Program.cs`, `MainWindow.cs`, `WbhConverter.cs`, nuevos `AppVersion.cs` y `UiResources.axaml`; lock actualizado de `tests/PizarrasPro.Tests`; nuevo proyecto `tests/PizarrasPro.ThemeSmoke` con su lock; este estado, licencia y evidencias.

## Reproducir

Desde la raíz en PowerShell:

```powershell
$env:DOTNET_CLI_HOME = Join-Path $PWD '.tools/home'
$env:NUGET_PACKAGES = Join-Path $PWD '.tools/packages'
& .tools/dotnet/dotnet.exe restore tests/PizarrasPro.Tests/PizarrasPro.Tests.csproj --locked-mode -p:UsedAvaloniaProducts=
& .tools/dotnet/dotnet.exe build tests/PizarrasPro.Tests/PizarrasPro.Tests.csproj -c Release --no-restore -p:UsedAvaloniaProducts=
& .tools/dotnet/dotnet.exe run --project tests/PizarrasPro.Tests/PizarrasPro.Tests.csproj -c Release --no-build -- docs/validation/suki-01-functional
& .tools/dotnet/dotnet.exe restore tests/PizarrasPro.ThemeSmoke/PizarrasPro.ThemeSmoke.csproj --locked-mode -p:UsedAvaloniaProducts=
& .tools/dotnet/dotnet.exe run --project tests/PizarrasPro.ThemeSmoke/PizarrasPro.ThemeSmoke.csproj -c Release --no-restore -p:UsedAvaloniaProducts=
```

Detenerse si cualquier comando devuelve código distinto de cero. `UsedAvaloniaProducts=` evita el registro de telemetría fuera del workspace; no desactiva auditoría de vulnerabilidades.

## Siguiente paso exacto

**Tarea 2/5 · Ventana principal**: leer este estado y el plan; aplicar los recursos SukiUI a cabecera, ubicación, botones y superficies; mantener fila central 1 y divisor; comprobar visualmente apertura, maximizado y tamaño mínimo; dejar `docs/SUKI-02-estado.md`. No iniciar aquí esa tarea ni publicar todavía.
