using SistemaFacturacionSRI.Domain.Entities;

namespace SistemaFacturacionSRI.Application.Interfaces.Repositories
{
    /// <summary>
    /// Operaciones específicas para la entidad Cliente.
    /// </summary>
    public interface IClienteRepository
    {
        /// <summary>
        /// Crea un nuevo cliente en la base de datos.
        /// </summary>
        Task CrearAsync(Cliente cliente);

        /// <summary>
        /// Obtiene un cliente por su ID incluyendo TipoIdentificacion.
        /// </summary>
        Task<Cliente?> ObtenerPorIdAsync(int clienteId);

        /// <summary>
        /// Obtiene un cliente por su identificación.
        /// </summary>
        Task<Cliente?> ObtenerPorIdentificacionAsync(string identificacion);

        /// <summary>
        /// Actualiza los datos de un cliente.
        /// </summary>
        Task ActualizarAsync(Cliente cliente);

        /// <summary>
        /// Lista clientes con filtros y paginación.
        /// </summary>
        Task<(List<Cliente> Clientes, int TotalRegistros)> ListarConFiltrosAsync(
            string? busqueda,
            int? tipoIdentificacionId,
            int pageNumber,
            int pageSize,
            string? orderBy,
            bool orderAscending);
    }
}