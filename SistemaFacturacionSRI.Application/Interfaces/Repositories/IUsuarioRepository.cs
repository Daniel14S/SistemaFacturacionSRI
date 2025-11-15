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
        /// Obtiene un usuario por su identificador.
        /// </summary>
        Task<Usuario?> ObtenerPorIdAsync(int usuarioId);

        /// <summary>
        /// Actualiza los datos del usuario.
        /// </summary>
        Task ActualizarAsync(Usuario usuario);
    }
}
