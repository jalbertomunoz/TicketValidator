using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TicketValidator.Api.Configuration;
using TicketValidator.Api.Contracts;
using TicketValidator.Application.UseCases.AnalyzeTicket;
using TicketValidator.Domain.Enums;
using TicketValidator.Domain.Models;

namespace TicketValidator.Api.Controllers;

[ApiController]
[Route("api/v1/tickets")]
public sealed class TicketsController : ControllerBase
{
    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly HashSet<string> SupportedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/jpg",
        "application/octet-stream"
    };
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png"
    };

    private readonly AnalyzeTicketHandler _handler;
    private readonly UploadOptions _uploadOptions;

    public TicketsController(AnalyzeTicketHandler handler, IOptions<UploadOptions> uploadOptions)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _uploadOptions = uploadOptions?.Value ?? throw new ArgumentNullException(nameof(uploadOptions));
    }

    /// <summary>Analiza un ticket o factura de gasto mediante OCR, Inteligencia Artificial y reglas de negocio.</summary>
    /// <response code="200">Análisis realizado correctamente, incluso para resultados funcionales de rechazo o revisión.</response>
    /// <response code="400">Petición inválida, archivo ausente, formato no soportado, firma inválida o tamaño excedido.</response>
    /// <response code="500">Error técnico no controlado.</response>
    [HttpPost("analyze")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(AnalyzeTicketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AnalyzeTicketResponse>> AnalyzeAsync(
        [FromForm] AnalyzeTicketRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || request is null || !Enum.IsDefined(request.ExpenseType) || request.ExpenseType == ExpenseType.Unknown)
        {
            return BadRequest("El tipo de gasto no es válido.");
        }

        var file = request.File;
        if (file is null || file.Length == 0)
        {
            return BadRequest("Debe adjuntarse una imagen no vacía.");
        }

        if (file.Length > _uploadOptions.MaxFileSizeBytes)
        {
            return BadRequest($"La imagen supera el límite de {_uploadOptions.MaxFileSizeBytes} bytes.");
        }

        if (!SupportedExtensions.Contains(Path.GetExtension(file.FileName)))
        {
            return BadRequest("La extensión del archivo debe ser .jpg, .jpeg o .png.");
        }

        byte[] image;
        await using (var stream = new MemoryStream())
        {
            await file.CopyToAsync(stream, cancellationToken);
            image = stream.ToArray();
        }

        if (!HasExpectedSignature(image, Path.GetExtension(file.FileName)))
        {
            return BadRequest("El contenido del archivo no coincide con el formato indicado.");
        }

        if (!HasCompatibleContentType(file.ContentType, Path.GetExtension(file.FileName)))
        {
            return BadRequest("El archivo debe ser una imagen JPEG o PNG.");
        }

        var result = await _handler.HandleAsync(
            new AnalyzeTicketCommand(image, request.ExpenseType),
            cancellationToken);

        return Ok(MapResponse(result));
    }

    private static bool HasExpectedSignature(byte[] image, string extension) =>
        (extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
            ? image.AsSpan().StartsWith(JpegSignature)
            : extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
                && image.AsSpan().StartsWith(PngSignature);

    private static bool HasCompatibleContentType(string contentType, string extension)
    {
        if (!SupportedContentTypes.Contains(contentType))
        {
            return false;
        }

        if (contentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            ? contentType.Equals("image/png", StringComparison.OrdinalIgnoreCase)
            : contentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase)
                || contentType.Equals("image/jpg", StringComparison.OrdinalIgnoreCase);
    }

    private static AnalyzeTicketResponse MapResponse(AnalyzeTicketResult result) => new()
    {
        AnalysisId = result.AnalysisId,
        Status = MapStatus(result.Decision.Status),
        ReasonCode = MapReasonCode(result.Decision.ReasonCode),
        Message = result.Decision.Message,
        Ticket = MapTicket(result.Ticket),
        Verification = MapVerification(result.Verification, result.OcrRawText)
    };

    private static TicketResponse MapTicket(TicketData ticket) => new()
    {
        DocumentType = MapDocumentType(ticket.DocumentType),
        EstablishmentName = ticket.EstablishmentName,
        EstablishmentType = ticket.EstablishmentType?.ToString(),
        Address = ticket.Address is null ? null : new AddressResponse
        {
            Street = ticket.Address.Street,
            City = ticket.Address.City,
            PostalCode = ticket.Address.PostalCode,
            Country = ticket.Address.Country
        },
        TaxId = ticket.TaxId,
        InvoiceNumber = ticket.InvoiceNumber,
        Date = ticket.Date,
        Time = ticket.Time,
        Total = ticket.Total,
        Products = ticket.Products.Select(product => new ProductResponse
        {
            Concept = product.Concept,
            NormalizedText = product.NormalizedText,
            Amount = product.Amount,
            Category = product.Category?.ToString(),
            IsAlcohol = product.IsAlcohol
        }).ToArray(),
        VatDetails = ticket.VatDetails.Select(vat => new VatResponse
        {
            Rate = vat.Rate,
            TaxableAmount = vat.TaxableAmount,
            Amount = vat.Amount
        }).ToArray()
    };

    private static VerificationResponse MapVerification(VerificationData verification, string? ocrRawText) => new()
    {
        OcrReadable = verification.OcrReadable,
        OcrRawText = ocrRawText,
        VisualDocumentType = MapDocumentType(verification.VisualDocumentType),
        DateMatch = verification.DateMatch,
        OcrDate = verification.OcrDate,
        VisualDate = verification.VisualDate,
        TotalMatch = verification.TotalMatch,
        OcrTotal = verification.OcrTotal,
        VisualTotal = verification.VisualTotal,
        ManipulationDetected = verification.ManipulationDetected
    };

    private static string MapStatus(AnalysisStatus status) => status switch
    {
        AnalysisStatus.Approved => "APPROVED",
        AnalysisStatus.Rejected => "REJECTED",
        AnalysisStatus.ReviewRequired => "REVIEW_REQUIRED",
        AnalysisStatus.Unreadable => "UNREADABLE",
        AnalysisStatus.ProcessingError => "PROCESSING_ERROR",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };

    private static string MapReasonCode(ReasonCode reasonCode) => reasonCode switch
    {
        ReasonCode.Ok => "OK",
        ReasonCode.ErrNoDocumento => "ERR_NO_DOCUMENTO",
        ReasonCode.ErrNoLegible => "ERR_NO_LEGIBLE",
        ReasonCode.ErrDocumentoManipulado => "ERR_DOCUMENTO_MANIPULADO",
        ReasonCode.ErrBebidaAlcoholica => "ERR_BEBIDA_ALCOHOLICA",
        ReasonCode.ErrTipoGastoIncoherente => "ERR_TIPO_GASTO_INCOHERENTE",
        ReasonCode.ErrSinTotal => "ERR_SIN_TOTAL",
        ReasonCode.ErrSinFecha => "ERR_SIN_FECHA",
        ReasonCode.ErrSinCif => "ERR_SIN_CIF",
        ReasonCode.ErrFechaAntigua => "ERR_FECHA_ANTIGUA",
        ReasonCode.ErrFechaFutura => "ERR_FECHA_FUTURA",
        ReasonCode.DocumentTypeMismatch => "DOCUMENT_TYPE_MISMATCH",
        ReasonCode.DateMismatch => "DATE_MISMATCH",
        ReasonCode.TotalMismatch => "TOTAL_MISMATCH",
        ReasonCode.OcrLowConfidence => "OCR_LOW_CONFIDENCE",
        _ => throw new ArgumentOutOfRangeException(nameof(reasonCode), reasonCode, null)
    };

    private static string? MapDocumentType(DocumentType? documentType) => documentType switch
    {
        DocumentType.Receipt => "TICKET",
        DocumentType.Invoice => "FACTURA",
        DocumentType.NotDocument => "NO_DOCUMENTO",
        DocumentType.Unknown => "UNKNOWN",
        null => null,
        _ => throw new ArgumentOutOfRangeException(nameof(documentType), documentType, null)
    };
}
