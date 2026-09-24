# SUKI-04 · Diálogos y ventanas secundarias · Completada

Fecha: 23/09/2026. Verificados los estados completos de fases 1–3. Alcance exclusivo: fase 4; versión conservada **1.0.02**. No se ha iniciado fase 5 ni creado ZIP, tareas, agentes o automatizaciones.

## Cambios
- Nuevo SecondaryUi: ventanas SukiWindow, render de texto Antialias, posición centrada en propietario, Escape y botones de cierre comunes.
- Mensajes, errores y cambio de clase usan el mismo diálogo modal con cuerpo desplazable y acciones fijas. El cambio de clase presenta Guardar.
- Borrado definitivo y subida que elimina el original muestran advertencia explícita y acciones Eliminar / Subir y eliminar. Cancelar recibe el foco; la acción destructiva no es el botón predeterminado. Cerrar o Escape no confirma.
- Horario SukiUI de 1040×620, mínimo 740×420; cinco días y recreo estructural conservados. Contenido horizontal desplazable al reducir la ventana (mínimo interno 920); botones y error quedan fuera del desplazamiento. Recursos SukiUI para recreo y error HH:MM.
- Configuración de nube SukiUI con mínimo 520×400, pestañas desplazables y Cerrar fijo. Selector de carpeta con mínimo 580×360 y Cancelar explícito.
- Factorías separadas de la apertura modal permiten probar las composiciones reales sin activar escaneos ni conexiones. El selector remoto sigue cargando al abrirse desde el flujo real.
- Sin cambios en CloudDrive, reglas de horario, operaciones de archivos, configuración publicada ni versión.

## Archivos
- src/PizarrasPro/SecondaryUi.cs (nuevo)
- src/PizarrasPro/MainWindow.cs, ScheduleWindow.cs, CloudWindow.cs, UiResources.axaml
- tests/PizarrasPro.ThemeSmoke/Program.cs y DialogChecks.cs (nuevo)
- docs/validation/suki-04-* y este estado

## Validación real
- Compilación Release final: **0 errores, 0 advertencias**, suki-04-build.txt.
- Batería funcional: **23 correctas, 0 fallos**, suki-04-tests.txt, documentos sintéticos en suki-04-functional.
- Prueba nativa Avalonia con tema real y ventanas secundarias reales abiertas como modales sobre un propietario de prueba. Comprueba rechazo de HH:MM inválido, Cancelar sin guardar horario, recreo preservado, selección de clase y resultado afirmativo, Escape con resultado falso en confirmación destructiva y aviso, foco inicial seguro y ausencia de botón destructivo predeterminado.
- Dimensiones mínimas y texto largo: botones de acción fijos dentro del ancho de ventana. Los controles contenidos en áreas desplazables pueden requerir desplazamiento, como corresponde al horario y la nube.
- Capturas inspeccionadas: suki-04-schedule.png, schedule-minimum, cloud-microsoft, cloud-google, folder, delete, class y error. Registro suki-04-dialogs.txt.
- Selector de carpetas probado con fila sintética y sin invocar su carga remota. No se autenticó, subió ni eliminó ningún documento real.

## Límites
No es una sesión manual completa: se usan eventos Avalonia y capturas RenderTargetBitmap. MainWindow no ejecuta sus eventos de apertura/cierre; sus preferencias se sustituyen en la prueba por una configuración aislada en suki-04-synthetic. Los conectores reales, navegador OAuth y operaciones remotas no se probaron ni modificaron. El guardado de asignación sigue cubierto por la batería funcional; la prueba visual comprueba selección y resultado del diálogo. La validación integral y el portable corresponden a fase 5.

## Reproducir
Desde la raíz, configurar DOTNET_CLI_HOME=.tools/home y NUGET_PACKAGES=.tools/packages. Usar .tools/dotnet/dotnet.exe:
1. build tests/PizarrasPro.ThemeSmoke/PizarrasPro.ThemeSmoke.csproj -c Release --no-restore -p:UsedAvaloniaProducts=
2. run --project tests/PizarrasPro.ThemeSmoke/PizarrasPro.ThemeSmoke.csproj -c Release --no-build -- docs/validation/suki-04-main.png dialogs
3. run --project tests/PizarrasPro.Tests/PizarrasPro.Tests.csproj -c Release --no-restore -p:UsedAvaloniaProducts= -- docs/validation/suki-04-functional
Detenerse ante cualquier salida no cero.

## Siguiente paso exacto
Cuando el usuario lo indique, abrir la tarea **5/5 · Validación y ZIP portable**, leer el plan y estados 1–4, ejecutar exclusivamente la fase 5 y mantener 1.0.02. Preservar la configuración actual de la carpeta portable anterior conforme a SUKI-01. La fase 4 no deja bloqueos para comenzar fase 5.
