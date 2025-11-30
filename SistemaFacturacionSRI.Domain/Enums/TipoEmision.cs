namespace SistemaFacturacionSRI.Domain.Enums
{
    /// <summary>
    /// Tipo de emisión del comprobante electrónico
    /// </summary>
    public enum TipoEmision
    {
        /// <summary>
        /// Emisión normal (online)
        /// </summary>
        NORMAL = 1,

        /// <summary>
        /// Emisión por contingencia (offline)
        /// </summary>
        CONTINGENCIA = 2
    }
}
