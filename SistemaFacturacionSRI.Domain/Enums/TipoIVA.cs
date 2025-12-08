namespace SistemaFacturacionSRI.Domain.Enums
{
    /// <summary>
    /// Define los tipos de IVA válidos en el sistema según normativa SRI Ecuador.
    /// Los valores numéricos representan el CÓDIGO SRI, no el porcentaje.
    /// </summary>
    public enum TipoIVA
    {
        /// <summary>
        /// IVA 0% - Productos exentos o tarifa 0% (código SRI: 0)
        /// Ejemplo: Medicinas, productos de la canasta básica.
        /// </summary>
        IVA_0 = 0,

        /// <summary>
        /// IVA 15% - Tarifa vigente en Ecuador (código SRI: 4)
        /// Ejemplo: Mayoría de productos y servicios gravados.
        /// </summary>
        IVA_15 = 4
    }
}