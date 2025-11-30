using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaFacturacionSRI.Domain.DTOs.Auth;
using SistemaFacturacionSRI.Domain.Interfaces;

namespace SistemaFacturacionSRI.WebUI.Controllers
{
    /// <summary>
    /// Controlador para la autenticación de usuarios
    /// Maneja login, logout y validación de tokens
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// POST: api/auth/login
        /// Autentica un usuario y genera un token JWT
        /// </summary>
        /// <param name="request">Credenciales del usuario (username y password)</param>
        /// <returns>Token JWT y datos del usuario si las credenciales son válidas</returns>
        /// <response code="200">Login exitoso, devuelve token y datos del usuario</response>
        /// <response code="400">Datos de entrada inválidos</response>
        /// <response code="401">Credenciales incorrectas o usuario bloqueado</response>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
        {
            try
            {
                // Validar que el request no sea nulo
                if (request == null)
                {
                    _logger.LogWarning("Intento de login con request nulo");
                    return BadRequest(new LoginResponseDto
                    {
                        Success = false,
                        Message = "Los datos de inicio de sesión son requeridos"
                    });
                }

                // Validar el ModelState (validaciones de DataAnnotations)
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Intento de login con datos inválidos: {Username}", request.Username);
                    return BadRequest(new LoginResponseDto
                    {
                        Success = false,
                        Message = "Los datos proporcionados no son válidos"
                    });
                }

                _logger.LogInformation("Intento de login para el usuario: {Username}", request.Username);

                // Llamar al servicio de autenticación
                var response = await _authService.LoginAsync(request);

                // Si el login no fue exitoso, devolver 401 Unauthorized
                if (!response.Success)
                {
                    _logger.LogWarning("Login fallido para el usuario: {Username}. Razón: {Message}", 
                        request.Username, response.Message);
                    return Unauthorized(response);
                }

                // Login exitoso
                _logger.LogInformation("Login exitoso para el usuario: {Username}", request.Username);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado durante el login del usuario: {Username}", request.Username);
                return StatusCode(StatusCodes.Status500InternalServerError, new LoginResponseDto
                {
                    Success = false,
                    Message = "Ocurrió un error interno al procesar la solicitud. Por favor, intente nuevamente."
                });
            }
        }

        /// <summary>
        /// POST: api/auth/logout
        /// Cierra la sesión del usuario actual
        /// </summary>
        /// <returns>Confirmación de logout exitoso</returns>
        /// <response code="200">Logout exitoso</response>
        /// <response code="401">Usuario no autenticado</response>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> Logout()
        {
            try
            {
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("Intento de logout sin ID de usuario válido");
                    return Unauthorized(new { message = "No se pudo identificar al usuario" });
                }

                _logger.LogInformation("Usuario {UserId} cerrando sesión", userId);

                var resultado = await _authService.LogoutAsync(userId);

                if (!resultado)
                {
                    _logger.LogWarning("No se pudo cerrar sesión para el usuario {UserId}", userId);
                    return BadRequest(new { message = "No se pudo cerrar la sesión" });
                }

                _logger.LogInformation("Logout exitoso para el usuario {UserId}", userId);
                return Ok(new { message = "Sesión cerrada exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado durante el logout");
                return StatusCode(StatusCodes.Status500InternalServerError, new 
                { 
                    message = "Ocurrió un error al cerrar la sesión" 
                });
            }
        }

        /// <summary>
        /// GET: api/auth/validate
        /// Valida si el token JWT actual es válido
        /// </summary>
        /// <returns>Confirmación de validez del token</returns>
        /// <response code="200">Token válido</response>
        /// <response code="401">Token inválido o expirado</response>
        [HttpGet("validate")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public ActionResult ValidateToken()
        {
            try
            {
                // Si llegamos aquí, el token es válido (pasó la autenticación)
                var username = User.Identity?.Name;
                var role = User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;

                return Ok(new 
                { 
                    valid = true, 
                    username = username,
                    role = role,
                    message = "Token válido" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar el token");
                return Unauthorized(new { valid = false, message = "Token inválido" });
            }
        }

        /// <summary>
        /// GET: api/auth/me
        /// Obtiene la información del usuario autenticado actual
        /// </summary>
        /// <returns>Datos del usuario autenticado</returns>
        /// <response code="200">Datos del usuario</response>
        /// <response code="401">No autenticado</response>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public ActionResult GetCurrentUser()
        {
            try
            {
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
                var username = User.Identity?.Name;
                var email = User.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
                var role = User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;

                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized(new { message = "Usuario no identificado" });
                }

                return Ok(new
                {
                    id = int.Parse(userIdClaim),
                    username = username,
                    email = email,
                    role = role
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener información del usuario actual");
                return StatusCode(StatusCodes.Status500InternalServerError, new 
                { 
                    message = "Error al obtener información del usuario" 
                });
            }
        }

        
        
    }

    
}