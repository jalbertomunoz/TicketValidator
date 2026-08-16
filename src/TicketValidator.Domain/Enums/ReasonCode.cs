namespace TicketValidator.Domain.Enums;

public enum ReasonCode
{
    Ok,
    ErrNoDocumento,
    ErrNoLegible,
    ErrDocumentoManipulado,
    ErrBebidaAlcoholica,
    ErrTipoGastoIncoherente,
    ErrSinTotal,
    ErrSinFecha,
    DocumentTypeMismatch,
    DateMismatch,
    TotalMismatch,
    OcrLowConfidence
}
