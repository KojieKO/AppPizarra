# SUKI-03 · Tabla, visor y menús · Completada

Fecha: 23/09/2026. Verificados los estados completos de fases 1 y 2 antes de comenzar. Alcance cerrado: exclusivamente fase 3; versión **1.0.02**. No se han iniciado otras fases, publicado paquetes ni creado tareas o agentes.

## Cambios

- Tabla SukiUI con filas compactas, tipografía de celdas de 12 px, encabezados de 11 px y menor reserva para el indicador de ordenación. Se conservan las seis columnas, selección múltiple, ordenación y desplazamiento horizontal. ARCHIVO muestra el nombre; la ruta completa queda en el tooltip de fila. Ancho inicial de archivo 300 px; los anchos guardados siguen teniendo prioridad.
- Selección con fondo del tema y borde de acento al recibir foco. Mensajes diferenciados para ubicación sin archivos o no disponible. Divisor conservado, con mínimos de 240/280 px para evitar comprimir excesivamente los paneles.
- Navegación PDF compacta, tooltips y nombres accesibles, porcentaje relativo al ajuste y botones habilitados según página, selección y estado ocupado. Se conserva Ctrl+rueda. El visor gana 25 px de altura respecto a fase 2 en los tamaños comprobados.
- Selección vacía, múltiple o WBH limpia imagen y contador y explica el siguiente paso. Cambio de documento elimina inmediatamente la imagen anterior; se mantiene el descarte de renders obsoletos.
- Corregido el tamaño lógico de la imagen: en este entorno el render nativo produjo 806×538 píxeles para una zona de ajuste de unos 448×454. El tamaño visual se calcula con la relación de aspecto y el zoom solicitados, independientemente de la resolución nativa. La comprobación final confirma que Ajustar cabe completo.
- Menú contextual del visor: Abrir en visor del sistema, Abrir carpeta y Cambiar clase. Menú de tabla conserva copia/corte/pegado, conversión, movimientos y borrado. Acciones se habilitan según selección y ocupado; al ejecutar se verifica que la selección siga siendo la misma que al abrir el menú. Clic derecho sobre otra fila la selecciona; dentro de una selección múltiple la conserva.
- Suavizado Antialias también en los menús emergentes para evitar el defecto de glifos observado al renderizarlos, ya conocido en fase 1.

## Archivos modificados

- src/PizarrasPro/MainWindow.cs
- src/PizarrasPro/FileService.cs: únicamente propiedad de presentación Name; sin cambios en operaciones de archivos.
- src/PizarrasPro/UiResources.axaml
- tests/PizarrasPro.ThemeSmoke/Program.cs y nuevo WorkspaceChecks.cs
- Este estado y evidencias docs/validation/suki-03-*

## Verificaciones realizadas

- Compilación Release final: **0 errores, 0 advertencias** (suki-03-build.txt).
- Batería funcional: **23 correctas, 0 fallos** con datos sintéticos (suki-03-tests.txt). Incluye copia, movimiento, cancelación, colisiones, horario, WBH, PDF y liberación de archivos.
- Prueba nativa Avalonia con el contenido real y tema real, alojado en SukiWindow de prueba: ordenación descendente; navegación primera/última página; zoom; Ajustar; evento Ctrl+rueda; cambio de proporciones del panel con nuevo render; anchos guardados y recargados en configuración aislada; cambios rápidos PDF/PDF/WBH; selección múltiple; lógica de selección del clic derecho; apertura real de ambos menús; rechazo de acción tras cambiar de selección; comandos deshabilitados mientras está ocupado; apertura exclusiva del PDF mostrado; imagen ajustada dentro del visor.
- Las acciones de botones se prueban mediante eventos Avalonia; la selección del clic derecho se comprueba invocando el mismo método que usa el manejador, no mediante ratón físico. El cambio del divisor se simula cambiando las proporciones de sus columnas, verificando el redimensionado y render resultantes.
- Render poblado 1280×780: tabla 733,5×553; visor 472,5×478. Render mínimo 900×560: tabla 505,5×333; visor 320,5×258. Comprobaciones geométricas de cabecera y no solapamiento del pie correctas. A tamaño mínimo las columnas restantes se consultan mediante desplazamiento horizontal.
- Capturas inspeccionadas: suki-03-workspace.png, suki-03-minimum.png y suki-03-menu.png. Registro funcional de interfaz: suki-03-workspace.txt.

## Límites y preservación

No fue una sesión manual completa de MainWindow: se omiten sus eventos Opened/Closing para no explorar unidades ni guardar preferencias del usuario. Los documentos proceden de la batería sintética; las preferencias de interfaz de prueba se guardan exclusivamente en docs/validation/suki-03-functional/ui-config. No se abrieron aplicaciones externas ni se ejecutaron borrados/movimientos desde los menús de interfaz. La lógica de archivos está cubierta por la batería sintética; los diálogos quedan para fase 4. No se usaron documentos reales, cuentas, nube, históricos Python ni artefactos publicados. No se atribuye el exceso de resolución a una causa interna concreta del renderizador.

## Reproducir

Desde la raíz, configurar DOTNET_CLI_HOME=.tools/home y NUGET_PACKAGES=.tools/packages. Usar .tools/dotnet/dotnet.exe:

1. run --project tests/PizarrasPro.Tests/PizarrasPro.Tests.csproj -c Release --no-restore -p:UsedAvaloniaProducts= -- docs/validation/suki-03-functional
2. build tests/PizarrasPro.ThemeSmoke/PizarrasPro.ThemeSmoke.csproj -c Release --no-restore -p:UsedAvaloniaProducts=
3. run --project tests/PizarrasPro.ThemeSmoke/PizarrasPro.ThemeSmoke.csproj -c Release --no-build -- docs/validation/suki-03-workspace.png workspace
4. run --project tests/PizarrasPro.ThemeSmoke/PizarrasPro.ThemeSmoke.csproj -c Release --no-build -- docs/validation/suki-03-minimum.png minimum

Detenerse si algún proceso devuelve código distinto de cero. No deshabilitar auditorías de vulnerabilidades.

## Siguiente paso exacto

Cuando el usuario lo indique, abrir la tarea **4/5 · Diálogos y ventanas secundarias**, leer el plan y los estados 1–3, y ejecutar exclusivamente fase 4 manteniendo 1.0.02. La fase 3 no deja bloqueos para comenzar la siguiente. No hay reanudaciones automáticas.
