using System.Text.Json;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace TicketValidator.Api.OpenApi;

public sealed class CamelCaseSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
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
