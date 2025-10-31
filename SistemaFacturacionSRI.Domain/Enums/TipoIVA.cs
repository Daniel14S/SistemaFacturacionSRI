namespace SistemaFacturacionSRI.Domain.Enums
{
    /// <summary>
    /// Define los tipos de IVA válidos en el sistema según normativa SRI Ecuador.
    /// Los valores numéricos representan el porcentaje de IVA.
    /// </summary>
    public enum TipoIVA
    {
        /// <summary>
        /// IVA del 0% - Productos exentos de IVA.
        /// Ejemplo: Medicinas, productos de la canasta básica.
        /// </summary>
        IVA_0 = 0,

        /// <summary>
        /// IVA del 12% - Tarifa estándar de IVA.
        /// Ejemplo: La mayoría de productos y servicios.
        /// </summary>
        IVA_12 = 12,

        /// <summary>
        /// IVA del 15% - Tarifa especial de IVA.
        /// Ejemplo: Productos suntuarios, servicios especiales.
        /// </summary>
        IVA_15 = 15
    }
}