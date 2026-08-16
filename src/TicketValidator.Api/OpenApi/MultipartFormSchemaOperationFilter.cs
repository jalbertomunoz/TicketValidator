using System.Text.Json;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace TicketValidator.Api.OpenApi;

public sealed class MultipartFormSchemaOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var requestBody = operation.RequestBody;
        if (requestBody is null
            || !requestBody.Content.TryGetValue("multipart/form-data", out var mediaType)
            || mediaType?.Schema is not { } schema)
        {
            return;
        }

        foreach (var property in schema.Properties.ToArray())
        {
            var camelCaseName = JsonNamingPolicy.CamelCase.ConvertName(property.Key);
            if (camelCaseName == property.Key)
            {
                continue;
            }

            schema.Properties.Remove(property.Key);
            schema.Properties[camelCaseName] = property.Value;

            if (schema.Required.Remove(property.Key))
            {
                schema.Required.Add(camelCaseName);
            }
        }
    }
}
