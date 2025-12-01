using SistemaFacturacionSRI.Domain.Entities;

namespace SistemaFacturacionSRI.Domain.Interfaces.Repositories
{
    /// <summary>
    /// Repositorio para la entidad ConfiguracionEmpresa
    /// Nota: Solo debe existir UN registro de configuración
    /// </summary>
    public interface IConfiguracionEmpresaRepository
    {
        /// <summary>
        /// Obtiene la configuración de la empresa (registro único)
        /// </summary>
        Task<ConfiguracionEmpresa?> ObtenerConfiguracionAsync();

        /// <summary>
        /// Obtiene la configuración por ID
        /// </summary>
        Task<ConfiguracionEmpresa?> ObtenerPorIdAsync(int id);

        /// <summary>
        /// Crea la configuración inicial
        /// </summary>
        Task<ConfiguracionEmpresa> CrearAsync(ConfiguracionEmpresa configuracion);

        /// <summary>
        /// Actualiza la configuración existente
        /// </summary>
        Task ActualizarAsync(ConfiguracionEmpresa configuracion);

        /// <summary>
        /// Verifica si ya existe una configuración
        /// </summary>
        Task<bool> ExisteConfiguracionAsync();
    }
}