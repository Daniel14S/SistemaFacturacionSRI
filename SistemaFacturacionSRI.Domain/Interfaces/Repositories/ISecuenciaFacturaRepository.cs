using SistemaFacturacionSRI.Domain.Entities;

namespace SistemaFacturacionSRI.Domain.Interfaces.Repositories
{
    /// <summary>
    /// Repositorio para la entidad SecuenciaFactura
    /// </summary>
    public interface ISecuenciaFacturaRepository
    {
        /// <summary>
        /// Obtiene la secuencia activa para un establecimiento y punto de emisión
        /// </summary>
        Task<SecuenciaFactura?> ObtenerActivaAsync(string establecimiento, string puntoEmision);

        /// <summary>
        /// Obtiene una secuencia por su ID
        /// </summary>
        Task<SecuenciaFactura?> ObtenerPorIdAsync(int id);

        /// <summary>
        /// Crea una nueva secuencia
        /// </summary>
        Task<SecuenciaFactura> CrearAsync(SecuenciaFactura secuencia);

        /// <summary>
        /// Actualiza una secuencia existente
        /// </summary>
        Task ActualizarAsync(SecuenciaFactura secuencia);

        /// <summary>
        /// Incrementa la secuencia de forma atómica (thread-safe)
        /// </summary>
        Task<int> IncrementarSecuenciaAsync(int secuenciaId);

        /// <summary>
        /// Obtiene el siguiente número de secuencia sin incrementar
        /// </summary>
        Task<int> ObtenerSiguienteNumeroAsync(string establecimiento, string puntoEmision);

        /// <summary>
        /// Lista todas las secuencias
        /// </summary>
        Task<List<SecuenciaFactura>> ListarAsync();

        /// <summary>
        /// Activa o desactiva una secuencia
        /// </summary>
        Task CambiarEstadoAsync(int id, bool activo);
    }
}