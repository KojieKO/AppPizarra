# SUKI-05 · Validación y portable · Completada con límite documentado

Fecha: 23/09/2026. Fases 1–4 comprobadas como completadas antes de comenzar. Alcance exclusivo: fase 5; versión conservada **1.0.02**.

## Validación realizada

- Batería funcional existente: **23 correctas, 0 fallos**, con datos sintéticos en `docs/validation/suki-05-functional`.
- Comprobación nativa de interfaz con tema Light/Blue y SukiWindow: diálogos de horario, recreo, validación HH:MM, cancelación, Escape, confirmaciones destructivas, nube sin autenticación, selector de carpeta y mensajes largos. Captura `docs/validation/suki-05-main.png`; registro de salida correcto.
- Publicación Release autónoma Windows x64 compilada correctamente con .NET 10, `--self-contained`, archivo único y recursos nativos incluidos.
- Se conserva la configuración de `artifacts/PizarrasPro-1.0.01-fixed`: `preferencias.json` y `horario.json` se copiaron al nuevo portable sin usar `Backups Phyton`.
- Se incluyeron `README.txt`, `THIRD-PARTY-NOTICES.txt` y `licenses/SukiUI-6.1.1-LICENSE.txt`.

## Entregables

- Carpeta: `artifacts/PizarrasPro-1.0.02-portable-win-x64`.
- ZIP: `artifacts/PizarrasPro-1.0.02-portable-win-x64.zip`.
- SHA-256 del ejecutable: `8EFD874BC10A8C81007D43C65B933913CB4ADC7B56CF01587A68B68D53751DEC`.
- SHA-256 del ZIP: `66FEA5C92E753461845A8878E6D35BB5CF50818972816AB209F5181C5F1DF539`.

## Publicación reproducible

El procedimiento está en `scripts/publish-portable.ps1`. Detiene el proceso ante errores de restauración o publicación, publica para `win-x64`, copia la configuración vigente, añade licencias e instrucciones y comprueba que existe el ejecutable antes de terminar.

## Límite real

La ejecución final del script fue bloqueada una vez por el límite de uso del revisor automático; se ejecutaron sus pasos de publicación y empaquetado por separado. La comprobación posterior de hashes dentro de una extracción temporal del ZIP quedó pendiente porque el mismo límite impidió ejecutar ese comando. El ZIP fue creado y su hash registrado. No se autenticó en nube, no se subieron documentos ni se modificaron datos reales.

## Siguiente paso exacto

La versión 1.0.02 queda lista para entrega. Si se desea cerrar el único límite restante, ejecutar una extracción temporal del ZIP y comparar los hashes de `PizarrasPro.exe` y `preferencias.json` con la carpeta publicada.
