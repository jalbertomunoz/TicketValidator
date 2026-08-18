# TicketValidator

## Configuración local de OpenAI

El modelo inicial se configura en `src/TicketValidator.Api/appsettings.json` mediante la sección `OpenAI` y no contiene secretos.

Para configurar la clave únicamente en el equipo local, ejecutar desde `src/TicketValidator.Api`:

```powershell
dotnet user-secrets set "OpenAI:ApiKey" "<TU_API_KEY>"
```

Los User Secrets se almacenan fuera del repositorio. No se debe añadir la clave a `appsettings.json`, al código fuente, a logs ni a archivos incluidos en Docker.

En Render, configurar la variable de entorno `OpenAI__ApiKey`. ASP.NET Core interpreta `__` como `:`, por lo que equivale a `OpenAI:ApiKey`.

## Swagger / OpenAPI

Con la API en ejecución, abrir `https://localhost:<puerto>/swagger`. El puerto
depende del perfil configurado en `launchSettings.json`.

1. Abrir `POST /api/v1/tickets/analyze`.
2. Seleccionar **Try it out**.
3. Elegir una imagen `.jpg`, `.jpeg` o `.png` en `file`.
4. Indicar el `expenseType`.
5. Ejecutar la petición con **Execute**.

Swagger UI permanece disponible también en Render como interfaz técnica de
demostración del MVP académico.

## Auditoría en fichero

Cada análisis genera una línea técnica en `logs/ticket-validator.log` por
defecto, relativa al directorio de ejecución de la aplicación. La sección
`AuditLog` de `appsettings.json` permite configurar el directorio y el nombre
del fichero.

El log contiene el identificador de análisis, tipo de gasto, estado, código de
motivo, duración y datos básicos de error. No almacena imágenes, OCR completo,
prompts, respuestas de OpenAI ni secretos. El directorio `logs/` está excluido
de Git.

## Testing

Ejecutar toda la suite con:

```powershell
dotnet test
```

Los tests unitarios no llaman a OpenAI. Los tests de integración habituales
tampoco consumen la API de OpenAI ni requieren `OPENAI_API_KEY`; las pruebas
del pipeline y de la API sustituyen las dependencias externas por fakes
controlados. Las pruebas reales contra OpenAI se ejecutan manualmente y quedan
fuera de la suite normal.

## Política de lectura

Tras pruebas experimentales del MVP con tickets reales, la IA visual es la
fuente principal para leer fecha y total directamente de la imagen. OCR se
conserva como evidencia textual independiente para contrastar esos campos, y la
IA sobre OCR mantiene la extracción estructurada auxiliar. El código toma siempre
la decisión final.

Con OCR parcial, la ausencia de fecha o total OCR no impide usar y aprobar los
campos visuales. Con OCR nulo, una lectura visual suficiente conserva esos
valores pero devuelve `REVIEW_REQUIRED / OCR_LOW_CONFIDENCE`, ya que no hay
contraste independiente. Sin OCR ni evidencia visual suficiente se devuelve
`UNREADABLE / ERR_NO_LEGIBLE`. Si ambos valores difieren, el resultado es
revisión; si solo existe OCR para un campo crítico, también se solicita revisión
conservadora.

## Docker

Build:

```powershell
docker build -t ticketvalidator .
```

Run:

```powershell
docker run --rm -p 8080:8080 -e OpenAI__ApiKey="<TU_API_KEY>" ticketvalidator
```

Swagger está disponible en `http://localhost:8080/swagger`. Para conservar los
logs opcionalmente, añadir `-v ticketvalidator-logs:/app/logs` al comando.

## Web Demo

La interfaz web incluida en `wwwroot` es una herramienta auxiliar para pruebas
manuales y demostración. No forma parte del núcleo funcional de TicketValidator
ni contiene lógica de negocio.

Como ayuda de diagnóstico, la respuesta de análisis expone `ocrRawText` dentro
de `verification` y la demo permite visualizarlo. Este dato no interviene en
las reglas, no se persiste y no se registra en logs.

- Web demo: `http://localhost:<puerto>/`
- Swagger: `http://localhost:<puerto>/swagger`
