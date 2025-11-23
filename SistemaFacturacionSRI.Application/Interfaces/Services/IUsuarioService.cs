using SistemaFacturacionSRI.Application.DTOs.Auth;
using SistemaFacturacionSRI.Application.DTOs.Common;
using SistemaFacturacionSRI.Application.DTOs.Usuario;

namespace SistemaFacturacionSRI.Application.Interfaces.Services
{
    /// <summary>
    /// Interfaz para el servicio de gestión de usuarios.
    /// Define las operaciones CRUD y administración de usuarios del sistema.
    /// </summary>
    public interface IUsuarioService
    {
        // ========== OPERACIONES CRUD ==========

        /// <summary>
        /// Crea un nuevo usuario en el sistema.
        /// Hashea la contraseña automáticamente y valida que el username sea único.
        /// </summary>
        /// <param name="dto">Datos del usuario a crear</param>
        /// <returns>DTO con información del usuario creado (sin password)</returns>
        /// <exception cref="InvalidOperationException">Si el username o email ya existe</exception>
        Task<UsuarioDto> CrearUsuarioAsync(CrearUsuarioDto dto);

        /// <summary>
        /// Lista todos los usuarios del sistema con paginación y filtros.
        /// </summary>
        /// <param name="filtro">Filtros de búsqueda y paginación</param>
        /// <returns>Resultado paginado con lista de usuarios</returns>
        Task<PagedResultDto<UsuarioListDto>> ListarUsuariosAsync(FiltroUsuarioDto filtro);

        /// <summary>
        /// Obtiene un usuario por su ID con toda su información.
        /// </summary>
        /// <param name="usuarioId">ID del usuario</param>
        /// <returns>DTO con información del usuario o null si no existe</returns>
        Task<UsuarioDto?> ObtenerUsuarioPorIdAsync(int usuarioId);

        /// <summary>
        /// Obtiene un usuario por su username.
        /// Útil para verificar disponibilidad de username.
        /// </summary>
        /// <param name="username">Nombre de usuario</param>
        /// <returns>DTO con información del usuario o null si no existe</returns>
        Task<UsuarioDto?> ObtenerUsuarioPorUsernameAsync(string username);

        /// <summary>
        /// Actualiza la información de un usuario existente.
        /// No actualiza la contraseña (usar CambiarPasswordAsync).
        /// </summary>
        /// <param name="dto">Datos actualizados del usuario</param>
        /// <returns>DTO con información actualizada</returns>
        /// <exception cref="KeyNotFoundException">Si el usuario no existe</exception>
        /// <exception cref="InvalidOperationException">Si el nuevo username/email ya existe</exception>
        Task<UsuarioDto> ActualizarUsuarioAsync(ActualizarUsuarioDto dto);

        // ========== GESTIÓN DE ESTADO ==========

        /// <summary>
        /// Desactiva un usuario (soft delete).
        /// El usuario no podrá iniciar sesión pero sus datos se conservan.
        /// </summary>
        /// <param name="usuarioId">ID del usuario a desactivar</param>
        /// <returns>True si se desactivó correctamente</returns>
        /// <exception cref="KeyNotFoundException">Si el usuario no existe</exception>
        /// <exception cref="InvalidOperationException">Si es el último administrador</exception>
        Task<bool> DesactivarUsuarioAsync(int usuarioId);

        /// <summary>
        /// Activa un usuario previamente desactivado.
        /// También resetea los intentos de login fallidos.
        /// </summary>
        /// <param name="usuarioId">ID del usuario a activar</param>
        /// <returns>True si se activó correctamente</returns>
        /// <exception cref="KeyNotFoundException">Si el usuario no existe</exception>
        Task<bool> ActivarUsuarioAsync(int usuarioId);

        /// <summary>
        /// Desbloquea un usuario que fue bloqueado por intentos fallidos.
        /// Resetea el contador de intentos y activa la cuenta.
        /// </summary>
        /// <param name="usuarioId">ID del usuario a desbloquear</param>
        /// <returns>True si se desbloqueó correctamente</returns>
        /// <exception cref="KeyNotFoundException">Si el usuario no existe</exception>
        Task<bool> DesbloquearUsuarioAsync(int usuarioId);

        // ========== GESTIÓN DE ROLES ==========

        /// <summary>
        /// Cambia el rol de un usuario.
        /// Solo puede ser ejecutado por administradores.
        /// </summary>
        /// <param name="dto">Datos del cambio de rol</param>
        /// <returns>True si se cambió correctamente</returns>
        /// <exception cref="KeyNotFoundException">Si el usuario no existe</exception>
        /// <exception cref="InvalidOperationException">Si es el último administrador y se intenta degradar</exception>
        Task<bool> CambiarRolAsync(CambiarRolDto dto);

        // ========== GESTIÓN DE CONTRASEÑAS ==========

        /// <summary>
        /// Cambia la contraseña de un usuario.
        /// Requiere la contraseña actual para mayor seguridad.
        /// </summary>
        /// <param name="dto">Datos del cambio de contraseña</param>
        /// <returns>True si se cambió correctamente</returns>
        /// <exception cref="KeyNotFoundException">Si el usuario no existe</exception>
        /// <exception cref="InvalidOperationException">Si la contraseña actual es incorrecta</exception>
        Task<bool> CambiarPasswordAsync(CambiarPasswordDto dto);

        /// <summary>
        /// Resetea la contraseña de un usuario (solo administrador).
        /// Genera una contraseña temporal que el usuario debe cambiar al iniciar sesión.
        /// </summary>
        /// <param name="usuarioId">ID del usuario</param>
        /// <returns>Contraseña temporal generada</returns>
        /// <exception cref="KeyNotFoundException">Si el usuario no existe</exception>
        Task<string> ResetearPasswordAsync(int usuarioId);

        // ========== VALIDACIONES Y UTILIDADES ==========

        /// <summary>
        /// Verifica si un username está disponible (no está en uso).
        /// </summary>
        /// <param name="username">Username a verificar</param>
        /// <param name="usuarioIdExcluir">ID del usuario a excluir de la búsqueda (para actualización)</param>
        /// <returns>True si está disponible</returns>
        Task<bool> UsernameDisponibleAsync(string username, int? usuarioIdExcluir = null);

        /// <summary>
        /// Verifica si un email está disponible (no está en uso).
        /// </summary>
        /// <param name="email">Email a verificar</param>
        /// <param name="usuarioIdExcluir">ID del usuario a excluir de la búsqueda (para actualización)</param>
        /// <returns>True si está disponible</returns>
        Task<bool> EmailDisponibleAsync(string email, int? usuarioIdExcluir = null);

        /// <summary>
        /// Obtiene la cantidad total de usuarios activos por rol.
        /// </summary>
        /// <returns>Diccionario con conteo por rol</returns>
        Task<Dictionary<string, int>> ObtenerEstadisticasUsuariosAsync();

        /// <summary>
        /// Verifica si un usuario es el último administrador del sistema.
        /// Útil para prevenir que se elimine o degrade el último admin.
        /// </summary>
        /// <param name="usuarioId">ID del usuario a verificar</param>
        /// <returns>True si es el último administrador</returns>
        Task<bool> EsUltimoAdministradorAsync(int usuarioId);

        /// <summary>
/// Verifica si una cédula ya existe en el sistema.
/// </summary>
/// <param name="cedula">Cédula a verificar</param>
/// <param name="usuarioIdExcluir">ID del usuario a excluir de la búsqueda (para actualización)</param>
/// <returns>True si la cédula ya existe</returns>
//Task<bool> ExisteCedulaAsync(string cedula, int? usuarioIdExcluir = null);


        
    }
}