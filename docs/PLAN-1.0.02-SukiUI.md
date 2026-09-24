# Pizarras Pro 1.0.02 · Migración visual a SukiUI

Solicitud del 23/09/2026: renovar la interfaz utilizando https://github.com/kikipoulet/SukiUI y dividir el trabajo en cinco tareas cortas y reanudables.

## Forma de trabajo

- Las cinco tareas forman una sola subsubversión: **1.0.02**. No incrementar la versión al terminar cada fase.
- Ejecutar una fase cada vez, en el orden indicado. Todas trabajan sobre el mismo directorio y no hay repositorio Git.
- Solo la tarea 1 comienza ahora. Las tareas 2–5 quedan preparadas para que el usuario las continúe cuando le convenga. No encadenar tareas ni programar reanudaciones automáticas.
- Al terminar o interrumpirse una fase, escribir `docs/SUKI-01-estado.md` (o 02–05) con cambios, archivos, decisiones, verificaciones reales, asuntos pendientes y siguiente paso exacto. Actualizarlo durante la fase si el trabajo se prolonga. No dar una fase por validada solo porque compila.
- Si una dependencia anterior no está terminada, informar de ella y terminar el turno sin esperar ni sondear continuamente.
- Dividir en tareas facilita retomar el trabajo, pero el límite de uso es compartido por la cuenta y no se reinicia al abrir otra tarea.

## Base inspeccionada

- Directorio: `C:\Users\kojie\OneDrive - educarex.es\AppPizarras`.
- Proyecto principal: `src/PizarrasPro/PizarrasPro.csproj`. Versión inicial 1.0.01; .NET 10, Windows 10.0.19041.0, Avalonia 11.3.12.
- Interfaz construida en C#: `Program.cs`, `MainWindow.cs`, `ScheduleWindow.cs`, `CloudWindow.cs`.
- Lógica: `Configuration.cs`, `FileService.cs`, `WbhConverter.cs`, `PdfPreview.cs`, `CloudDrive.cs`.
- Pruebas existentes: `tests/PizarrasPro.Tests`. La batería anterior reportó 23 casos correctos; confirmar sobre el código modificado cuando corresponda.
- SDK local: `.tools/dotnet/dotnet.exe`; caché de paquetes `.tools/packages`; CLI home `.tools/home`.
- Artefactos observados: `artifacts/PizarrasPro-1.0.01-fixed` y `artifacts/PizarrasPro-1.0.01-portable-win-x64.zip`. La carpeta tiene cambios más recientes que el ZIP; comprobar contenidos y configuración actual antes de migrar, sin asumir que son idénticos.
- Archivo histórico Python: `Backups Phyton`. No modificarlo.
- Fallo de diseño conocido y corregido: la fila central expansible es la **1** en `Auto,*,Auto,Auto`; tabla y visor no deben compartir fila con el pie ni quedar desplazados al fondo.

## Diseño y funciones que se conservan

- Aspecto SukiUI real, con paleta clara azul como punto de partida, tipografía legible, separación contenida, botones consistentes y estados de interacción visibles. Los colores deben usar los recursos del tema; evitar fondos fijos que tapen SukiUI.
- Mantener la interfaz compacta, la cabecera en una sola línea, el divisor entre tabla y PDF y la mayor superficie posible para el visor. A tamaños estrechos, evitar recortes de botones mediante tamaños mínimos razonables y reducción del contenido secundario.
- Mantener retirados los botones inferiores específicos del visor. «Cambiar clase…» y «Abrir en visor del sistema» deben estar disponibles mediante menú contextual, también accesible desde el visor. No eliminar funciones de la aplicación al simplificar su aspecto.
- Tabla con ARCHIVO, TIPO, TAMAÑO, FECHA, HORA y CLASE; anchos manuales persistentes, selección múltiple, ordenación y desplazamiento. Mantener Horario antes de Refrescar.
- Visor: páginas, zoom, ajuste, Ctrl+rueda, respuesta al redimensionado, descarte de renders antiguos y liberación de archivos antes de mover/borrar.
- Horario lunes–viernes, recreo estructural, validación HH:MM y reglas actuales de inferencia. No sustituir las reglas de 0.9.7 por reglas anteriores de memoria.
- Conservar copia/movimiento verificados, cancelación, colisiones sin sobrescritura, conversión WBH, confirmaciones de borrado y limpieza opcional.
- Nube: inicio de sesión controlado por el usuario, tokens solo en memoria. La migración visual no autoriza subir documentos ni iniciar sesión en cuentas reales para probar.
- Portable autónomo Windows x64: configuración junto al ejecutable, rutas transportables, sin instalación de .NET para el usuario. No añadir servicios externos ni un instalador.
- Puede eliminarse el artefacto de una subsubversión anterior **después** de comprobar el nuevo paquete y preservar la configuración actual. No borrar documentos del usuario ni los históricos Python. Antes de cualquier borrado, verificar rutas absolutas y contenido.

## 1/5 · Base SukiUI

**Objetivo:** integrar el tema y las dependencias de forma compatible y compilable.

- Consultar la documentación y versiones publicadas; fijar versiones estables compatibles con el proyecto, sin asumir que `main` o la versión más reciente es compatible.
- Integrar SukiTheme con color explícito y SukiWindow siguiendo la API de la versión elegida. Resolver los estilos de DataGrid: evaluar el paquete SukiUI.DataGrid y evitar mezclar estilos incompatibles.
- Ajustar únicamente lo necesario para el arranque del tema y definir recursos reutilizables para las siguientes fases; no rediseñar toda la ventana en esta fase.
- Centralizar la versión visible 1.0.02 para evitar discrepancias con csproj, título y metadatos de los PDF. Conservar el artefacto publicado 1.0.01 mientras se trabaja.
- Comprobar restauración, compilación y carga del tema. Guardar versiones, licencia, fuentes y resultados en `docs/SUKI-01-estado.md`.

**Termina cuando:** dependencias fijadas, tema integrado, compilación comprobada y estado de arranque documentado con sus límites. No publicar el ZIP definitivo aún.

## 2/5 · Ventana principal

**Depende de:** tarea 1 terminada.

- Aplicar la composición visual SukiUI a la ventana principal: cabecera compacta, ubicación, botones y superficies de trabajo.
- Reemplazar colores duros por recursos del tema, ordenar espaciados y mantener densidad apropiada para trabajar con pizarras.
- Evitar la regresión de filas solapadas. Verificar tabla y visor visibles al abrir, al maximizar y a tamaño mínimo; cabecera sin controles cortados.
- Comprobar visualmente la ventana real o mediante un render de Avalonia y registrar qué tipo de comprobación se realizó. Compilar y dejar `docs/SUKI-02-estado.md`.

**Termina cuando:** estructura principal coherente y comprobada, sin cambiar la lógica de archivos.

## 3/5 · Tabla, visor y menús

**Depende de:** tarea 2 terminada.

- Afinar DataGrid, selección, encabezados, foco, estados vacíos y representación PDF/WBH usando SukiUI.
- Estilizar la barra de navegación PDF, páginas, zoom y ajuste. Conservar superficie del visor y paneles ajustables.
- Completar el menú contextual de tabla y visor con las acciones que sustituyen los botones inferiores. Los comandos deben operar sobre el documento correcto y respetar selección y estado ocupado.
- Verificar ajuste, zoom, varias páginas, tamaño de columnas, cambio rápido de selección y apertura de menús con archivos de prueba. Evitar tocar PDF reales del usuario.
- Compilar y dejar `docs/SUKI-03-estado.md`.

**Termina cuando:** la zona de trabajo y los menús funcionan con el nuevo diseño y se ha comprobado el comportamiento relevante.

## 4/5 · Diálogos y ventanas secundarias

**Depende de:** tarea 3 terminada.

- Unificar horario, cambio de clase, confirmaciones, errores y configuración de nube con los componentes y recursos de SukiUI.
- Mantener distinción entre avisos y decisiones destructivas; Cancelar, Escape, foco, modalidad y mensajes claros.
- Conservar recreo como separador y edición lunes–viernes. No modificar el comportamiento de los conectores de nube como parte del restyling.
- Verificar ventanas con datos sintéticos, incluidas dimensiones mínimas y texto largo. No autenticar ni subir archivos reales.
- Compilar y dejar `docs/SUKI-04-estado.md`.

**Termina cuando:** todas las superficies visibles mantienen el mismo diseño y las confirmaciones conservan su significado.

## 5/5 · Validación y ZIP portable

**Depende de:** tareas 1–4 terminadas.

- Ejecutar las pruebas funcionales existentes y verificaciones relevantes de interfaz; revisar regresiones de la migración visual, redimensionado y lectura de PDF sintéticos.
- Crear un script de publicación reproducible que se detenga si falla compilación o publicación y nunca empaquete una salida incompleta. Verificar cada resultado y conservar identificadores de procesos si los comandos continúan.
- Publicar una carpeta autónoma y un ZIP `PizarrasPro-1.0.02-portable-win-x64.zip`, con configuración actual preservada, sin reutilizar ciegamente `Backups Phyton/preferencias.json`.
- Incluir instrucciones breves de uso/actualización y avisos de licencia de las bibliotecas redistribuidas.
- Abrir/probar la publicación con copias de prueba y verificar que el ZIP contiene el mismo ejecutable y configuración. Si algún paso visual o cuenta real no se ha probado, indicarlo sin declarar validación total.
- Solo después de verificar el nuevo paquete se pueden retirar los artefactos antiguos autorizados. No borrar un ejecutable en uso; informar si queda pendiente.
- Entregar enlaces del ZIP y ejecutable, y `docs/SUKI-05-estado.md` con pruebas, resultados y límites.

**Termina cuando:** existe un portable verificado y entregable, y el estado final permite saber exactamente qué se ha probado.

## Referencias verificadas al preparar el plan

- Repositorio: https://github.com/kikipoulet/SukiUI
- Inicio e integración: https://kikipoulet.github.io/SukiUI/documentation/getting-started/launch.html
- Límites compartidos de Codex: https://learn.chatgpt.com/docs/pricing

## Tareas creadas en Codex

Estas tareas usan el proyecto local App Pizarras. La primera está iniciada; las otras se han preparado sin iniciar su implementación. Para continuar, abrir la siguiente en orden y escribir «Comienza esta tarea».

| Fase | Identificador de la tarea |
| --- | --- |
| 1 · Base SukiUI | 01a0cddf-ea4b-7631-84ad-1187ce44713c |
| 2 · Ventana principal | 01a0cde0-063e-7d63-96c4-ef11ea7631c0 |
| 3 · Tabla, visor y menús | 01a0cde0-1873-7731-9686-4e9c363e89c3 |
| 4 · Diálogos y horario | 01a0cde0-29c6-7531-89bd-52dc7aea5884 |
| 5 · Validación y portable | 01a0cde0-409c-7c70-b8b5-882861806d8a |

## Notas de compilación

Usar el SDK del proyecto con `DOTNET_CLI_HOME` y `NUGET_PACKAGES` dirigidos a `.tools`. Anteriormente Avalonia intentó escribir un registro de compilación fuera del workspace; `-p:UsedAvaloniaProducts=` permitió compilar sin esa tarea de telemetría. Verificar que esta propiedad siga siendo aplicable. No desactivar validaciones de seguridad ni el análisis de vulnerabilidades. Solicitar escalación solo si sigue siendo necesaria conforme a las herramientas disponibles.
