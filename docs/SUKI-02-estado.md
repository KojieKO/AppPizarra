# SUKI-02 · Ventana principal · Completada

Fecha: 23/09/2026. Fase 1 comprobada como completada antes de comenzar. Alcance: exclusivamente fase 2 del plan; versión conservada **1.0.02**.

## Cambios
- Cabecera en una línea, título de 20 px, selector de ubicación flexible con mínimo de 160 px y ruta completa en tooltip. Horario permanece antes de Refrescar.
- Las etiquetas secundarias UBICACIÓN y versión/PORTABLE se ocultan por debajo de 1080 px de ancho de cabecera. La versión sigue en el título de ventana.
- Tabla y visor alojados en superficies con borde y radio, usando recursos dinámicos SukiUI. Sustituidos los colores fijos del visor, divisor y texto secundario.
- Espacios verticales de cabecera y pie reducidos. Se conservan la fila central expansible 1, el divisor ajustable y las cuatro filas Auto,*,Auto,Auto.
- No se ha modificado la lógica de archivos, configuración, horario, nube, columnas ni comandos. No se han tocado artefactos publicados ni documentos del usuario.

## Archivos
- src/PizarrasPro/MainWindow.cs
- tests/PizarrasPro.ThemeSmoke/Program.cs: ampliación de la comprobación existente con tamaños y límites geométricos.
- docs/validation/suki-02-build.txt y suki-02-{initial,minimum,maximized}.{txt,png}

## Verificación real
- Compilación Release final: 0 errores, 0 advertencias.
- Render nativo Avalonia de la composición real de MainWindow alojada en SukiWindow de prueba, con tema real Light/Blue. Las tres capturas se inspeccionaron visualmente.
- Inicial 1280×780: tabla 733,5×553; visor 472,5×453.
- Mínimo 900×560: tabla 505,5×333; visor 320,5×233. Todos los botones de cabecera y acciones inferiores visibles.
- Estado WindowState.Maximized real en la ventana de prueba: 1453×865 en el entorno actual; tabla 829×624; visor 536×524.
- Comprobaciones automáticas de límites de todos los elementos visibles de cabecera, presencia y tamaño de tabla/visor, y separación entre cuerpo y pie. También pasan carga de tema, seis columnas, recursos y versión.

## Límites y pendientes de fases posteriores
- Es un render de Avalonia con ventana de prueba; no una sesión interactiva completa de MainWindow. Se omiten sus eventos Opened/Closing para evitar explorar unidades o guardar configuración.
- Tabla vacía: no se han probado documentos, arrastre del divisor, selección ni operaciones de archivos en esta fase. La batería funcional no se repitió porque no cambió esa lógica.
- Encabezados abreviados y desplazamiento horizontal de tabla siguen pendientes de afinado en fase 3, igual que navegación PDF y menú contextual del visor. No se declara validación funcional de esas áreas.
- No se ha creado ZIP ni iniciado ninguna otra fase.

## Reproducción
Con DOTNET_CLI_HOME apuntando a .tools/home y NUGET_PACKAGES a .tools/packages, compilar tests/PizarrasPro.ThemeSmoke/PizarrasPro.ThemeSmoke.csproj en Release con --no-restore y -p:UsedAvaloniaProducts=.
Ejecutar ese proyecto con --no-build pasando como argumentos la ruta PNG y initial, minimum o maximized.

## Siguiente paso exacto
Abrir la tarea 3/5 cuando el usuario decida continuar. Leer el plan y este estado; ejecutar exclusivamente «Tabla, visor y menús». La fase 2 no requiere trabajo pendiente para iniciar esa fase.
