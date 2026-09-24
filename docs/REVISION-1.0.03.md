# Revisión para publicación · 1.0.03

Fecha: 24/09/2026. Base: código C# actual, estados SUKI-01–05 y 1.0.03-01–05. Se conserva 1.0.03 y la licencia CC0 ya presente en GitHub.

## Cambios de esta revisión

- README completo: instalación, requisitos reales de Windows, operaciones, horario, temas, configuración, actualización, WBH, nube, desarrollo y pruebas.
- Empaquetador con versión leída del proyecto, SDK local o instalado, dependencias bloqueadas y creación del ZIP. Rechaza destinos existentes y exteriores al proyecto; ya no borra un portable anterior ni sus preferencias.
- Inventario de dependencias y avisos de licencia obtenidos de sus paquetes NuGet, con los textos del runtime .NET. Se conserva la licencia original de SukiUI.
- Distribución limpia generada desde las fuentes actuales, sin preferencias, horario personal, credenciales ni registros. La carpeta portable de uso anterior permanece intacta.
- Exclusiones de Git para salidas de compilación, históricos Python, configuración personal y credenciales.

## Comprobaciones realizadas

| Comprobación | Resultado |
| --- | --- |
| Compilación Release | Correcta. |
| Batería funcional | 23 correctas, 0 fallos; datos sintéticos. |
| ThemeSmoke `workspace` | PDF, navegación, zoom, ajuste, selección, menús, columnas y liberación del documento: correcto. |
| ThemeSmoke `dialogs` | Horario, validación, cancelación, recreo, foco seguro, Escape y ventanas de nube sin conexión: correcto. |
| ThemeSmoke `minimum` | Tamaño 900 × 560, controles y distribución: correcto. |
| ThemeSmoke `appearance` | Tres temas, persistencia, contraste, cambios simulados del sistema y colores del PDF: correcto. |
| Capturas | Revisadas visualmente las capturas nuevas del espacio de trabajo y tema oscuro. |
| Auditoría NuGet | Consulta en línea completada; no se notifican paquetes vulnerables, incluidas dependencias transitivas, en los orígenes consultados. |
| ZIP portable | 93 archivos verificados por SHA-256 tras extracción; sin configuración personal. |
| Arranque desde extracción limpia | Proceso activo en este Windows; cerrado después de la comprobación. |
| Protección del empaquetador | Rechaza una carpeta portable existente sin borrarla. |

La primera compilación mostró NU1900 por acceso a NuGet; la consulta posterior con acceso de red completó la auditoría. La ausencia de avisos conocidos no equivale a una auditoría integral de seguridad.

## Límites

- Las pruebas de interfaz utilizan ventanas Avalonia y eventos de ensayo. No constituyen una sesión manual completa ni una validación en otro equipo.
- El modo Según el sistema se comprueba con eventos simulados, no cambiando las preferencias reales de Windows.
- OAuth, subida y eliminación tras subida no se probaron con cuentas reales. El README deja explícito que la acción actual elimina el original después de la verificación remota.
- La conversión WBH admite un subconjunto de formas y puede recurrir a la imagen compuesta. No se certifican todos los formatos WBH.
- El proyecto y el portable actual son Windows x64; no se certifican Linux/macOS.
- No se modificó la lógica funcional ni se inició la hoja de ruta 1.1.00.

Los comandos reproducibles figuran en el README. Las evidencias extensas de esta ejecución permanecen en `artifacts/`; los estados anteriores documentan sus propias comprobaciones históricas.
