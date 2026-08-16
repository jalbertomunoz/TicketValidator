# TicketValidator

## Configuración local de OpenAI

El modelo inicial se configura en `src/TicketValidator.Api/appsettings.json` mediante la sección `OpenAI` y no contiene secretos.

Para configurar la clave únicamente en el equipo local, ejecutar desde `src/TicketValidator.Api`:

```powershell
dotnet user-secrets set "OpenAI:ApiKey" "<TU_API_KEY>"
```

Los User Secrets se almacenan fuera del repositorio. No se debe añadir la clave a `appsettings.json`, al código fuente, a logs ni a archivos incluidos en Docker.

En Render, configurar la variable de entorno `OpenAI__ApiKey`. ASP.NET Core interpreta `__` como `:`, por lo que equivale a `OpenAI:ApiKey`.
