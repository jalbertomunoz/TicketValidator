using TicketValidator.Application.DTOs;
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
        ExpenseType expenseType,
        ExpenseCoherenceResult coherence)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        ArgumentNullException.ThrowIfNull(verification);
        ArgumentNullException.ThrowIfNull(coherence);

        if (verification.VisualDocumentType == DocumentType.NotDocument)
        {
            if (ticket.DocumentType is DocumentType.Receipt or DocumentType.Invoice)
            {
                return CreateDecision(
                    AnalysisStatus.ReviewRequired,
                    ReasonCode.DocumentTypeMismatch,
                    "La imagen no parece un ticket ni una factura, pero el OCR identifica un documento de gasto.");
            }

            return CreateDecision(
                AnalysisStatus.Rejected,
                ReasonCode.ErrNoDocumento,
                "El documento proporcionado no es un ticket ni una factura.");
        }

        if (!verification.OcrReadable && !HasSufficientVisualEvidence(verification))
        {
            return CreateDecision(
                AnalysisStatus.Unreadable,
                ReasonCode.ErrNoLegible,
                "No se ha encontrado evidencia textual OCR ni lectura visual suficiente.");
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

        if (coherence.IsCoherent is false)
        {
            var message = coherence.IncompatibleConcepts.Count == 0
                ? "La mayoría de la compra es claramente incoherente con el tipo de gasto."
                : $"La mayoría de la compra es claramente incoherente con el tipo de gasto: {string.Join(", ", coherence.IncompatibleConcepts)}.";
            return CreateDecision(
                AnalysisStatus.Rejected,
                ReasonCode.ErrTipoGastoIncoherente,
                message);
        }

        if (verification.DateMatch is false)
        {
            return CreateDecision(
                AnalysisStatus.ReviewRequired,
                ReasonCode.DateMismatch,
                "Existe una discrepancia entre OCR e IA visual en la fecha.");
        }

        if (verification.TotalMatch is false)
        {
            return CreateDecision(
                AnalysisStatus.ReviewRequired,
                ReasonCode.TotalMismatch,
                "Existe una discrepancia entre OCR e IA visual en el importe total.");
        }

        if (verification.VisualTotal is null && verification.OcrTotal is null)
        {
            return CreateDecision(
                AnalysisStatus.ReviewRequired,
                ReasonCode.ErrSinTotal,
                "No se ha podido determinar el importe total.");
        }

        if (verification.VisualDate is null && verification.OcrDate is null)
        {
            return CreateDecision(
                AnalysisStatus.ReviewRequired,
                ReasonCode.ErrSinFecha,
                "No se ha podido determinar la fecha.");
        }

        if ((verification.VisualDate is null && verification.OcrDate is not null)
            || (verification.VisualTotal is null && verification.OcrTotal is not null))
        {
            return CreateDecision(
                AnalysisStatus.ReviewRequired,
                ReasonCode.OcrLowConfidence,
                "Al menos un campo crítico solo dispone de evidencia OCR y requiere revisión visual.");
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

    private static bool HasSufficientVisualEvidence(VerificationData verification) =>
        verification.VisualDocumentType is DocumentType.Receipt or DocumentType.Invoice
        && (verification.VisualDate is not null || verification.VisualTotal is not null);
}
