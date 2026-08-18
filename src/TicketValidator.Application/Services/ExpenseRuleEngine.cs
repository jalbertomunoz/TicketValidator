using TicketValidator.Application.DTOs;
using TicketValidator.Application.Abstractions;
using TicketValidator.Domain.Enums;
using TicketValidator.Domain.Models;
using TicketValidator.Domain.Results;

namespace TicketValidator.Application.Services;

public sealed class ExpenseRuleEngine : IExpenseRuleEngine
{
    private readonly TimeProvider _timeProvider;

    public ExpenseRuleEngine(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

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
            product.IsAlcohol is true && !string.IsNullOrWhiteSpace(product.Concept));
        if (alcoholProduct is not null)
        {
            return CreateDecision(
                AnalysisStatus.Rejected,
                ReasonCode.ErrBebidaAlcoholica,
                $"Se ha encontrado el concepto {alcoholProduct.Concept}.");
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

        if (verification.DateMatch is not true || verification.TotalMatch is not true)
        {
            return CreateDecision(
                AnalysisStatus.ReviewRequired,
                ReasonCode.OcrLowConfidence,
                "La fecha y el importe total deben estar corroborados por OCR e IA visual.");
        }

        if (!verification.OcrReadable)
        {
            return CreateDecision(
                AnalysisStatus.ReviewRequired,
                ReasonCode.OcrLowConfidence,
                "La lectura visual no ha podido contrastarse porque OCR no ha obtenido evidencia textual.");
        }

        if (verification.DateMatch is true && verification.VisualDate is { } ticketDate)
        {
            var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
            if (ticketDate > today)
            {
                return CreateDecision(
                    AnalysisStatus.ReviewRequired,
                    ReasonCode.ErrFechaFutura,
                    "La fecha del documento es posterior a la fecha actual y requiere revisión.");
            }

            if (ticketDate.Year < today.Year)
            {
                return CreateDecision(
                    AnalysisStatus.ReviewRequired,
                    ReasonCode.ErrFechaAntigua,
                    "La fecha del documento corresponde a un año anterior al actual y requiere revisión.");
            }
        }

        if (RequiresTaxId(expenseType) && string.IsNullOrWhiteSpace(ticket.TaxId))
        {
            return CreateDecision(
                AnalysisStatus.ReviewRequired,
                ReasonCode.ErrSinCif,
                "No se ha podido determinar el CIF/NIF del emisor para este tipo de gasto.");
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

    private static bool RequiresTaxId(ExpenseType expenseType) => expenseType is
        ExpenseType.Meals
        or ExpenseType.Diet
        or ExpenseType.Breakfast
        or ExpenseType.Lunch
        or ExpenseType.Dinner
        or ExpenseType.Material;
}
