using SistemaFacturacionSRI.Domain.DTOs.Common;
using SistemaFacturacionSRI.Domain.DTOs.Factura;
using SistemaFacturacionSRI.Domain.Interfaces.Repositories;
using SistemaFacturacionSRI.Domain.Interfaces.Services;
using SistemaFacturacionSRI.Domain.Entities;
using SistemaFacturacionSRI.Domain.Enums;
using AutoMapper;

namespace SistemaFacturacionSRI.Application.Services
{
    public class FacturaService : IFacturaService
    {
        private readonly IFacturaRepository _facturaRepository;
        private readonly IClienteRepository _clienteRepository;
        private readonly IProductoRepository _productoRepository;
        private readonly ISecuenciaService _secuenciaService;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IMapper _mapper;

        public FacturaService(
            IFacturaRepository facturaRepository,
            IClienteRepository clienteRepository,
            IProductoRepository productoRepository,
            ISecuenciaService secuenciaService,
            IUsuarioRepository usuarioRepository,
            IMapper mapper)
        {
            _facturaRepository = facturaRepository;
            _clienteRepository = clienteRepository;
            _productoRepository = productoRepository;
            _secuenciaService = secuenciaService;
            _usuarioRepository = usuarioRepository;
            _mapper = mapper;
        }

        /// <summary>
        /// T-20: Crea una nueva factura con validaciones de negocio
        /// </summary>
        public async Task<FacturaDto> CrearFacturaAsync(CrearFacturaDto dto, int usuarioId, CancellationToken cancellationToken = default)
        {
            // T-21: Validar cliente existe y está activo
            var cliente = await _clienteRepository.ObtenerPorIdAsync(dto.ClienteId);
            if (cliente == null)
            {
                throw new InvalidOperationException($"El cliente con ID {dto.ClienteId} no existe");
            }
            if (!cliente.Estado) // ⚠️ CORRECCIÓN: Cliente usa Estado, no Activo
            {
                throw new InvalidOperationException($"El cliente {cliente.NombreCompleto()} está inactivo");
            }

            // T-21: Validar que hay al menos un detalle
            if (dto.Detalles == null || !dto.Detalles.Any())
            {
                throw new InvalidOperationException("La factura debe tener al menos un detalle");
            }

            // T-21: Validar productos
            var productosIds = dto.Detalles.Select(d => d.ProductoId).Distinct().ToList();
            var productos = await _productoRepository.ObtenerPorIdsAsync(productosIds);
            
            if (productos.Count != productosIds.Count)
            {
                throw new InvalidOperationException("Uno o más productos no existen");
            }

            // T-21: Validar productos activos y con stock
            foreach (var detalle in dto.Detalles)
            {
                var producto = productos.First(p => p.Id == detalle.ProductoId);

                if (!producto.Activo) // Producto sí tiene Activo
                {
                    throw new InvalidOperationException($"El producto {producto.Nombre} está inactivo");
                }

                if (detalle.Cantidad <= 0)
                {
                    throw new InvalidOperationException($"La cantidad del producto {producto.Nombre} debe ser mayor a cero");
                }

                // ⚠️ CORRECCIÓN: Producto usa StockDisponible (propiedad calculada)
                if (producto.StockDisponible < (int)detalle.Cantidad)
                {
                    throw new InvalidOperationException(
                        $"Stock insuficiente para {producto.Nombre}. Disponible: {producto.StockDisponible}, Solicitado: {detalle.Cantidad}");
                }

                if (detalle.PrecioUnitario <= 0)
                {
                    throw new InvalidOperationException($"El precio del producto {producto.Nombre} debe ser mayor a cero");
                }

                // T-21: Validar tarifas IVA válidas (ahora es int)
                if (!Enum.IsDefined(typeof(TipoIVA), detalle.CodigoPorcentajeIVA))
                {
                    throw new InvalidOperationException($"Tarifa IVA inválida para producto {producto.Nombre}");
                }
            }

            // ⚠️ CORRECCIÓN: Obtener secuencia con Async
            var numeroFactura = await _secuenciaService.ObtenerSiguienteNumeroAsync();

            // Crear entidad Factura
            var factura = new Factura
            {
                NumeroFactura = numeroFactura,
                ClienteId = dto.ClienteId,
                UsuarioId = usuarioId,
                FechaEmision = DateTime.Now,
                Ambiente = Ambiente.PRUEBAS,
                TipoEmision = TipoEmision.NORMAL,
                Estado = EstadoFactura.BORRADOR,
                Observaciones = dto.Observaciones
            };

            // Crear detalles y calcular totales
            var detalles = new List<DetalleFactura>();
            var subtotales = new Dictionary<TipoIVA, decimal>
            {
                { TipoIVA.IVA_0, 0 },
                { TipoIVA.IVA_12, 0 },
                { TipoIVA.IVA_15, 0 }
            };

            decimal descuentoTotal = 0;
            decimal ivaTotal = 0;

            foreach (var detalleDto in dto.Detalles)
            {
                var producto = productos.First(p => p.Id == detalleDto.ProductoId);
                
                // Calcular valores del detalle
                decimal precioTotalSinImpuesto = detalleDto.Cantidad * detalleDto.PrecioUnitario;
                decimal descuentoLinea = detalleDto.Descuento;
                decimal baseImponible = precioTotalSinImpuesto - descuentoLinea;
                
                // ⚠️ CORRECCIÓN: CodigoPorcentajeIVA ahora es int, no necesita cast inicial
                var tipoIVA = (TipoIVA)detalleDto.CodigoPorcentajeIVA;
                decimal tarifa = ObtenerTarifa(tipoIVA);
                decimal valorIVA = baseImponible * tarifa;
                decimal valorTotal = baseImponible + valorIVA;

                var detalle = new DetalleFactura
                {
                    ProductoId = producto.Id,
                    CodigoPrincipal = producto.Codigo,
                    Descripcion = producto.Nombre,
                    Cantidad = detalleDto.Cantidad,
                    PrecioUnitario = detalleDto.PrecioUnitario,
                    Descuento = descuentoLinea,
                    PrecioTotalSinImpuesto = precioTotalSinImpuesto,
                    CodigoPorcentajeIVA = detalleDto.CodigoPorcentajeIVA,
                    Tarifa = tarifa,
                    BaseImponible = baseImponible,
                    Valor = valorIVA,
                    ValorTotal = valorTotal
                };

                detalles.Add(detalle);

                // Acumular en subtotales por tarifa
                subtotales[tipoIVA] += baseImponible;
                descuentoTotal += descuentoLinea;
                ivaTotal += valorIVA;
            }

            // Asignar subtotales
            factura.Subtotal0 = subtotales[TipoIVA.IVA_0];
            factura.Subtotal12 = subtotales[TipoIVA.IVA_12];
            factura.Subtotal15 = subtotales[TipoIVA.IVA_15];
            factura.SubtotalNoObjetoIVA = 0;
            factura.SubtotalExentoIVA = 0;
            factura.SubtotalConDescuento = subtotales.Values.Sum();
            factura.Descuento = descuentoTotal;
            factura.IVA12 = subtotales[TipoIVA.IVA_12] * 0.12m;
            factura.IVA15 = subtotales[TipoIVA.IVA_15] * 0.15m;
            factura.ImporteTotal = factura.SubtotalConDescuento + ivaTotal + factura.Propina;

            // Asignar detalles
            factura.Detalles = detalles;

            // Guardar en BD
            await _facturaRepository.CrearAsync(factura);

            // Retornar DTO
            return _mapper.Map<FacturaDto>(factura);
        }

        /// <summary>
        /// Lista facturas con filtros y paginación
        /// </summary>
        public async Task<PagedResultDto<Factura>> ListarFacturasAsync(FiltroFacturaDto filtro, CancellationToken cancellationToken = default)
        {
            var resultado = await _facturaRepository.ListarConFiltrosAsync(
                clienteId: filtro.ClienteId,
                usuarioId: filtro.UsuarioId,
                estado: filtro.Estado,
                fechaDesde: filtro.FechaDesde,
                fechaHasta: filtro.FechaHasta,
                numeroFactura: filtro.NumeroFactura,
                pagina: filtro.PageNumber,
                tamanoPagina: filtro.PageSize
            );
            
            var facturas = resultado.facturas;
            var total = resultado.total;
            
            return new PagedResultDto<Factura>
            {
                Items = facturas,
                TotalItems = total, 
                PageNumber = filtro.PageNumber, 
                PageSize = filtro.PageSize 
            };
        }

        /// <summary>
        /// Obtiene una factura por ID con detalles completos
        /// </summary>
        public async Task<Factura?> ObtenerPorIdAsync(int facturaId, CancellationToken cancellationToken = default)
        {
            return await _facturaRepository.ObtenerConDetallesCompletosAsync(facturaId);
        }

        /// <summary>
        /// Actualiza el estado de una factura
        /// </summary>
        public async Task<Factura> ActualizarEstadoAsync(int facturaId, EstadoFactura nuevoEstado, CancellationToken cancellationToken = default)
        {
            var factura = await _facturaRepository.ObtenerPorIdAsync(facturaId);
            if (factura == null)
            {
                throw new KeyNotFoundException($"Factura con ID {facturaId} no encontrada");
            }

            // Validar transición de estado permitida
            ValidarTransicionEstado(factura.Estado, nuevoEstado);

            factura.Estado = nuevoEstado;
            factura.FechaModificacion = DateTime.Now;
            
            if (nuevoEstado == EstadoFactura.AUTORIZADA)
            {
                factura.FechaAutorizacion = DateTime.Now;
            }

            await _facturaRepository.ActualizarAsync(factura);
            
            return factura;
        }

        /// <summary>
        /// Anula una factura (solo Admin)
        /// </summary>
        public async Task<Factura> AnularFacturaAsync(int facturaId, int usuarioId, string? motivo = null, CancellationToken cancellationToken = default)
        {
            var factura = await _facturaRepository.ObtenerPorIdAsync(facturaId);
            if (factura == null)
            {
                throw new KeyNotFoundException($"Factura con ID {facturaId} no encontrada");
            }

            // Validar que el usuario existe y es administrador
            var usuario = await _usuarioRepository.ObtenerPorIdAsync(usuarioId);
            if (usuario == null)
            {
                throw new KeyNotFoundException($"Usuario con ID {usuarioId} no encontrado");
            }

            // Verificar que el usuario tenga rol asignado
            if (usuario.RolId != (int)SistemaFacturacionSRI.Domain.Enums.Rol.Administrador)
            {
                throw new UnauthorizedAccessException("Solo los administradores pueden anular facturas");
            }




            // Solo se pueden anular facturas autorizadas
            if (factura.Estado != EstadoFactura.AUTORIZADA)
            {
                throw new InvalidOperationException("Solo se pueden anular facturas autorizadas");
            }

            factura.Estado = EstadoFactura.ANULADA;
            factura.FechaModificacion = DateTime.Now;
            
            // Agregar motivo a las observaciones
            if (!string.IsNullOrEmpty(motivo))
            {
                factura.Observaciones = string.IsNullOrEmpty(factura.Observaciones) 
                    ? $"ANULADA: {motivo}" 
                    : $"{factura.Observaciones}\nANULADA: {motivo}";
            }
            
            await _facturaRepository.ActualizarAsync(factura);
            
            return factura;
        }

        // ==================== MÉTODOS AUXILIARES ====================

        private decimal ObtenerTarifa(TipoIVA tipo)
        {
            return tipo switch
            {
                TipoIVA.IVA_0 => 0m,
                TipoIVA.IVA_12 => 0.12m,
                TipoIVA.IVA_15 => 0.15m,
                _ => throw new ArgumentException($"Tipo IVA no válido: {tipo}")
            };
        }

        private void ValidarTransicionEstado(EstadoFactura estadoActual, EstadoFactura nuevoEstado)
        {
            var transicionesValidas = new Dictionary<EstadoFactura, List<EstadoFactura>>
            {
                { EstadoFactura.BORRADOR, new() { EstadoFactura.GENERADA, EstadoFactura.ANULADA } },
                { EstadoFactura.GENERADA, new() { EstadoFactura.FIRMADA, EstadoFactura.ANULADA } },
                { EstadoFactura.FIRMADA, new() { EstadoFactura.ENVIADA, EstadoFactura.ANULADA } },
                { EstadoFactura.ENVIADA, new() { EstadoFactura.RECIBIDA, EstadoFactura.DEVUELTA, EstadoFactura.ANULADA } },
                { EstadoFactura.RECIBIDA, new() { EstadoFactura.AUTORIZADA, EstadoFactura.NO_AUTORIZADA } },
                { EstadoFactura.DEVUELTA, new() { EstadoFactura.ENVIADA, EstadoFactura.ANULADA } },
                { EstadoFactura.NO_AUTORIZADA, new() { EstadoFactura.ENVIADA, EstadoFactura.ANULADA } },
                { EstadoFactura.AUTORIZADA, new() { EstadoFactura.ANULADA } },
                { EstadoFactura.ANULADA, new() }
            };

            if (!transicionesValidas.ContainsKey(estadoActual))
            {
                throw new InvalidOperationException($"Estado actual desconocido: {estadoActual}");
            }

            if (!transicionesValidas[estadoActual].Contains(nuevoEstado))
            {
                throw new InvalidOperationException(
                    $"Transición de estado no permitida: {estadoActual} → {nuevoEstado}");
            }
        }
    }
}