using SistemaFacturacionSRI.Application.DTOs.Auth;
using SistemaFacturacionSRI.Application.DTOs.Usuario;
using SistemaFacturacionSRI.Application.DTOs.Common; 
using System.Net.Http.Json;

namespace SistemaFacturacionSRI.WebUI.Services
{
    /// <summary>
    /// Servicio cliente HTTP para consumir la API de Usuarios.
    /// Este servicio se comunica con UsuarioController en el backend.
    /// </summary>
    public class UsuarioHttpService : IUsuarioHttpService
    {
        private readonly HttpClient _httpClient;
        private const string API_BASE_URL = "/api/usuarios";

        public UsuarioHttpService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
/// Obtiene todos los usuarios del sistema.
/// GET /api/usuarios
/// </summary>
public async Task<List<UsuarioDto>> ObtenerTodosAsync()
{
    try
    {
        // El backend devuelve PagedResultDto, no una lista directa
        var response = await _httpClient.GetFromJsonAsync<PagedResultDto<UsuarioListDto>>(API_BASE_URL);
        
        if (response == null || response.Items == null)
        {
            return new List<UsuarioDto>();
        }

        // Convertir UsuarioListDto a UsuarioDto
        var usuarios = response.Items.Select(u => new UsuarioDto
        {
            Id = u.UsuarioId,
            Username = u.Username,
            Email = u.Email,
            Rol = u.Rol,
            Estado = u.Estado,
            FechaCreacion = u.FechaCreacion,
            UltimoAcceso = u.UltimoAcceso,
            NombreCompleto = u.NombreCompleto
        }).ToList();
        
        return usuarios;
    }
    catch (HttpRequestException ex)
    {
        throw new Exception($"Error al obtener usuarios: {ex.Message}", ex);
    }
}


        /// <summary>
        /// Obtiene un usuario por su ID.
        /// GET /api/usuarios/{id}
        /// </summary>
        public async Task<UsuarioDto?> ObtenerPorIdAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<UsuarioDto>($"{API_BASE_URL}/{id}");
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al obtener el usuario: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Crea un nuevo usuario en el sistema.
        /// POST /api/usuarios
        /// </summary>
        public async Task<UsuarioDto> CrearAsync(CrearUsuarioDto dto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(API_BASE_URL, dto);
                
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error al crear usuario: {error}");
                }

                var usuarioCreado = await response.Content.ReadFromJsonAsync<UsuarioDto>();
                return usuarioCreado ?? throw new Exception("No se recibió respuesta del servidor");
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error de conexión al crear usuario: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Actualiza un usuario existente.
        /// PUT /api/usuarios/{id}
        /// </summary>
        public async Task<UsuarioDto> ActualizarAsync(ActualizarUsuarioDto dto)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"{API_BASE_URL}/{dto.UsuarioId}", dto);
                
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error al actualizar usuario: {error}");
                }

                var usuarioActualizado = await response.Content.ReadFromJsonAsync<UsuarioDto>();
                return usuarioActualizado ?? throw new Exception("No se recibió respuesta del servidor");
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error de conexión al actualizar usuario: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Desactiva un usuario (soft delete).
        /// DELETE /api/usuarios/{id}
        /// </summary>
        public async Task DesactivarAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{API_BASE_URL}/{id}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error al desactivar usuario: {error}");
                }
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error de conexión al desactivar usuario: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Activa un usuario previamente desactivado.
        /// PUT /api/usuarios/{id}/activar
        /// </summary>
        public async Task ActivarAsync(int id)
        {
            try
            {
                var response = await _httpClient.PutAsync($"{API_BASE_URL}/{id}/activar", null);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error al activar usuario: {error}");
                }
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error de conexión al activar usuario: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Cambia el rol de un usuario (solo Admin).
        /// PUT /api/usuarios/{id}/rol
        /// </summary>
        public async Task CambiarRolAsync(int id, int nuevoRolId)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"{API_BASE_URL}/{id}/rol", new { RolId = nuevoRolId });

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error al cambiar rol: {error}");
                }
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error de conexión al cambiar rol: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Busca usuarios por nombre de usuario o email.
        /// </summary>
        public async Task<List<UsuarioDto>> BuscarAsync(string termino)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(termino))
                {
                    return await ObtenerTodosAsync();
                }

                var usuarios = await ObtenerTodosAsync();
                return usuarios
                    .Where(u => 
                        u.Username.Contains(termino, StringComparison.OrdinalIgnoreCase) ||
                        u.Email.Contains(termino, StringComparison.OrdinalIgnoreCase) ||
                        (u.NombreCompleto != null && u.NombreCompleto.Contains(termino, StringComparison.OrdinalIgnoreCase))
                    )
                    .ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al buscar usuarios: {ex.Message}", ex);
            }
        }

        // UsuarioHttpService.cs
public async Task CambiarPasswordAsync(CambiarPasswordDto dto)
{
    var response = await _httpClient.PutAsJsonAsync($"api/usuarios/{dto.UsuarioId}/cambiar-password", dto);
    
    if (!response.IsSuccessStatusCode)
    {
        var error = await response.Content.ReadAsStringAsync();
        throw new Exception($"Error al cambiar contraseña: {error}");
    }
}

    }
}