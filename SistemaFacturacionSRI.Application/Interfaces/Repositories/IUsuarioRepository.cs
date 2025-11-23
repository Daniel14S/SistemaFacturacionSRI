using SistemaFacturacionSRI.Domain.Entities;

namespace SistemaFacturacionSRI.Application.Interfaces.Repositories
{
    /// <summary>
    /// Operaciones específicas para la entidad Usuario.
    /// </summary>
    public interface IUsuarioRepository
    {
        /// <summary>
        /// Obtiene un usuario por su nombre de usuario.
        /// </summary>
        Task<Usuario?> ObtenerPorUsernameAsync(string username);

        /// <summary>
        /// Obtiene un usuario por su email.
        /// </summary>
        Task<Usuario?> ObtenerPorEmailAsync(string email);

        /// <summary>
        /// Obtiene un usuario por su identificador.
        /// </summary>
        Task<Usuario?> ObtenerPorIdAsync(int usuarioId);

        /// <summary>
        /// Crea un nuevo usuario en la base de datos.
        /// </summary>
        Task CrearAsync(Usuario usuario);  // ← ASEGÚRATE DE QUE ESTÉ ESTE

        /// <summary>
        /// Actualiza los datos del usuario.
        /// </summary>
        Task ActualizarAsync(Usuario usuario);

        /// <summary>
        /// Lista usuarios con filtros y paginación.
        /// </summary>
        Task<(List<Usuario> Usuarios, int TotalRegistros)> ListarConFiltrosAsync(
            string? busqueda,
            int? rolId,
            bool? estado,
            bool? soloBloqueados,
            int pageNumber,
            int pageSize,
            string? orderBy,
            bool orderAscending);
        }

        

        
}