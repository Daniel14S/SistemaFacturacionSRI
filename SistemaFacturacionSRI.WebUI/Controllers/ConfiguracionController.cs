using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaFacturacionSRI.Domain.DTOs.Configuracion;
using SistemaFacturacionSRI.Domain.Interfaces.Services;
using SistemaFacturacionSRI.WebUI.Authorization;

namespace SistemaFacturacionSRI.WebUI.Controllers
{
    /// <summary>
    /// Controlador REST para gestión de configuración empresarial.
    /// Solo Administradores pueden gestionar la configuración.
    /// T-025: SPRINT 3 - DÍA 1
    /// </summary>
    [Authorize(Roles = "Administrador")] // Solo Admin
    [ApiController]
    [Route("api/[controller]")]
    public class ConfiguracionController : ControllerBase
    {
        private readonly IConfiguracionService _configuracionService;
        private readonly ILogger<ConfiguracionController> _logger;

        public ConfiguracionController(
            IConfiguracionService configuracionService,
            ILogger<ConfiguracionController> logger)
        {
            _configuracionService = configuracionService;
            _logger = logger;
        }

        /// <summary>
        /// GET /api/configuracion
        /// Obtiene la configuración actual de la empresa
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ConfiguracionEmpresaDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ConfiguracionEmpresaDto>> ObtenerConfiguracion()
        {
            try
            {
                var configuracion = await _configuracionService.ObtenerConfiguracionAsync();
                return Ok(configuracion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener configuración empresarial");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Error interno al obtener configuración"
                });
            }
        }

        /// <summary>
        /// PUT /api/configuracion
        /// Actualiza la configuración de la empresa
        /// </summary>
        [HttpPut]
        [ProducesResponseType(typeof(ConfiguracionEmpresaDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ConfiguracionEmpresaDto>> ActualizarConfiguracion(
            [FromBody] ActualizarConfiguracionDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        message = "Datos inválidos",
                        errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                    });
                }

                var configuracionActualizada = await _configuracionService.ActualizarConfiguracionAsync(dto);
                
                _logger.LogInformation("Configuración empresarial actualizada exitosamente");
                
                return Ok(configuracionActualizada);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar configuración empresarial");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Error interno al actualizar configuración"
                });
            }
        }

        /// <summary>
        /// GET /api/configuracion/certificado/validar
        /// Valida que el certificado digital sea válido y no haya expirado
        /// </summary>
        [HttpGet("certificado/validar")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> ValidarCertificado()
        {
            try
            {
                var esValido = await _configuracionService.ValidarCertificadoDigitalAsync();
                
                return Ok(new
                {
                    certificadoValido = esValido,
                    mensaje = esValido 
                        ? "Certificado digital válido" 
                        : "Certificado digital inválido o expirado"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar certificado digital");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Error interno al validar certificado"
                });
            }
        }

        /// <summary>
        /// GET /api/configuracion/certificado/info
        /// Obtiene información del certificado digital actual
        /// </summary>
        [HttpGet("certificado/info")]
        [ProducesResponseType(typeof(InfoCertificadoDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<InfoCertificadoDto>> ObtenerInfoCertificado()
        {
            try
            {
                var info = await _configuracionService.ObtenerInfoCertificadoAsync();
                
                if (info == null)
                {
                    return NotFound(new { message = "No se ha configurado un certificado digital" });
                }

                return Ok(info);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener información del certificado");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Error interno al obtener información del certificado"
                });
            }
        }

        /// <summary>
        /// PATCH /api/configuracion/ambiente
        /// Cambia el ambiente del SRI (Pruebas/Producción)
        /// </summary>
        [HttpPatch("ambiente")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> CambiarAmbienteSRI([FromBody] CambiarAmbienteDto dto)
        {
            try
            {
                if (string.IsNullOrEmpty(dto.NuevoAmbiente))
                {
                    return BadRequest(new { message = "El ambiente es requerido (1 = Pruebas, 2 = Producción)" });
                }

                if (dto.NuevoAmbiente != "1" && dto.NuevoAmbiente != "2")
                {
                    return BadRequest(new { message = "Ambiente inválido. Use: 1 = Pruebas, 2 = Producción" });
                }

                var resultado = await _configuracionService.CambiarAmbienteSRIAsync(dto.NuevoAmbiente);
                
                if (!resultado)
                {
                    return BadRequest(new { message = "No se pudo cambiar el ambiente" });
                }

                _logger.LogWarning("Ambiente SRI cambiado a: {Ambiente}", 
                    dto.NuevoAmbiente == "1" ? "PRUEBAS" : "PRODUCCIÓN");

                return Ok(new
                {
                    mensaje = $"Ambiente cambiado exitosamente a {(dto.NuevoAmbiente == "1" ? "PRUEBAS" : "PRODUCCIÓN")}",
                    nuevoAmbiente = dto.NuevoAmbiente
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar ambiente SRI");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Error interno al cambiar ambiente"
                });
            }
        }

        /// <summary>
        /// GET /api/configuracion/validar-completa
        /// Verifica si la configuración está completa para emitir facturas
        /// </summary>
        [HttpGet("validar-completa")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> ValidarConfiguracionCompleta()
        {
            try
            {
                var (estaCompleta, camposFaltantes) = await _configuracionService.ValidarConfiguracionCompletaAsync();
                
                return Ok(new
                {
                    configuracionCompleta = estaCompleta,
                    camposFaltantes = camposFaltantes,
                    mensaje = estaCompleta 
                        ? "Configuración lista para emitir facturas" 
                        : "Faltan campos por configurar"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar configuración completa");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Error interno al validar configuración"
                });
            }
        }
    }

    // DTO auxiliar para cambiar ambiente
    public class CambiarAmbienteDto
    {
        public string NuevoAmbiente { get; set; } = string.Empty;
    }
}