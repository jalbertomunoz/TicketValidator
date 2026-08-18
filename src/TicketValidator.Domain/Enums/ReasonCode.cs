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
    ErrSinCif,
    ErrFechaAntigua,
    ErrFechaFutura,
    DocumentTypeMismatch,
    DateMismatch,
    TotalMismatch,
    OcrLowConfidence
}
