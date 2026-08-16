using TicketValidator.Application.Abstractions;
using TicketValidator.Domain.Enums;
using TicketValidator.Domain.Models;
using TicketValidator.Domain.Results;

namespace TicketValidator.Application.Services;

public sealed class ExpenseRuleEngine : IExpenseRuleEngine
{
    public AnalysisDecision Evaluate(
        TicketData ticket,
        VerificationData verification,
        ExpenseType expenseType)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        ArgumentNullException.ThrowIfNull(verification);

        if (!verification.OcrReadable)
        {
            return CreateDecision(
                AnalysisStatus.Unreadable,
                ReasonCode.ErrNoLegible,
                "No se ha encontrado evidencia textual OCR.");
        }

        if (verification.ManipulationDetected is true)
        {
            return CreateDecision(
                AnalysisStatus.Rejected,
                ReasonCode.ErrDocumentoManipulado,
                "Se han detectado indicios de manipulación.");
        }

        var alcoholProduct = ticket.Products.FirstOrDefault(product =>
            product.IsAlcohol is true && !string.IsNullOrWhiteSpace(product.OcrText));
        if (alcoholProduct is not null)
        {
            return CreateDecision(
                AnalysisStatus.Rejected,
                ReasonCode.ErrBebidaAlcoholica,
                $"Se ha encontrado el concepto {alcoholProduct.OcrText}.");
        }

        if (verification.OcrTotal is null)
        {
            return CreateDecision(
                AnalysisStatus.ReviewRequired,
                ReasonCode.ErrSinTotal,
                "No se ha podido determinar el importe total mediante OCR.");
        }

        if (verification.OcrDate is null)
        {
            return CreateDecision(
                AnalysisStatus.ReviewRequired,
                ReasonCode.ErrSinFecha,
                "No se ha podido determinar la fecha mediante OCR.");
        }

        if (verification.DateMatch is false)
        {
            return CreateDecision(
                AnalysisStatus.ReviewRequired,
                ReasonCode.DateMismatch,
                "Existe una discrepancia entre OCR e IA en la fecha.");
        }

        if (verification.TotalMatch is false)
        {
            return CreateDecision(
                AnalysisStatus.ReviewRequired,
                ReasonCode.TotalMismatch,
                "Existe una discrepancia entre OCR e IA en el importe total.");
        }

        return CreateDecision(AnalysisStatus.Approved, ReasonCode.Ok, null);
    }

    private static AnalysisDecision CreateDecision(
        AnalysisStatus status,
        ReasonCode reasonCode,
        string? message) => new()
        {
            Status = status,
            ReasonCode = reasonCode,
            Message = message
        };
}
