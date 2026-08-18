using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TicketValidator.Application.Abstractions;
using TicketValidator.Domain.Enums;
using TicketValidator.Domain.Results;

namespace TicketValidator.Infrastructure.Logging;

public sealed class FileAuditLogger : IAuditLogger
{
    private static readonly SemaphoreSlim WriteLock = new(1, 1);
    private readonly AuditLogOptions _options;
    private readonly ILogger<FileAuditLogger> _logger;

    public FileAuditLogger(IOptions<AuditLogOptions> options, ILogger<FileAuditLogger> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task LogAsync(
        Guid analysisId,
        ExpenseType expenseType,
        AnalysisDecision? decision,
        TimeSpan duration,
        Exception? error,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await WriteLock.WaitAsync(cancellationToken);
            try
            {
                var directoryPath = ResolveDirectoryPath();
                Directory.CreateDirectory(directoryPath);

                var line = CreateLine(analysisId, expenseType, decision, duration, error);
                var filePath = Path.Combine(directoryPath, _options.FileName);
                await File.AppendAllTextAsync(filePath, line + Environment.NewLine, Encoding.UTF8, cancellationToken);
            }
            finally
            {
                WriteLock.Release();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                "No se pudo escribir el log de auditoría. ErrorType={ErrorType} ErrorMessage={ErrorMessage}",
                exception.GetType().Name,
                Sanitize(exception.Message));
        }
    }

    private string ResolveDirectoryPath() => Path.GetFullPath(
        string.IsNullOrWhiteSpace(_options.DirectoryPath) ? "logs" : _options.DirectoryPath,
        AppContext.BaseDirectory);

    private static string CreateLine(
        Guid analysisId,
        ExpenseType expenseType,
        AnalysisDecision? decision,
        TimeSpan duration,
        Exception? error)
    {
        var status = decision is null ? "PROCESSING_ERROR" : FormatStatus(decision.Status);
        var reasonCode = decision is null ? string.Empty : FormatReasonCode(decision.ReasonCode);
        var durationMs = Math.Max(0, (long)duration.TotalMilliseconds);

        return string.Join(" | ",
            $"Timestamp={DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture)}",
            $"AnalysisId={analysisId}",
            $"ExpenseType={expenseType}",
            $"Status={status}",
            $"ReasonCode={reasonCode}",
            $"DurationMs={durationMs}",
            $"ErrorType={error?.GetType().Name ?? string.Empty}",
            $"ErrorMessage={Sanitize(error?.Message)}");
    }

    private static string FormatStatus(AnalysisStatus status) => status switch
    {
        AnalysisStatus.Approved => "APPROVED",
        AnalysisStatus.Rejected => "REJECTED",
        AnalysisStatus.ReviewRequired => "REVIEW_REQUIRED",
        AnalysisStatus.Unreadable => "UNREADABLE",
        AnalysisStatus.ProcessingError => "PROCESSING_ERROR",
        _ => status.ToString()
    };

    private static string FormatReasonCode(ReasonCode reasonCode) => reasonCode switch
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
        _ => reasonCode.ToString()
    };

    private static string Sanitize(string? value) => string.IsNullOrWhiteSpace(value)
        ? string.Empty
        : value.Replace("\r", " ").Replace("\n", " ").Replace("|", "/").Trim();
}
