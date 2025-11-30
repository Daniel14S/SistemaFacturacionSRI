using SistemaFacturacionSRI.Domain.DTOs.Auth;
using SistemaFacturacionSRI.Domain.DTOs.Usuario;

namespace SistemaFacturacionSRI.WebUI.Services
{
    /// <summary>
    /// Interfaz para el servicio HTTP de Usuarios.
    /// Define todos los métodos para consumir la API de usuarios.
    /// </summary>
    public interface IUsuarioHttpService
    {
        /// <summary>
        /// Obtiene todos los usuarios del sistema.
        /// GET /api/usuarios
        /// </summary>
        Task<List<UsuarioDto>> ObtenerTodosAsync();

        /// <summary>
        /// Obtiene un usuario por su ID.
        /// GET /api/usuarios/{id}
        /// </summary>
        Task<UsuarioDto?> ObtenerPorIdAsync(int id);

        /// <summary>
        /// Crea un nuevo usuario en el sistema.
        /// POST /api/usuarios
        /// </summary>
        Task<UsuarioDto> CrearAsync(CrearUsuarioDto dto);

        /// <summary>
        /// Actualiza un usuario existente.
        /// PUT /api/usuarios/{id}
        /// </summary>
        Task<UsuarioDto> ActualizarAsync(ActualizarUsuarioDto dto);

        /// <summary>
        /// Desactiva un usuario (soft delete).
        /// DELETE /api/usuarios/{id}
        /// </summary>
        Task DesactivarAsync(int id);

        /// <summary>
        /// Activa un usuario previamente desactivado.
        /// PUT /api/usuarios/{id}/activar
        /// </summary>
        Task ActivarAsync(int id);

        /// <summary>
        /// Cambia el rol de un usuario (solo Admin).
        /// PUT /api/usuarios/{id}/rol
        /// </summary>
        Task CambiarRolAsync(int id, int nuevoRolId);

        /// <summary>
        /// Busca usuarios por nombre de usuario o email.
        /// </summary>
        Task<List<UsuarioDto>> BuscarAsync(string termino);

        /// <summary>
/// Cambia la contraseña de un usuario.
/// PUT /api/usuarios/{id}/cambiar-password
/// </summary>
Task CambiarPasswordAsync(CambiarPasswordDto dto);

/// <summary>
/// Verifica si una cédula ya existe en el sistema.
/// GET /api/usuarios/existe-cedula?cedula=xxx
/// </summary>
//Task<bool> ExisteCedulaAsync(string cedula, int? excluirUsuarioId = null);

    }
}