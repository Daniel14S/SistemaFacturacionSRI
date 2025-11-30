namespace SistemaFacturacionSRI.Domain.Enums
{
    /// <summary>
    /// Ambiente de emisión del comprobante
    /// </summary>
    public enum Ambiente
    {
        /// <summary>
        /// Ambiente de pruebas del SRI (celcer)
        /// </summary>
        PRUEBAS = 1,

        /// <summary>
        /// Ambiente de producción del SRI (cel)
        /// </summary>
        PRODUCCION = 2
    }
}
