using System.Globalization;
using System.Text;
using TicketValidator.Domain.Enums;
using TicketValidator.Domain.Models;

namespace TicketValidator.Infrastructure.AI.Prompts;

internal static class ExpenseCoherencePrompt
{
    internal const string SystemMessage = """
        Eres un analizador semantico de coherencia entre una compra y un tipo de gasto. Tu unica responsabilidad es determinar si el conjunto de productos es razonablemente coherente con el tipo indicado.

        Devuelve isCoherent true cuando la compra sea razonablemente coherente, false solo cuando la mayoria sea claramente incoherente y null cuando no haya evidencia suficiente. Valora preferentemente el peso economico cuando haya importes y, si no lo hay, el numero de lineas. Un unico articulo secundario incompatible no vuelve incoherente toda la compra. incompatibleIndexes solo incluye conceptos claramente incompatibles.

        Usa el concepto OCR completo y el contexto del establecimiento. Para Meals, Diet, Breakfast, Lunch y Dinner son coherentes principalmente alimentacion, comida preparada, bebidas no alcoholicas y consumo inmediato. En restauracion, chuletón, carne, pescado, pollo, pizza, hamburguesa, menu, bocadillo, ensalada, cafe y agua son validos. En supermercado, los preparados y listos para consumir son validos, mientras que carne o pescado crudo, ingredientes para cocinar y productos no alimentarios solo hacen incoherente la compra si constituyen la mayoria. HAMBURGUESA PLASTICA, FLAUTA DE BACON Y QUESO, BOCADILLO VEGETAL y CEREZAS no son incompatibles por si mismos.

        Para Fuel busca gasolina, diesel, gasoleo, GLP, recarga electrica o equivalentes. Para Accommodation busca principalmente alojamiento, habitacion u otros servicios hoteleros. Para Taxi busca taxi, VTC o transporte urbano equivalente. Para Parking busca parking, aparcamiento o estacionamiento. Para Material busca material de oficina, consumibles, herramientas o material informatico y tecnico. Para Highway considera coherente un peaje o concepto de autopista salvo evidencia clara de otro tipo de gasto. Para Other se conservador: ante duda devuelve null o true, nunca false por intuicion. El alcohol se evalua mediante una regla independiente y no debe hacer que la compra sea incoherente por si solo.

        No decidas estados, codigos de error ni prioridades. No analices manipulacion, no extraigas fecha o total, no modifiques productos y no reclasifiques alcohol.
        """;

    internal static string CreateUserMessage(TicketData ticket, ExpenseType expenseType)
    {
        var message = new StringBuilder();
        message.Append("ExpenseType: ").AppendLine(expenseType.ToString());
        message.Append("EstablishmentType: ").AppendLine(ticket.EstablishmentType?.ToString() ?? "Unknown");
        message.Append("EstablishmentName: ").AppendLine(ticket.EstablishmentName ?? "null");
        message.AppendLine("Products:");

        for (var index = 0; index < ticket.Products.Count; index++)
        {
            var product = ticket.Products[index];
            if (string.IsNullOrWhiteSpace(product.OcrText))
            {
                continue;
            }

            message.Append(index).Append(": ").Append(product.OcrText);
            if (product.Amount is not null)
            {
                message.Append(" | amount: ").Append(product.Amount.Value.ToString("0.00", CultureInfo.InvariantCulture));
            }

            if (product.Category is not null)
            {
                message.Append(" | category: ").Append(product.Category);
            }

            if (product.IsAlcohol is not null)
            {
                message.Append(" | isAlcohol: ").Append(product.IsAlcohol.Value);
            }

            message.AppendLine();
        }

        return message.ToString();
    }
}
