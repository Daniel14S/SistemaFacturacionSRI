namespace SistemaFacturacionSRI.Domain.Enums
{
    /// <summary>
    /// Posibles estados del ciclo de vida de una factura electrónica.
    /// </summary>
    public enum EstadoFactura
    {
        BORRADOR,
        GENERADA,
        FIRMADA,
        ENVIADA,
        RECIBIDA,
        AUTORIZADA,
        NO_AUTORIZADA,
        DEVUELTA,
        ANULADA
    }
}
