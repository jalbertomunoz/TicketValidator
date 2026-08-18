using System.Text;
using TicketValidator.Domain.Models;

namespace TicketValidator.Infrastructure.AI.Prompts;

internal static class ProductClassificationPrompt
{
    internal const string SystemMessage = """
        Eres un clasificador semantico de lineas de producto ya extraidas de un ticket.

        Tu unica responsabilidad es clasificar cada producto por su denominacion completa. No extraigas empresa, fecha, total ni otros datos. No decidas si un ticket es valido, no apliques reglas de negocio y no analices manipulacion.

        Devuelve exactamente una clasificacion por cada indice recibido. No devuelvas ni reformules el concepto.

        category solo puede ser: food, nonAlcoholicBeverage, alcoholicBeverage, other o unknown. Usa unknown cuando no exista certeza suficiente.
        isAlcohol es true solo si el concepto completo identifica con suficiente certeza una bebida alcoholica; false si identifica con suficiente certeza un producto no alcoholico; null ante duda.
        Analiza el concepto completo, no palabras aisladas ni asociaciones semanticas. CEREZAS es food e isAlcohol false; no puede reinterpretarse como LICOR DE CEREZAS. LICOR DE CEREZAS es alcoholicBeverage e isAlcohol true. HAMBURGUESA PLASTICA, FLAUTA DE BACON Y QUESO y BOCADILLO VEGETAL son food.
        Una denominacion explicita de bebida sin alcohol prevalece sobre palabras como cerveza, vino, cava o sidra. CERVEZA SIN ALCOHOL, CERVEZA 0,0, CERVEZA 0.0, CERVEZA 0,0 MAHOU, HEINEKEN 0.0 cuando identifica claramente la version sin alcohol, VINO SIN ALCOHOL, SIDRA SIN ALCOHOL, alcohol free, non-alcoholic, zero alcohol y sin alcoh. son nonAlcoholicBeverage e isAlcohol false. Si la variante comercial no permite saberlo con certeza, usa unknown y null.
        """;

    internal static string CreateUserMessage(IReadOnlyList<ProductData> products)
    {
        var message = new StringBuilder();

        for (var index = 0; index < products.Count; index++)
        {
            var concept = products[index].Concept;
            if (!string.IsNullOrWhiteSpace(concept))
            {
                message.Append(index).Append(": ").AppendLine(concept);
            }
        }

        return message.ToString();
    }
}
