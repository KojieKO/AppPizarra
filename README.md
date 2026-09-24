# Pizarra Pro

Aplicación portable para Windows que organiza archivos de pizarra **PDF y WBH**, convierte WBH a PDF y permite consultar los documentos junto al horario de clases.

**Versión actual: 1.0.03.** Implementada en C# con .NET 10, Avalonia y SukiUI. El nombre del ejecutable se conserva como `PizarrasPro.exe`. Esta entrega corresponde al proyecto C# actual; las antiguas versiones Python son históricos locales.

## Descarga y puesta en marcha

Descarga el ZIP portable desde [las publicaciones de GitHub](https://github.com/KojieKO/AppPizarra/releases). El código se encuentra en este repositorio y también se distribuye como ZIP de fuentes.

1. Extrae **todo** el ZIP en una carpeta del equipo o de una memoria USB con permiso de escritura.
2. Abre `PizarrasPro.exe`. No requiere instalar Python, .NET ni un lector PDF adicional.
3. Elige la carpeta de origen y pulsa **Refrescar**. Se buscan PDF y WBH en sus subcarpetas; se omiten enlaces y puntos de reanálisis.
4. Configura el destino de PDF en **Opciones → Almacenamiento** y las clases en **Horario**.

Requisitos: Windows x64, Windows 10 versión 2004 (compilación 19041) o posterior, incluido Windows 11. Esta entrega utiliza el motor PDF de Windows y **no es una distribución para Linux o macOS**. La interfaz admite un tamaño mínimo de 900 × 560; las columnas restantes se consultan mediante desplazamiento horizontal.

## Funciones

| Sección | Contenido |
| --- | --- |
| Archivos | Tabla con archivo, tipo, tamaño, fecha, hora y clase; selección múltiple, ordenación y anchos guardados. |
| Visor | PDF dentro de la aplicación, navegación por páginas, zoom, ajustar y Ctrl + rueda. Los WBH se convierten antes de visualizarse. |
| Horario | Editor de lunes a viernes, tramos sin solapamientos y recreo estructural. |
| Opciones | Apariencia, origen y destino, servicios conectados y Acerca de. |
| Apariencia | Luminoso, Oscuro o Según el sistema, con persistencia y aplicación inmediata. El tema no altera los colores del PDF. |

La fecha se extrae de nombres como `pdf_2026-09-24-10-05-09.pdf` o `2026-09-24-10-05-09.wbh`; si no coinciden, se usa la fecha de modificación. La asignación de clase contempla cinco minutos de tolerancia al final de cada tramo, prioriza clases frente al recreo cuando coinciden y permite elegir entre las clases coincidentes desde el menú contextual cuando hay dos o más opciones.

## Operaciones con archivos

- **Convertir WBH** genera PDF en la carpeta de origen seleccionada. Conserva el WBH por defecto; la eliminación posterior es una casilla explícita y exige comprobar el PDF. Sin selección se procesan los WBH de la lista.
- **Mover PDF al destino** mueve únicamente PDF y no limpia sus carpetas de origen. Sin selección se procesan los PDF pendientes de la lista.
- **Mover a raíz** está disponible en unidades extraíbles o rutas que contienen `Archivo de pizarra` / `Archivos de Pizarra`. Puede quitar el prefijo `pdf_` a nombres con fecha. La limpieza opcional, desmarcada inicialmente, **borra también WBH y otros archivos de carpetas que ya no contienen PDF**.
- **Copiar, cortar y pegar** operan desde el menú contextual o con Ctrl+C, Ctrl+X y Ctrl+V, mediante el portapapeles interno de la aplicación. Las transferencias verifican SHA-256 y generan nombres alternativos ante colisiones.
- **Borrar** y la tecla Supr eliminan definitivamente los archivos seleccionados tras confirmación: no pasan por la papelera.
- Las operaciones por lotes muestran progreso y permiten solicitar cancelación. La conversión de un WBH en curso puede tardar en terminar antes de atenderla.

## Compatibilidad WBH

El conversor interpreta páginas del contenedor WBH, trazos `GeneralPen` e imágenes `InsertImage` con transformaciones compatibles. Ordena las páginas numéricamente y usa las imágenes insertadas originales.

Cuando no puede reconstruir una página, intenta usar su imagen compuesta de previsualización. Esa alternativa puede tener menor resolución. Si tampoco hay una representación compatible, la conversión falla y conserva el original: no se garantiza compatibilidad con todos los productores y variantes WBH. Comprueba el resultado antes de eliminar originales importantes.

## Configuración y actualización

La configuración se guarda junto al ejecutable:

| Archivo | Uso |
| --- | --- |
| `preferencias.json` | Ubicaciones recientes, destino, columnas, apariencia y asignaciones manuales de clase. |
| `horario.json` | Horario semanal. |
| `registros/pizarras.log` | Diagnóstico; puede contener rutas de documentos. |

Las rutas interiores y las del mismo USB se guardan de forma relativa cuando corresponde. El ZIP de distribución no incluye preferencias, horarios personales ni credenciales.

Para actualizar, cierra la aplicación, extrae la nueva versión en otra carpeta y copia allí tus `preferencias.json` y `horario.json`, conservando una copia de la versión anterior. No ejecutes el empaquetador sobre tu carpeta de uso. Si las preferencias están dañadas, la aplicación informa del problema y evita sobrescribirlas; guarda una copia antes de corregirlas o retirarlas.

## OneDrive y Google Drive: integración pendiente de validación real

Hay código de conexión OAuth, selección de carpeta remota y subida. **Esta versión no ha sido validada de extremo a extremo con cuentas reales** y no se presenta como una función certificada. La evolución de los servicios conectados permanece en la [hoja de ruta](https://github.com/KojieKO/AppPizarra/blob/main/docs/PLAN-1.0.03-1.1.00.md).

El diálogo actual solicita un identificador de aplicación pública de Microsoft Entra con redirección `http://localhost` y permiso `Files.ReadWrite`, o un JSON OAuth de Google de tipo Escritorio con Drive API habilitada. Google solicita el alcance de Drive completo. El inicio de sesión se realiza en el navegador; las sesiones y tokens de la aplicación se mantienen en memoria. El identificador de Microsoft se guarda en preferencias; el JSON de Google se carga en memoria.

La acción actual es **Subir y eliminar**: tras confirmación, comprueba el archivo remoto y elimina el original local. OneDrive verifica tamaño y admite subidas simples de hasta 250 MB; Google comprueba tamaño y MD5. Ante fallo se conserva el original y se detiene el lote. No uses documentos únicos para validar esta integración.

## Compilar desde el código fuente

Necesitas Windows y el **SDK de .NET 10**. Desde la raíz del repositorio:

```powershell
dotnet restore src/PizarrasPro/PizarrasPro.csproj --locked-mode
dotnet build src/PizarrasPro/PizarrasPro.csproj -c Release --no-restore
dotnet run --project src/PizarrasPro/PizarrasPro.csproj -c Release
```

Las versiones directas y transitivas están registradas en los archivos `packages.lock.json`. La auditoría NuGet está activada. Si aparece `NU1900`, la compilación puede finalizar pero **la consulta de vulnerabilidades no se ha completado**.

Para crear una distribución nueva, ejecuta en PowerShell:

```powershell
./scripts/publish-portable.ps1
# Si el destino ya existe, elige otro:
./scripts/publish-portable.ps1 -Output artifacts/nueva-entrega
```

El script lee la versión del proyecto, utiliza el SDK local `.tools/dotnet` si existe o `dotnet` del sistema, restaura con bloqueo de dependencias y publica un ejecutable autónomo `win-x64`. Incluye documentación y licencias, crea el ZIP y muestra su SHA-256. Rechaza destinos existentes y rutas exteriores al proyecto.

## Pruebas y alcance de la revisión

Los proyectos de pruebas son ejecutables de comprobación; se lanzan con `dotnet run`, no con `dotnet test`:

```powershell
dotnet run --project tests/PizarrasPro.Tests -c Release -- artifacts/suki-03-functional
dotnet run --project tests/PizarrasPro.ThemeSmoke -c Release -- artifacts/review-minimum.png minimum
dotnet run --project tests/PizarrasPro.ThemeSmoke -c Release -- artifacts/review-workspace.png workspace
dotnet run --project tests/PizarrasPro.ThemeSmoke -c Release -- artifacts/review-dialogs.png dialogs
dotnet run --project tests/PizarrasPro.ThemeSmoke -c Release -- artifacts/review-appearance.png appearance
```

El modo `workspace` necesita los datos sintéticos de la primera orden en `suki-03-functional`, junto a la captura de salida. Las comprobaciones cubren horario, configuración, transferencias, colisiones, cancelación, limpieza, conversión WBH, render PDF e interfaz. Las ventanas de ensayo omiten el arranque y cierre normales de la ventana principal para evitar escaneos y cambios de preferencias personales.

Consulta [la revisión de publicación](https://github.com/KojieKO/AppPizarra/blob/main/docs/REVISION-1.0.03.md) y los estados de desarrollo en `docs/`. Las pruebas automatizadas y el arranque en este equipo no sustituyen la validación manual completa en otro ordenador. No se certifican OAuth real, Linux/macOS ni todas las variantes WBH.

## Estructura

```text
src/PizarrasPro/             Aplicación C#, recursos y dependencias bloqueadas
tests/PizarrasPro.Tests/     Comprobaciones funcionales con documentos sintéticos
tests/PizarrasPro.ThemeSmoke/Comprobaciones de interfaz Avalonia
scripts/                    Publicación del portable
docs/                       Planes, estados y licencias de terceros
artifacts/                  Salidas locales de compilación y pruebas (excluidas de Git)
```

## Licencias

Se conserva la licencia **CC0 1.0 Universal** ya existente en el repositorio: [LICENSE](LICENSE). Las dependencias conservan sus propias licencias; SukiUI utiliza MIT. El portable incluye los avisos y textos de dependencias disponibles en sus paquetes NuGet, además de los avisos del runtime .NET. La licencia del proyecto no sustituye las de terceros.
