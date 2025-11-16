using SistemaFacturacionSRI.Application.DTOs.Auth;
using SistemaFacturacionSRI.Application.DTOs.Common;
using SistemaFacturacionSRI.Application.DTOs.Usuario;
using SistemaFacturacionSRI.Application.Interfaces.Repositories;
using SistemaFacturacionSRI.Application.Interfaces.Security;
using SistemaFacturacionSRI.Application.Interfaces.Services;
using SistemaFacturacionSRI.Domain.Entities;
using SistemaFacturacionSRI.Domain.Enums;

namespace SistemaFacturacionSRI.Application.Services
{
    /// <summary>
    /// Implementación del servicio de gestión de usuarios.
    /// Maneja todas las operaciones CRUD y administración de usuarios.
    /// </summary>
    public class UsuarioService : IUsuarioService
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IPasswordHasher _passwordHasher;

        public UsuarioService(
            IUsuarioRepository usuarioRepository,
            IPasswordHasher passwordHasher)
        {
            _usuarioRepository = usuarioRepository;
            _passwordHasher = passwordHasher;
        }

        // ========== CREAR USUARIO ==========

        /// <inheritdoc />
        public async Task<UsuarioDto> CrearUsuarioAsync(CrearUsuarioDto dto)
        {
            // 1. VALIDACIONES PREVIAS
            await ValidarDatosUnicos(dto.Username, dto.Email);

            // 2. VALIDAR QUE EL ROL EXISTA
            ValidarRol(dto.RolId);

            // 3. HASHEAR LA CONTRASEÑA
            var passwordHash = _passwordHasher.HashPassword(dto.Password);

            // 4. CREAR LA ENTIDAD USUARIO
            var usuario = new Usuario
            {
                Username = dto.Username.Trim(),
                Email = dto.Email.Trim().ToLower(),
                PasswordHash = passwordHash,
                
                // Datos personales
                Nombre1 = dto.Nombre1.Trim(),
                Nombre2 = dto.Nombre2?.Trim(),
                Apellido1 = dto.Apellido1.Trim(),
                Apellido2 = dto.Apellido2?.Trim(),
                
                // Configuración de cuenta
                RolId = dto.RolId,
                Estado = dto.Estado,
                IntentosLogin = 0,
                FechaCreacion = DateTime.UtcNow,
                UltimoAcceso = null
            };

            // 5. GUARDAR EN BASE DE DATOS
            await _usuarioRepository.CrearAsync(usuario);

            // 6. RECARGAR USUARIO CON ROL (para obtener el nombre del rol)
            var usuarioCreado = await _usuarioRepository.ObtenerPorIdAsync(usuario.UsuarioId);

            if (usuarioCreado == null)
            {
                throw new InvalidOperationException("Error al crear el usuario");
            }

            // 7. MAPEAR A DTO Y RETORNAR
            return MapearUsuarioDto(usuarioCreado);
        }

        // ========== MÉTODOS AUXILIARES PRIVADOS ==========

        /// <summary>
        /// Valida que el username y email sean únicos en el sistema.
        /// </summary>
        private async Task ValidarDatosUnicos(string username, string email, int? usuarioIdExcluir = null)
        {
            // Verificar username
            var usuarioConUsername = await _usuarioRepository.ObtenerPorUsernameAsync(username);
            if (usuarioConUsername != null && usuarioConUsername.UsuarioId != usuarioIdExcluir)
            {
                throw new InvalidOperationException($"El usuario '{username}' ya está en uso");
            }

            // Verificar email
            var usuarioConEmail = await _usuarioRepository.ObtenerPorEmailAsync(email);
            if (usuarioConEmail != null && usuarioConEmail.UsuarioId != usuarioIdExcluir)
            {
                throw new InvalidOperationException($"El email '{email}' ya está en uso");
            }
        }

        /// <summary>
        /// Valida que el rol exista y sea válido.
        /// </summary>
        private void ValidarRol(int rolId)
        {
            if (!Enum.IsDefined(typeof(SistemaFacturacionSRI.Domain.Enums.Rol), rolId))
            {
                throw new InvalidOperationException($"El rol con ID {rolId} no es válido. Use Administrador (1) o Vendedor (2)");
            }
        }

        /// <summary>
        /// Mapea una entidad Usuario a UsuarioDto (sin contraseña).
        /// </summary>
        private UsuarioDto MapearUsuarioDto(Usuario usuario)
        {
            return new UsuarioDto
            {
                Id = usuario.UsuarioId,
                Username = usuario.Username,
                Email = usuario.Email,
                Rol = usuario.Rol?.NombreRol ?? "Sin Rol",
                Estado = usuario.Estado,
                FechaCreacion = usuario.FechaCreacion,
                UltimoAcceso = usuario.UltimoAcceso,
                NombreCompleto = ConstruirNombreCompleto(usuario)
            };
        }

        /// <summary>
        /// Mapea una entidad Usuario a UsuarioListDto (versión simplificada para listas).
        /// </summary>
        private UsuarioListDto MapearUsuarioListDto(Usuario usuario)
        {
            return new UsuarioListDto
            {
                UsuarioId = usuario.UsuarioId,
                Username = usuario.Username,
                Email = usuario.Email,
                NombreCompleto = ConstruirNombreCompleto(usuario) ?? "Sin nombre",
                Rol = usuario.Rol?.NombreRol ?? "Sin Rol",
                RolId = usuario.RolId,
                Estado = usuario.Estado,
                FechaCreacion = usuario.FechaCreacion,
                UltimoAcceso = usuario.UltimoAcceso,
                EstaBloqueado = usuario.IntentosLogin >= 5,
                IntentosLogin = usuario.IntentosLogin
            };
        }

        /// <summary>
        /// Construye el nombre completo concatenando nombres y apellidos.
        /// </summary>
        private string? ConstruirNombreCompleto(Usuario usuario)
        {
            var nombres = new List<string?>
            {
                usuario.Nombre1,
                usuario.Nombre2,
                usuario.Apellido1,
                usuario.Apellido2
            };

            var nombreCompleto = string.Join(" ", nombres.Where(n => !string.IsNullOrWhiteSpace(n)));
            return string.IsNullOrWhiteSpace(nombreCompleto) ? null : nombreCompleto;
        }

        // ========== MÉTODOS PENDIENTES (implementaremos después) ==========

        /// <inheritdoc />
        public async Task<PagedResultDto<UsuarioListDto>> ListarUsuariosAsync(FiltroUsuarioDto filtro)
        {
            // 1. VALIDAR PARÁMETROS DE PAGINACIÓN
            if (filtro.PageNumber < 1)
                filtro.PageNumber = 1;
            
            if (filtro.PageSize < 1)
                filtro.PageSize = 10;

            // 2. LLAMAR AL REPOSITORIO CON FILTROS
            var (usuarios, totalRegistros) = await _usuarioRepository.ListarConFiltrosAsync(
                filtro.Busqueda,
                filtro.RolId,
                filtro.Estado,
                filtro.SoloBloqueados,
                filtro.PageNumber,
                filtro.PageSize,
                filtro.OrderBy,
                filtro.OrderAscending
            );

            // 3. MAPEAR A DTOs
            var usuariosDto = usuarios.Select(u => MapearUsuarioListDto(u)).ToList();

            // 4. CREAR RESULTADO PAGINADO
            return new PagedResultDto<UsuarioListDto>
            {
                Items = usuariosDto,
                TotalItems = totalRegistros,
                PageNumber = filtro.PageNumber,
                PageSize = filtro.PageSize
            };
        }

        public Task<UsuarioDto?> ObtenerUsuarioPorIdAsync(int usuarioId)
        {
            throw new NotImplementedException();
        }

        public Task<UsuarioDto?> ObtenerUsuarioPorUsernameAsync(string username)
        {
            throw new NotImplementedException();
        }

        public Task<UsuarioDto> ActualizarUsuarioAsync(ActualizarUsuarioDto dto)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DesactivarUsuarioAsync(int usuarioId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ActivarUsuarioAsync(int usuarioId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DesbloquearUsuarioAsync(int usuarioId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> CambiarRolAsync(CambiarRolDto dto)
        {
            throw new NotImplementedException();
        }

        public Task<bool> CambiarPasswordAsync(CambiarPasswordDto dto)
        {
            throw new NotImplementedException();
        }

        public Task<string> ResetearPasswordAsync(int usuarioId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UsernameDisponibleAsync(string username, int? usuarioIdExcluir = null)
        {
            throw new NotImplementedException();
        }

        public Task<bool> EmailDisponibleAsync(string email, int? usuarioIdExcluir = null)
        {
            throw new NotImplementedException();
        }

        public Task<Dictionary<string, int>> ObtenerEstadisticasUsuariosAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> EsUltimoAdministradorAsync(int usuarioId)
        {
            throw new NotImplementedException();
        }
    }
}