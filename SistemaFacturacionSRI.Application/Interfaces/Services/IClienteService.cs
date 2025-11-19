using SistemaFacturacionSRI.Application.DTOs.Cliente;
using SistemaFacturacionSRI.Application.DTOs.Common;

namespace SistemaFacturacionSRI.Application.Interfaces.Services
{
    /// <summary>
    /// Interfaz para el servicio de gestión de clientes.
    /// Define las operaciones CRUD para clientes del sistema de facturación.
    /// </summary>
    public interface IClienteService
    {
        /// <summary>
        /// Crea un nuevo cliente en el sistema.
        /// Valida que la identificación sea única y el formato sea correcto según tipo SRI.
        /// </summary>
        /// <param name="dto">Datos del cliente a crear</param>
        /// <returns>DTO con información del cliente creado</returns>
        /// <exception cref="InvalidOperationException">Si la identificación ya existe o formato inválido</exception>
        Task<ClienteDto> CrearClienteAsync(CrearClienteDto dto);

        /// <summary>
        /// Lista todos los clientes con paginación y filtros.
        /// </summary>
        /// <param name="filtro">Filtros de búsqueda y paginación</param>
        /// <returns>Resultado paginado con lista de clientes</returns>
        Task<PagedResultDto<ClienteListDto>> ListarClientesAsync(FiltroClienteDto filtro);

        /// <summary>
        /// Obtiene un cliente por su ID.
        /// </summary>
        /// <param name="clienteId">ID del cliente</param>
        /// <returns>DTO con información del cliente o null si no existe</returns>
        Task<ClienteDto?> ObtenerClientePorIdAsync(int clienteId);

        /// <summary>
        /// Actualiza la información de un cliente existente.
        /// </summary>
        /// <param name="dto">Datos actualizados del cliente</param>
        /// <returns>DTO con información actualizada</returns>
        /// <exception cref="KeyNotFoundException">Si el cliente no existe</exception>
        Task<ClienteDto> ActualizarClienteAsync(ActualizarClienteDto dto);

        /// <summary>
        /// Busca clientes por identificación.
        /// </summary>
        /// <param name="identificacion">Número de identificación a buscar</param>
        /// <returns>Cliente encontrado o null</returns>
        Task<ClienteDto?> BuscarPorIdentificacionAsync(string identificacion);

        /// <summary>
/// Busca clientes por nombre o identificación (sin paginación).
/// Útil para autocompletado y búsquedas rápidas.
/// </summary>
/// <param name="termino">Término de búsqueda</param>
/// <param name="limite">Número máximo de resultados (por defecto 10)</param>
/// <returns>Lista de clientes que coinciden con el término</returns>
Task<List<ClienteListDto>> BuscarClientesAsync(string termino, int limite = 10);

        
    }
}