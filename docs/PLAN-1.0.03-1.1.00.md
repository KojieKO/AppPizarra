# Pizarra Pro 1.0.03 y 1.1.00

## Objetivo

Preparar Pizarra Pro 1.0.03 como una versión local estable y ordenada, y dejar definida la hoja de ruta completa de Pizarra Pro 1.1.00 para la distribución multiinstituto y la conexión web con OneDrive y Google Drive.

La 1.0.03 no debe depender de la implementación OAuth de la 1.1.00. Primero se consolida la aplicación local; después se aborda la conexión web externa.

## Pizarra Pro 1.0.03

### Minitarea 1 · Identidad y About

- Cambiar el nombre visible a `Pizarra Pro`.
- Quitar la versión del título y de la cabecera principal.
- Mantener la versión centralizada en `AppVersion`.
- Crear una ventana o página «Acerca de Pizarra Pro».
- Mostrar allí la versión real de la compilación, inicialmente `1.0.03`.

Resultado: nombre limpio en la aplicación y un único lugar para consultar la versión.

### Minitarea 2 · Tema y apariencia

- Añadir en Opciones los modos `Luminoso`, `Oscuro` y `Según el sistema`.
- Persistir la selección en la configuración existente.
- Aplicar el tema al iniciar.
- Aplicarlo inmediatamente al cambiarlo.
- Comprobar también ventanas secundarias y diálogos.

Resultado: toda la aplicación sigue una única preferencia visual.

### Minitarea 3 · Navegación principal

- Crear tres pestañas principales: `Archivos`, `Horario` y `Opciones`.
- Integrar la vista actual de archivos en `Archivos`.
- Integrar el horario actual en `Horario`.
- Mantener las operaciones existentes: selección, vista previa, conversión y subida preparada.

Resultado: la aplicación tiene una estructura coherente tipo aplicación de Windows moderna.

### Minitarea 4 · Página de Opciones

- Crear secciones para `Apariencia`, `Almacenamiento`, `Servicios conectados` y `Acerca de`.
- Mover a esta página las opciones actualmente dispersas en menús o ventanas.
- Mantener las preferencias existentes y migrarlas sin pérdida.
- Dejar los botones de OneDrive y Google Drive preparados para la futura 1.1.00, sin cambiar todavía el mecanismo de conexión.

Resultado: la configuración queda centralizada sin mezclar todavía el trabajo OAuth.

### Minitarea 5 · Verificación y entrega 1.0.03

- Ejecutar las pruebas actuales y añadir las mínimas de nombre, versión, tema y navegación.
- Compilar la aplicación.
- Revisar visualmente los tres temas y las tres pestañas.
- Probar la conservación de preferencias y archivos existentes.
- Generar el portable `PizarraPro-1.0.03-portable-win-x64.zip`.
- Documentar expresamente cualquier límite de verificación visual o de dispositivo real.

Resultado: una 1.0.03 local, estable y preparada para servir de base a 1.1.00.

## Criterio de cierre de 1.0.03

No se inicia la implementación de 1.1.00 hasta que:

- la aplicación arranque correctamente;
- el nombre sea `Pizarra Pro`;
- el About muestre la versión correcta;
- los tres temas funcionen y se conserven;
- las tres pestañas sean utilizables;
- archivos, horario, vista previa y configuración existente sigan funcionando;
- el portable se haya generado y probado en una instalación limpia.

## Pizarra Pro 1.1.00 · Hoja de ruta preparada

La 1.1.00 será la versión de distribución multiinstituto. Cada usuario autorizará su propia cuenta y sus propias carpetas, sin depender de un instituto concreto ni de la provincia.

### Fase 1 · Portal AppPizarra

- Revisar y ordenar `KojieKO/AppPizarra`.
- Publicar la web pública de Pizarra Pro.
- Añadir descarga del portable mediante GitHub Releases.
- Añadir páginas de conexión, ayuda y privacidad.
- Separar claramente contenido público de cualquier configuración sensible.

### Fase 2 · Diseño común de autenticación

- Usar el navegador del usuario y OAuth con PKCE.
- No pedir JSON OAuth al usuario.
- No guardar secretos ni tokens en GitHub Pages, el repositorio ni el portable.
- Usar retorno local temporal mediante `127.0.0.1`.
- Mostrar estados de conexión, cancelación y error.
- Definir almacenamiento local protegido por usuario y equipo.

### Fase 3 · OneDrive

- Registrar una aplicación Microsoft de uso externo/multiinstituto.
- Permitir cuentas Microsoft personales y educativas, sujeto a las políticas de cada organización.
- Implementar autorización web desde Pizarra Pro.
- Recibir la autorización en la instalación local.
- Mantener selección de carpeta, subida y conservación del original local.

### Fase 4 · Google Drive

- Registrar una aplicación OAuth para usuarios externos.
- Configurar consentimiento, privacidad y permisos mínimos.
- Completar la verificación de Google si fuese necesaria.
- Implementar autorización con navegador y PKCE.
- Recibir la autorización en la instalación local.
- Mantener selección de carpeta, subida y conservación del original local.

### Fase 5 · Servicio privado, solo si fuese necesario

GitHub Pages será suficiente para el portal público. Solo se añadirá un backend privado si Microsoft o Google requieren un intercambio que no pueda realizarse con PKCE y retorno local.

Posibles alojamientos: Azure Functions, Cloudflare Workers u otra función segura con variables secretas. Esta decisión se tomará después de validar los flujos reales, no antes.

### Fase 6 · Validación multiinstituto

- Cuenta Microsoft personal.
- Cuenta Microsoft 365 educativa de otro instituto.
- Cuenta Google personal.
- Cuenta Google Workspace educativa.
- Usuario sin permisos administrativos.
- Centro con políticas restrictivas.
- Instalación limpia sin configuración previa.
- Fallos de red, cancelación y pérdida de sesión.

### Fase 7 · Release 1.1.00

- Publicar el portable estable en GitHub Releases.
- Actualizar el portal para mostrar la versión estable.
- Documentar instalación y conexión para centros externos.
- Confirmar que los originales locales se conservan cuando una subida falla.
- Publicar 1.1.00 solo después de validar al menos una cuenta de cada proveedor.

## Orden de ejecución

### Minitareas creadas en App Pizarras

Preparadas sin iniciar implementación. Abrir cada tarea en orden y escribir «Comienza esta tarea». Comparten el directorio local: ejecutarlas consecutivamente, no a la vez. Cada una deja su informe en `docs/1.0.03-0N-estado.md` para facilitar el relevo. El tamaño se limita por alcance; no se garantiza un consumo fijo del límite de uso.

| Tarea | Identificador |
| --- | --- |
| 1/5 · Identidad y Acerca de | 01a0cef9-df87-7700-8612-2bc61813425a |
| 2/5 · Temas luminoso, oscuro y sistema | 01a0cef9-f11a-7970-b034-4b7f02101566 |
| 3/5 · Archivos, Horario y Opciones | 01a0cefa-04bd-76a2-b9fe-23138a1574ad |
| 4/5 · Organizar Opciones | 01a0cefa-17b1-7bc3-91ab-d53079219a96 |
| 5/5 · Validación y portable | 01a0cefa-2982-7d63-9fd6-4162b33d964a |

La tarea 2 prepara un selector reutilizable; las tareas 3 y 4 lo incorporan a la navegación y a Opciones. La tarea 5 entrega el portable local y distingue las pruebas en un directorio limpio de las realizadas en otro equipo. La publicación y la nueva autenticación quedan para 1.1.00.

1. 1.0.03 · Identidad y About.
2. 1.0.03 · Tema y apariencia.
3. 1.0.03 · Navegación principal.
4. 1.0.03 · Página de Opciones.
5. 1.0.03 · Verificación y portable.
6. 1.1.00 · Portal AppPizarra.
7. 1.1.00 · Autenticación común.
8. 1.1.00 · OneDrive.
9. 1.1.00 · Google Drive.
10. 1.1.00 · Validación multiinstituto y publicación.

## Regla de alcance

La 1.0.03 no incorpora todavía la migración completa a OAuth web. La 1.1.00 no se empieza hasta cerrar la 1.0.03 y conservar este documento como referencia de alcance.
