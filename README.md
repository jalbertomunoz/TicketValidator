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
3. Elegir una imagen JPEG o PNG en `file`.
4. Indicar el `expenseType`.
5. Ejecutar la petición con **Execute**.

Swagger UI permanece disponible también en Render como interfaz técnica de
demostración del MVP académico.
