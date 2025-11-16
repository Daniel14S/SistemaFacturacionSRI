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

        // ========== LISTAR USUARIOS ==========

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

        // ========== T-24: OBTENER USUARIO POR ID (1.5h) ==========

        /// <inheritdoc />
        public async Task<UsuarioDto?> ObtenerUsuarioPorIdAsync(int usuarioId)
        {
            // 1. VALIDAR QUE EL ID SEA VÁLIDO
            if (usuarioId <= 0)
            {
                throw new ArgumentException("El ID del usuario debe ser mayor a cero", nameof(usuarioId));
            }

            // 2. BUSCAR USUARIO EN LA BASE DE DATOS (con información del rol)
            var usuario = await _usuarioRepository.ObtenerPorIdAsync(usuarioId);

            // 3. SI NO EXISTE, RETORNAR NULL
            if (usuario == null)
            {
                return null;
            }

            // 4. MAPEAR A DTO Y RETORNAR
            return MapearUsuarioDto(usuario);
        }

        // ========== T-24 ADICIONAL: OBTENER POR USERNAME ==========

        /// <inheritdoc />
        public async Task<UsuarioDto?> ObtenerUsuarioPorUsernameAsync(string username)
        {
            // 1. VALIDAR QUE EL USERNAME NO ESTÉ VACÍO
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("El username no puede estar vacío", nameof(username));
            }

            // 2. BUSCAR USUARIO POR USERNAME
            var usuario = await _usuarioRepository.ObtenerPorUsernameAsync(username.Trim());

            // 3. SI NO EXISTE, RETORNAR NULL
            if (usuario == null)
            {
                return null;
            }

            // 4. MAPEAR A DTO Y RETORNAR
            return MapearUsuarioDto(usuario);
        }

        // ========== T-25: ACTUALIZAR USUARIO (2h) ==========

        /// <inheritdoc />
        public async Task<UsuarioDto> ActualizarUsuarioAsync(ActualizarUsuarioDto dto)
        {
            // 1. VALIDAR QUE EL DTO NO SEA NULO
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto), "Los datos de actualización no pueden ser nulos");
            }

            // 2. BUSCAR EL USUARIO EXISTENTE
            var usuario = await _usuarioRepository.ObtenerPorIdAsync(dto.UsuarioId);
            
            if (usuario == null)
            {
                throw new KeyNotFoundException($"No se encontró el usuario con ID {dto.UsuarioId}");
            }

            // 3. VALIDAR QUE USERNAME Y EMAIL SEAN ÚNICOS (excluyendo al usuario actual)
            await ValidarDatosUnicos(dto.Username, dto.Email, dto.UsuarioId);

            // 4. ACTUALIZAR LOS DATOS DEL USUARIO
            usuario.Username = dto.Username.Trim();
            usuario.Email = dto.Email.Trim().ToLower();
            usuario.Nombre1 = dto.Nombre1.Trim();
            usuario.Nombre2 = dto.Nombre2?.Trim();
            usuario.Apellido1 = dto.Apellido1.Trim();
            usuario.Apellido2 = dto.Apellido2?.Trim();

            // NOTA: NO actualizamos el rol aquí (eso es con CambiarRolAsync)
            // NOTA: NO actualizamos la contraseña aquí (eso es con CambiarPasswordAsync)

            // 5. GUARDAR CAMBIOS EN LA BASE DE DATOS
            await _usuarioRepository.ActualizarAsync(usuario);

            // 6. RECARGAR USUARIO CON ROL ACTUALIZADO
            var usuarioActualizado = await _usuarioRepository.ObtenerPorIdAsync(usuario.UsuarioId);

            if (usuarioActualizado == null)
            {
                throw new InvalidOperationException("Error al actualizar el usuario");
            }

            // 7. MAPEAR A DTO Y RETORNAR
            return MapearUsuarioDto(usuarioActualizado);
        }

        // ========== T-26: DESACTIVAR USUARIO - SOFT DELETE (1.5h) ==========

        /// <inheritdoc />
        public async Task<bool> DesactivarUsuarioAsync(int usuarioId)
        {
            // 1. VALIDAR QUE EL ID SEA VÁLIDO
            if (usuarioId <= 0)
            {
                throw new ArgumentException("El ID del usuario debe ser mayor a cero", nameof(usuarioId));
            }

            // 2. BUSCAR EL USUARIO
            var usuario = await _usuarioRepository.ObtenerPorIdAsync(usuarioId);
            
            if (usuario == null)
            {
                throw new KeyNotFoundException($"No se encontró el usuario con ID {usuarioId}");
            }

            // 3. VERIFICAR SI YA ESTÁ DESACTIVADO
            if (!usuario.Estado)
            {
                // Ya está desactivado, no hacer nada pero retornar true
                return true;
            }

            // 4. VALIDAR QUE NO SEA EL ÚLTIMO ADMINISTRADOR ACTIVO
            if (usuario.RolId == (int)SistemaFacturacionSRI.Domain.Enums.Rol.Administrador)
            {
                var esUltimoAdmin = await EsUltimoAdministradorAsync(usuarioId);
                
                if (esUltimoAdmin)
                {
                    throw new InvalidOperationException(
                        "No se puede desactivar el último administrador del sistema. " +
                        "Debe haber al menos un administrador activo."
                    );
                }
            }

            // 5. DESACTIVAR EL USUARIO (SOFT DELETE)
            usuario.Estado = false;
            
            // 6. GUARDAR CAMBIOS
            await _usuarioRepository.ActualizarAsync(usuario);

            return true;
        }

        // ========== T-27: ACTIVAR USUARIO (1h) ==========

        /// <inheritdoc />
        public async Task<bool> ActivarUsuarioAsync(int usuarioId)
        {
            // 1. VALIDAR QUE EL ID SEA VÁLIDO
            if (usuarioId <= 0)
            {
                throw new ArgumentException("El ID del usuario debe ser mayor a cero", nameof(usuarioId));
            }

            // 2. BUSCAR EL USUARIO
            var usuario = await _usuarioRepository.ObtenerPorIdAsync(usuarioId);
            
            if (usuario == null)
            {
                throw new KeyNotFoundException($"No se encontró el usuario con ID {usuarioId}");
            }

            // 3. VERIFICAR SI YA ESTÁ ACTIVO
            if (usuario.Estado)
            {
                // Ya está activo, no hacer nada pero retornar true
                return true;
            }

            // 4. ACTIVAR EL USUARIO
            usuario.Estado = true;
            
            // 5. RESETEAR INTENTOS DE LOGIN (desbloquearlo también)
            usuario.IntentosLogin = 0;

            // 6. GUARDAR CAMBIOS
            await _usuarioRepository.ActualizarAsync(usuario);

            return true;
        }

        // ========== MÉTODOS PENDIENTES (implementaremos después) ==========
// ========== DESBLOQUEAR USUARIO ==========

/// <inheritdoc />
public async Task<bool> DesbloquearUsuarioAsync(int usuarioId)
{
    // 1. VALIDAR QUE EL ID SEA VÁLIDO
    if (usuarioId <= 0)
    {
        throw new ArgumentException("El ID del usuario debe ser mayor a cero", nameof(usuarioId));
    }

    // 2. BUSCAR EL USUARIO
    var usuario = await _usuarioRepository.ObtenerPorIdAsync(usuarioId);
    
    if (usuario == null)
    {
        throw new KeyNotFoundException($"No se encontró el usuario con ID {usuarioId}");
    }

    // 3. RESETEAR INTENTOS DE LOGIN
    usuario.IntentosLogin = 0;
    
    // 4. SI ESTABA INACTIVO POR BLOQUEO, ACTIVARLO
    if (!usuario.Estado && usuario.IntentosLogin >= 5)
    {
        usuario.Estado = true;
    }

    // 5. GUARDAR CAMBIOS
    await _usuarioRepository.ActualizarAsync(usuario);

    return true;
}

// ========== CAMBIAR ROL ==========

/// <inheritdoc />
public async Task<bool> CambiarRolAsync(CambiarRolDto dto)
{
    // 1. VALIDAR QUE EL DTO NO SEA NULO
    if (dto == null)
    {
        throw new ArgumentNullException(nameof(dto), "Los datos del cambio de rol no pueden ser nulos");
    }

    // 2. VALIDAR QUE EL ROL SEA VÁLIDO
    ValidarRol(dto.NuevoRolId);

    // 3. BUSCAR EL USUARIO
    var usuario = await _usuarioRepository.ObtenerPorIdAsync(dto.UsuarioId);
    
    if (usuario == null)
    {
        throw new KeyNotFoundException($"No se encontró el usuario con ID {dto.UsuarioId}");
    }

    // 4. VALIDAR QUE NO SE ESTÉ DEGRADANDO AL ÚLTIMO ADMINISTRADOR
    if (usuario.RolId == (int)SistemaFacturacionSRI.Domain.Enums.Rol.Administrador 
        && dto.NuevoRolId != (int)SistemaFacturacionSRI.Domain.Enums.Rol.Administrador)
    {
        var esUltimoAdmin = await EsUltimoAdministradorAsync(dto.UsuarioId);
        
        if (esUltimoAdmin)
        {
            throw new InvalidOperationException(
                "No se puede cambiar el rol del último administrador del sistema. " +
                "Debe haber al menos un administrador activo."
            );
        }
    }

    // 5. CAMBIAR EL ROL
    usuario.RolId = dto.NuevoRolId;

    // 6. GUARDAR CAMBIOS
    await _usuarioRepository.ActualizarAsync(usuario);

    return true;
}

// ========== CAMBIAR PASSWORD ==========

/// <inheritdoc />
public async Task<bool> CambiarPasswordAsync(CambiarPasswordDto dto)
{
    // 1. VALIDAR QUE EL DTO NO SEA NULO
    if (dto == null)
    {
        throw new ArgumentNullException(nameof(dto), "Los datos del cambio de contraseña no pueden ser nulos");
    }

    // 2. BUSCAR EL USUARIO
    var usuario = await _usuarioRepository.ObtenerPorIdAsync(dto.UsuarioId);
    
    if (usuario == null)
    {
        throw new KeyNotFoundException($"No se encontró el usuario con ID {dto.UsuarioId}");
    }

    // 3. VERIFICAR QUE LA CONTRASEÑA ACTUAL SEA CORRECTA
    var passwordActualValido = _passwordHasher.VerifyPassword(dto.PasswordActual, usuario.PasswordHash);
    
    if (!passwordActualValido)
    {
        throw new InvalidOperationException("La contraseña actual es incorrecta");
    }

    // 4. VALIDAR QUE LA NUEVA CONTRASEÑA SEA DIFERENTE
    if (dto.PasswordActual == dto.NuevaPassword)
    {
        throw new InvalidOperationException("La nueva contraseña debe ser diferente a la actual");
    }

    // 5. HASHEAR LA NUEVA CONTRASEÑA
    var nuevoPasswordHash = _passwordHasher.HashPassword(dto.NuevaPassword);

    // 6. ACTUALIZAR LA CONTRASEÑA
    usuario.PasswordHash = nuevoPasswordHash;

    // 7. RESETEAR INTENTOS DE LOGIN (por seguridad)
    usuario.IntentosLogin = 0;

    // 8. GUARDAR CAMBIOS
    await _usuarioRepository.ActualizarAsync(usuario);

    return true;
}

// ========== RESETEAR PASSWORD (ADMIN) ==========

/// <inheritdoc />
public async Task<string> ResetearPasswordAsync(int usuarioId)
{
    // 1. VALIDAR QUE EL ID SEA VÁLIDO
    if (usuarioId <= 0)
    {
        throw new ArgumentException("El ID del usuario debe ser mayor a cero", nameof(usuarioId));
    }

    // 2. BUSCAR EL USUARIO
    var usuario = await _usuarioRepository.ObtenerPorIdAsync(usuarioId);
    
    if (usuario == null)
    {
        throw new KeyNotFoundException($"No se encontró el usuario con ID {usuarioId}");
    }

    // 3. GENERAR CONTRASEÑA TEMPORAL (8 caracteres alfanuméricos)
    var passwordTemporal = GenerarPasswordTemporal();

    // 4. HASHEAR LA CONTRASEÑA TEMPORAL
    var passwordHash = _passwordHasher.HashPassword(passwordTemporal);

    // 5. ACTUALIZAR LA CONTRASEÑA
    usuario.PasswordHash = passwordHash;
    
    // 6. RESETEAR INTENTOS DE LOGIN
    usuario.IntentosLogin = 0;
    
    // 7. ACTIVAR LA CUENTA SI ESTABA BLOQUEADA
    if (!usuario.Estado)
    {
        usuario.Estado = true;
    }

    // 8. GUARDAR CAMBIOS
    await _usuarioRepository.ActualizarAsync(usuario);

    // 9. RETORNAR LA CONTRASEÑA TEMPORAL (el admin debe comunicársela al usuario)
    return passwordTemporal;
}

// ========== USERNAME DISPONIBLE ==========

/// <inheritdoc />
public async Task<bool> UsernameDisponibleAsync(string username, int? usuarioIdExcluir = null)
{
    // 1. VALIDAR QUE EL USERNAME NO ESTÉ VACÍO
    if (string.IsNullOrWhiteSpace(username))
    {
        return false;
    }

    // 2. BUSCAR SI EXISTE UN USUARIO CON ESE USERNAME
    var usuarioExistente = await _usuarioRepository.ObtenerPorUsernameAsync(username.Trim());

    // 3. SI NO EXISTE, ESTÁ DISPONIBLE
    if (usuarioExistente == null)
    {
        return true;
    }

    // 4. SI EXISTE PERO ES EL USUARIO QUE ESTAMOS EXCLUYENDO, ESTÁ DISPONIBLE
    if (usuarioIdExcluir.HasValue && usuarioExistente.UsuarioId == usuarioIdExcluir.Value)
    {
        return true;
    }

    // 5. SI EXISTE Y NO ES EL USUARIO EXCLUIDO, NO ESTÁ DISPONIBLE
    return false;
}

// ========== EMAIL DISPONIBLE ==========

/// <inheritdoc />
public async Task<bool> EmailDisponibleAsync(string email, int? usuarioIdExcluir = null)
{
    // 1. VALIDAR QUE EL EMAIL NO ESTÉ VACÍO
    if (string.IsNullOrWhiteSpace(email))
    {
        return false;
    }

    // 2. BUSCAR SI EXISTE UN USUARIO CON ESE EMAIL
    var usuarioExistente = await _usuarioRepository.ObtenerPorEmailAsync(email.Trim().ToLower());

    // 3. SI NO EXISTE, ESTÁ DISPONIBLE
    if (usuarioExistente == null)
    {
        return true;
    }

    // 4. SI EXISTE PERO ES EL USUARIO QUE ESTAMOS EXCLUYENDO, ESTÁ DISPONIBLE
    if (usuarioIdExcluir.HasValue && usuarioExistente.UsuarioId == usuarioIdExcluir.Value)
    {
        return true;
    }

    // 5. SI EXISTE Y NO ES EL USUARIO EXCLUIDO, NO ESTÁ DISPONIBLE
    return false;
}

// ========== OBTENER ESTADÍSTICAS ==========

/// <inheritdoc />
public async Task<Dictionary<string, int>> ObtenerEstadisticasUsuariosAsync()
{
    var estadisticas = new Dictionary<string, int>();

    // 1. OBTENER TODOS LOS USUARIOS (sin paginación)
    var filtroTodos = new FiltroUsuarioDto
    {
        PageNumber = 1,
        PageSize = 1000 // Suficiente para obtener todos
    };

    var resultado = await ListarUsuariosAsync(filtroTodos);
    var usuarios = resultado.Items;

    // 2. CALCULAR ESTADÍSTICAS
    estadisticas["TotalUsuarios"] = usuarios.Count;
    estadisticas["UsuariosActivos"] = usuarios.Count(u => u.Estado);
    estadisticas["UsuariosInactivos"] = usuarios.Count(u => !u.Estado);
    estadisticas["UsuariosBloqueados"] = usuarios.Count(u => u.EstaBloqueado);
    estadisticas["Administradores"] = usuarios.Count(u => u.RolId == (int)SistemaFacturacionSRI.Domain.Enums.Rol.Administrador);
    estadisticas["Vendedores"] = usuarios.Count(u => u.RolId == (int)SistemaFacturacionSRI.Domain.Enums.Rol.Vendedor);
    estadisticas["AdministradoresActivos"] = usuarios.Count(u => u.RolId == (int)SistemaFacturacionSRI.Domain.Enums.Rol.Administrador && u.Estado);
    estadisticas["VendedoresActivos"] = usuarios.Count(u => u.RolId == (int)SistemaFacturacionSRI.Domain.Enums.Rol.Vendedor && u.Estado);

    return estadisticas;
}

// ========== MÉTODO AUXILIAR: GENERAR PASSWORD TEMPORAL ==========

/// <summary>
/// Genera una contraseña temporal alfanumérica de 8 caracteres
/// </summary>
private string GenerarPasswordTemporal()
{
    const string caracteres = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
    var random = new Random();
    var password = new char[8];

    for (int i = 0; i < 8; i++)
    {
        password[i] = caracteres[random.Next(caracteres.Length)];
    }

    return new string(password);
}


        // ========== MÉTODO AUXILIAR: ES ÚLTIMO ADMINISTRADOR ==========

        /// <inheritdoc />
        public async Task<bool> EsUltimoAdministradorAsync(int usuarioId)
        {
            // Obtener el usuario actual
            var usuario = await _usuarioRepository.ObtenerPorIdAsync(usuarioId);
            
            if (usuario == null || usuario.RolId != (int)SistemaFacturacionSRI.Domain.Enums.Rol.Administrador)
            {
                // Si no existe o no es admin, definitivamente no es el último admin
                return false;
            }

            // Contar cuántos administradores ACTIVOS hay (excluyendo al usuario en cuestión)
            var filtro = new FiltroUsuarioDto
            {
                RolId = (int)SistemaFacturacionSRI.Domain.Enums.Rol.Administrador,
                Estado = true, // Solo activos
                PageNumber = 1,
                PageSize = 100 // Suficiente para contar
            };

            var resultado = await ListarUsuariosAsync(filtro);
            
            // Contar admins activos excluyendo el usuario actual
            var adminsActivosExcluyendoActual = resultado.Items
                .Count(u => u.UsuarioId != usuarioId && u.Estado);

            // Si hay 0 administradores activos además del actual, entonces es el último
            return adminsActivosExcluyendoActual == 0;
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
    }
}