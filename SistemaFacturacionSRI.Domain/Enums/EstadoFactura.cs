namespace SistemaFacturacionSRI.Domain.Enums
{
    /// <summary>
    /// Estados del ciclo de vida de una factura electrónica
    /// </summary>
    public enum EstadoFactura
    {
        /// <summary>
        /// Factura creada pero no procesada
        /// </summary>
        BORRADOR = 0,

        /// <summary>
        /// XML generado según estándar SRI
        /// </summary>
        GENERADA = 1,

        /// <summary>
        /// XML firmado con certificado digital
        /// </summary>
        FIRMADA = 2,

        /// <summary>
        /// Enviada al WebService del SRI
        /// </summary>
        ENVIADA = 3,

        /// <summary>
        /// Recibida por el SRI, pendiente de autorización
        /// </summary>
        RECIBIDA = 4,

        /// <summary>
        /// Autorizada por el SRI ✅
        /// </summary>
        AUTORIZADA = 5,

        /// <summary>
        /// No autorizada por el SRI ❌
        /// </summary>
        NO_AUTORIZADA = 6,

        /// <summary>
        /// Devuelta por el SRI con errores
        /// </summary>
        DEVUELTA = 7,

        /// <summary>
        /// Factura anulada manualmente
        /// </summary>
        ANULADA = 8,

        /// <summary>
        /// Factura pendiente de reenvío al SRI (tras cambio de estado desde DEVUELTA o NO_AUTORIZADA)
        /// </summary>
        PENDIENTE = 9
    }
}
