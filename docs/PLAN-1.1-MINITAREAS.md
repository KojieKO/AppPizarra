# Pizarra Pro 1.1 · Plan de minitareas

Estado inicial: 24/09/2026. Este documento organiza la hoja de ruta de `PLAN-1.0.03-1.1.00.md` según la regla de versiones acordada. Preparar el plan no cambia la versión de la aplicación: el proyecto sigue en `1.0.03` hasta que se implemente y cierre la primera minitarea.

## Regla de versiones y entregas

- Cada minitarea completada incrementa el último componente: tarea 1 → `1.1.01`, tarea 2 → `1.1.02`, etc. El número representa el trabajo acumulado hasta esa tarea, no solo sus cambios aislados.
- `1.1.00` nombra la hoja de ruta y no se publicará como versión intermedia. Con las seis tareas actuales, la primera entrega será `1.1.01` y la entrega final validada será `1.1.06`.
- Al iniciar una tarea se cambia `<Version>` en `src/PizarrasPro/PizarrasPro.csproj` a su versión asignada. `AppVersion` y el empaquetador siguen leyéndola de allí; no se duplicará la cifra en el código.
- Una tarea solo se cierra con su criterio de aceptación comprobado, un informe `docs/1.1.0N-estado.md` y una lista de pruebas realizadas y pendientes. Si necesita continuar, conserva su número: no se consume otra subsubversión por una corrección de la misma tarea.
- Para cada entrega pública se genera `PizarrasPro-1.1.0N-portable-win-x64.zip`, se comprueba extracción limpia y se actualizan juntos el ZIP visible en la raíz de GitHub, el enlace del README y GitHub Releases. No se sustituyen preferencias ni horarios personales.
- Si aparece una minitarea nueva imprescindible, se inserta antes de comenzar las siguientes y se renumeran solo las tareas aún no iniciadas. Un servicio privado opcional no reserva una versión si no llega a ser necesario.

## Inventario verificado antes de 1.1

| Área | Ya existe | Falta para 1.1 |
| --- | --- | --- |
| Aplicación local | 1.0.03 para Windows x64; archivos, conversión WBH, visor, horario, tres temas y Opciones. Portable y arranque en extracción limpia comprobados en este equipo. | Validación manual completa en otro equipo y regresión tras cada cambio de nube. |
| GitHub y descarga | Repositorio público ordenado, ZIP 1.0.03 en la raíz, enlace directo en README y publicación en Releases. | Portal público con ayuda, conexión y privacidad; actualización coherente de los tres puntos de descarga en cada entrega 1.1. |
| Interfaz de nube | Ventanas para conectar/desconectar, mostrar cuenta, elegir carpeta remota y lanzar subidas por lote. | Flujo de conexión sin pedir identificadores ni JSON al usuario, estados claros y pruebas reales. |
| OneDrive | Inicio de sesión en navegador mediante MSAL con `http://localhost`; listado de carpetas; subida simple de PDF de hasta 250 MB; comprobación remota de tamaño. | Registro de aplicación apta para uso externo, comprobación del flujo multiinstituto, retorno local y almacenamiento protegido de sesión según el diseño 1.1. |
| Google Drive | Inicio de sesión por GoogleWebAuthorizationBroker con JSON de Escritorio proporcionado por el usuario; listado de carpetas; subida y comprobación remota de tamaño y MD5. | Registro y consentimiento para usuarios externos, permisos mínimos, eliminación del requisito de JSON, flujo de retorno local y validación real. |
| Sesiones | Los tokens de Google se guardan en memoria; MSAL se crea por conexión. No hay tokens en el ZIP de distribución. | Persistencia local protegida por usuario y equipo, renovación y desconexión probadas. La protección elegida debe probarse también con un portable en USB. |
| Conservación de originales | Si la subida o la verificación remota falla, el código conserva el original y detiene el lote. | El flujo exitoso actual ejecuta «Subir y eliminar» y borra el original. 1.1 debe conservarlo siempre, también tras éxito, y verificarlo con ambos proveedores. |
| Pruebas | Batería local y pruebas de interfaz de 1.0.03; GitHub conserva las pruebas históricas en el ZIP de fuentes de 1.0.03. | Pruebas reales con cuentas Microsoft y Google personales/educativas, políticas restrictivas, cancelación, pérdida de red y sesión. |

«Ya existe» significa código o entrega observada, no integración en nube certificada: no hay evidencia de una conexión completa con cuentas reales.

## Orden de minitareas

| Tarea | Versión al cierre | Resultado principal | Estado |
| --- | --- | --- | --- |
| 1. Portal y guía pública | `1.1.01` | Web pública, ayuda, conexión y privacidad; descarga clara. | Completada el 24/09/2026; véase [informe 1.1.01](1.1.01-estado.md). |
| 2. Autenticación común y seguridad local | `1.1.02` | Navegador, retorno local, estados y almacenamiento protegido reutilizables. | Pendiente; hay flujos parciales por proveedor. |
| 3. OneDrive multiinstituto | `1.1.03` | Cuenta Microsoft propia, carpetas y subida que conserva originales. | Pendiente; hay cliente y subida sin validar. |
| 4. Google Drive externo | `1.1.04` | Cuenta Google propia sin JSON, carpetas y subida que conserva originales. | Pendiente; hay cliente y subida sin validar. |
| 5. Validación multiinstituto | `1.1.05` | Matriz de pruebas reales, correcciones y límites documentados. | Pendiente. |
| 6. Publicación estable | `1.1.06` | Portable final, ZIP raíz, README, Releases y portal sincronizados. | Pendiente. |

### 1 · Portal y guía pública → 1.1.01

Partir del repositorio y la descarga 1.0.03 ya publicados. Crear la web pública de Pizarra Pro con páginas de inicio, descarga, conexión, ayuda y privacidad. Explicar requisitos Windows, conservación de originales y estado real de los servicios. El portal solo contiene información pública; ninguna credencial, token, configuración personal o dato de usuarios. Elegir y documentar el alojamiento público antes de publicar la web. Cerrar cuando las páginas y sus enlaces sean accesibles y el portable `1.1.01` coincida con la versión mostrada en la aplicación.

### 2 · Autenticación común y seguridad local → 1.1.02

Diseñar una interfaz común para abrir el navegador, recibir el retorno temporal en `127.0.0.1`, cancelar, mostrar errores y cerrar sesión. Definir y probar PKCE donde corresponda y almacenamiento local protegido por usuario y equipo, separado de `preferencias.json` y del USB portable. No pedir al usuario archivos JSON ni secretos privados. No guardar secretos o tokens en GitHub, la web o el ZIP. Cerrar con pruebas de retorno, cancelación, error, reinicio y ausencia de credenciales en el portable. Si una limitación real de proveedor exige un backend, justificarla y añadir una minitarea versionada antes de continuar.

### 3 · OneDrive multiinstituto → 1.1.03

Registrar/configurar la aplicación pública de Microsoft para cuentas personales y educativas externas, sujetas a políticas del centro. Integrar el identificador público en la aplicación para que cada usuario solo autorice su cuenta, sin escribir un ID. Adaptar el cliente existente al flujo común; conservar selección de carpeta y verificación remota. Cambiar «Subir y eliminar» por subida que **siempre conserva el PDF local**. Cerrar con pruebas reales de cuenta personal y, si está disponible, educativa externa, y con casos de rechazo de consentimiento y fallo de red. Anotar cualquier cuenta que falte para la matriz de la tarea 5.

### 4 · Google Drive externo → 1.1.04

Registrar/configurar OAuth y consentimiento externo, revisar permisos mínimos y tramitar la verificación de Google si resulta necesaria. Sustituir la selección del JSON de Escritorio por conexión desde navegador mediante el flujo común. Reutilizar listado de carpetas y comprobación remota, reduciendo permisos cuando el caso de uso lo permita. Conservar siempre el PDF local. Cerrar con prueba real de cuenta personal y, si está disponible, Workspace educativa, más cancelación, permisos denegados y fallo de red. Registrar los límites que queden para la tarea 5.

### 5 · Validación multiinstituto → 1.1.05

Probar en instalación limpia: Microsoft personal, Microsoft 365 educativa de otro centro, Google personal, Google Workspace educativa, usuario sin permisos administrativos y centro con políticas restrictivas. Añadir red interrumpida, cancelación, sesión caducada, carpeta no accesible, colisión de nombre y comprobación de que el original local permanece tras éxito y fallo. Repetir la regresión local de archivos, conversión, visor, horario y temas. Cerrar con una matriz de resultado por escenario y límites explícitos; no presentar como validado lo que no se haya probado.

### 6 · Publicación estable → 1.1.06

Generar el portable limpio, comprobar extracción y arranque, licencias, ausencia de datos personales, SHA-256 y versión en «Acerca de». Actualizar la web, el enlace directo del README, el ZIP de la raíz y GitHub Releases a `1.1.06`, retirando de la portada la descarga antigua. Documentar instalación y conexión para centros externos. Publicar como estable solo después de demostrar al menos una conexión y subida real por proveedor y de conservar el original local en ambos casos.

## Formato del informe de cada minitarea

Crear `docs/1.1.0N-estado.md` con estas secciones: **alcance y versión**, **qué se reutilizó**, **qué cambió**, **pruebas y evidencias**, **límites pendientes**, **archivos/artefactos entregados** y **siguiente tarea**. Distinguir siempre comprobaciones con simulaciones de conexiones con cuentas reales. Guardar datos sensibles fuera del informe.
