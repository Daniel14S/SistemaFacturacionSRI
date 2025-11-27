namespace SistemaFacturacionSRI.Application.Interfaces.Services
{
    /// <summary>
    /// Expone operaciones para la generación segura de secuencias de facturación.
    /// </summary>
    public interface ISecuenciaService
    {
        /// <summary>
        /// Obtiene el siguiente número de factura para un establecimiento/punto de emisión.
        /// El formato retornado sigue el patrón 001-001-000000001.
        /// </summary>
        Task<string> GenerarSiguienteNumeroFacturaAsync(string establecimiento, string puntoEmision, CancellationToken cancellationToken = default);

        /// <summary>
        /// Devuelve el valor numérico actual de la secuencia sin incrementarla.
        /// </summary>
        Task<long> ObtenerSecuenciaActualAsync(string establecimiento, string puntoEmision, CancellationToken cancellationToken = default);
    }
}
