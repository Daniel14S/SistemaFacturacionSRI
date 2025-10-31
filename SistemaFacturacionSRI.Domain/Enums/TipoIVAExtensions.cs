namespace SistemaFacturacionSRI.Domain.Enums
{
    /// <summary>
    /// Métodos de extensión para el enum TipoIVA.
    /// Facilitan el trabajo con tipos de IVA en el sistema.
    /// </summary>
    public static class TipoIVAExtensions
    {
        /// <summary>
        /// Obtiene el porcentaje de IVA como decimal para cálculos.
        /// Ejemplo: IVA_12.ObtenerPorcentaje() retorna 0.12
        /// </summary>
        /// <param name="tipoIVA">El tipo de IVA</param>
        /// <returns>Porcentaje en formato decimal (0.12 para 12%)</returns>
        public static decimal ObtenerPorcentaje(this TipoIVA tipoIVA)
        {
            // Convertimos el valor del enum a decimal y dividimos entre 100
            // Ejemplo: 12 / 100 = 0.12
            return (decimal)tipoIVA / 100;
        }

        /// <summary>
        /// Calcula el valor del IVA sobre un monto base.
        /// Ejemplo: Si precio = 100 y IVA = 12%, retorna 12
        /// </summary>
        /// <param name="tipoIVA">El tipo de IVA a aplicar</param>
        /// <param name="montoBase">El monto base sobre el cual calcular el IVA</param>
        /// <returns>El valor del IVA calculado</returns>
        public static decimal CalcularIVA(this TipoIVA tipoIVA, decimal montoBase)
        {
            // Fórmula: montoBase × (porcentaje / 100)
            // Ejemplo: 100 × (12 / 100) = 12
            return montoBase * tipoIVA.ObtenerPorcentaje();
        }

        /// <summary>
        /// Calcula el precio total incluyendo el IVA.
        /// Ejemplo: Si precio = 100 y IVA = 12%, retorna 112
        /// </summary>
        /// <param name="tipoIVA">El tipo de IVA a aplicar</param>
        /// <param name="montoBase">El precio base sin IVA</param>
        /// <returns>Precio total (base + IVA)</returns>
        public static decimal CalcularTotal(this TipoIVA tipoIVA, decimal montoBase)
        {
            // Fórmula: montoBase + IVA
            // Ejemplo: 100 + 12 = 112
            return montoBase + tipoIVA.CalcularIVA(montoBase);
        }

        /// <summary>
        /// Obtiene una descripción legible del tipo de IVA.
        /// Útil para mostrar en interfaces de usuario.
        /// </summary>
        /// <param name="tipoIVA">El tipo de IVA</param>
        /// <returns>Descripción del tipo de IVA (ej: "IVA 12%")</returns>
        public static string ObtenerDescripcion(this TipoIVA tipoIVA)
        {
            return tipoIVA switch
            {
                TipoIVA.IVA_0 => "IVA 0%",
                TipoIVA.IVA_12 => "IVA 12%",
                TipoIVA.IVA_15 => "IVA 15%",
                _ => "IVA Desconocido"
            };
        }
    }
}