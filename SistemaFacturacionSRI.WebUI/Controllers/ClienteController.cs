using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaFacturacionSRI.Application.DTOs.Cliente;
using SistemaFacturacionSRI.Application.Interfaces.Services;
using SistemaFacturacionSRI.WebUI.Authorization;

namespace SistemaFacturacionSRI.WebUI.Controllers
{
    /// <summary>
    /// Controlador REST para gestión de clientes.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ClienteController : ControllerBase
    {
        private readonly IClienteService _clienteService;
        private readonly ILogger<ClienteController> _logger;

        public ClienteController(
            IClienteService clienteService,
            ILogger<ClienteController> logger)
        {
            _clienteService = clienteService;
            _logger = logger;
        }

        /// <summary>
        /// Lista todos los clientes con paginación y filtros.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<ActionResult> ListarClientes([FromQuery] FiltroClienteDto filtro)
        {
            try
            {
                var resultado = await _clienteService.ListarClientesAsync(filtro);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar clientes");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Error interno al listar clientes"
                });
            }
        }

        /// <summary>
        /// Obtiene un cliente por su ID.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ClienteDto>> ObtenerClientePorId(int id)
        {
            try
            {
                var cliente = await _clienteService.ObtenerClientePorIdAsync(id);
                
                if (cliente == null)
                {
                    return NotFound(new { message = $"Cliente con ID {id} no encontrado" });
                }

                return Ok(cliente);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener cliente {ClienteId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Error interno al obtener cliente"
                });
            }
        }

    /// <summary>
    /// Crea un nuevo cliente.
    /// </summary>
    [HttpPost]
    [AdminAuthorize]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ClienteDto>> CrearCliente([FromBody] CrearClienteDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest(new { message = "Los datos del cliente son requeridos" });
                }

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

                var clienteCreado = await _clienteService.CrearClienteAsync(dto);

                return CreatedAtAction(
                    nameof(ObtenerClientePorId),
                    new { id = clienteCreado.ClienteId },
                    clienteCreado
                );
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear cliente");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Error interno al crear cliente"
                });
            }
        }

                /// <summary>
                /// Actualiza un cliente existente.
                /// </summary>
                [HttpPut("{id}")]
                [AdminAuthorize]
                [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status200OK)]
                [ProducesResponseType(StatusCodes.Status400BadRequest)]
                [ProducesResponseType(StatusCodes.Status404NotFound)]
                public async Task<ActionResult<ClienteDto>> ActualizarCliente(int id, [FromBody] ActualizarClienteDto dto)
                {
                    try
                    {
                        if (dto == null)
                        {
                            return BadRequest(new { message = "Los datos del cliente son requeridos" });
                        }

                        if (dto.ClienteId == 0)
                        {
                            dto.ClienteId = id;
                        }
                        else if (dto.ClienteId != id)
                        {
                            return BadRequest(new { message = "El ID de la ruta no coincide con el ID del cliente" });
                        }

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

                        var clienteActualizado = await _clienteService.ActualizarClienteAsync(dto);

                        return Ok(clienteActualizado);
                    }
                    catch (KeyNotFoundException ex)
                    {
                        return NotFound(new { message = ex.Message });
                    }
                    catch (InvalidOperationException ex)
                    {
                        return BadRequest(new { message = ex.Message });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al actualizar cliente {ClienteId}", id);
                        return StatusCode(StatusCodes.Status500InternalServerError, new
                        {
                            message = "Error interno al actualizar cliente"
                        });
                    }
                }

                /// <summary>
                /// Cambia el estado (activo/inactivo) de un cliente.
                /// </summary>
                [HttpPatch("{id}/estado")]
                [AdminAuthorize]
                [ProducesResponseType(StatusCodes.Status204NoContent)]
                [ProducesResponseType(StatusCodes.Status400BadRequest)]
                [ProducesResponseType(StatusCodes.Status404NotFound)]
                public async Task<IActionResult> CambiarEstado(int id, [FromBody] CambiarEstadoClienteDto dto)
                {
                    try
                    {
                        if (dto == null)
                        {
                            return BadRequest(new { message = "Los datos del cambio de estado son requeridos" });
                        }

                        if (dto.ClienteId == 0)
                        {
                            dto.ClienteId = id;
                        }
                        else if (dto.ClienteId != id)
                        {
                            return BadRequest(new { message = "El ID del cuerpo no coincide con el de la ruta" });
                        }

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

                        await _clienteService.CambiarEstadoClienteAsync(dto);
                        return NoContent();
                    }
                    catch (KeyNotFoundException ex)
                    {
                        return NotFound(new { message = ex.Message });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al cambiar estado del cliente {ClienteId}", id);
                        return StatusCode(StatusCodes.Status500InternalServerError, new
                        {
                            message = "Error interno al cambiar estado del cliente"
                        });
                    }
                }


        /// <summary>
        /// Busca un cliente por su identificación.
        /// </summary>
        [HttpGet("buscar/{identificacion}")]
        [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ClienteDto>> BuscarPorIdentificacion(string identificacion)
        {
            try
            {
                var cliente = await _clienteService.BuscarPorIdentificacionAsync(identificacion);
                
                if (cliente == null)
                {
                    return NotFound(new { message = $"Cliente con identificación {identificacion} no encontrado" });
                }

                return Ok(cliente);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar cliente por identificación");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Error interno al buscar cliente"
                });
            }
        }

        /// <summary>
/// Busca clientes por nombre o identificación (búsqueda rápida).
/// </summary>
[HttpGet("buscar")]
[ProducesResponseType(typeof(List<ClienteListDto>), StatusCodes.Status200OK)]
public async Task<ActionResult<List<ClienteListDto>>> BuscarClientes(
    [FromQuery] string termino,
    [FromQuery] int limite = 10)
{
    try
    {
        if (string.IsNullOrWhiteSpace(termino))
        {
            return Ok(new List<ClienteListDto>());
        }

        var clientes = await _clienteService.BuscarClientesAsync(termino, limite);
        return Ok(clientes);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error al buscar clientes con término: {Termino}", termino);
        return StatusCode(StatusCodes.Status500InternalServerError, new
        {
            message = "Error interno al buscar clientes"
        });
    }
}



    }
}