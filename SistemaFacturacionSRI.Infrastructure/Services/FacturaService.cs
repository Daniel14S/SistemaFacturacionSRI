using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SistemaFacturacionSRI.Domain.DTOs.Common;
using SistemaFacturacionSRI.Domain.DTOs.Factura;
using SistemaFacturacionSRI.Domain.Entities;
using SistemaFacturacionSRI.Domain.Enums;
using SistemaFacturacionSRI.Domain.Interfaces.Repositories;
using SistemaFacturacionSRI.Domain.Interfaces.Services;
using SistemaFacturacionSRI.Infrastructure.Data;

namespace SistemaFacturacionSRI.Infrastructure.Services
{
    /// <summary>
    /// Servicio completo de facturas con creación, consultas y cambios de estado.
    /// </summary>
    public class FacturaService : IFacturaService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISecuenciaService _secuenciaService;
        private readonly IXmlGeneratorService? _xmlGeneratorService;
        private readonly IFirmaElectronicaService? _firmaElectronicaService;
        private readonly IFacturaRepository? _facturaRepository;
        private readonly ClaveAccesoGenerator _claveAccesoGenerator;
        private readonly ILogger<FacturaService> _logger;


        private static readonly IReadOnlyDictionary<EstadoFactura, EstadoFactura[]> _transicionesPermitidas =
            new Dictionary<EstadoFactura, EstadoFactura[]>
            {
                [EstadoFactura.BORRADOR] = new[] { EstadoFactura.GENERADA, EstadoFactura.FIRMADA, EstadoFactura.ANULADA },
                [EstadoFactura.GENERADA] = new[] { EstadoFactura.FIRMADA, EstadoFactura.ANULADA },
                [EstadoFactura.FIRMADA] = new[] { EstadoFactura.ENVIADA, EstadoFactura.ANULADA },
                [EstadoFactura.ENVIADA] = new[] { EstadoFactura.RECIBIDA, EstadoFactura.DEVUELTA, EstadoFactura.ANULADA },
                [EstadoFactura.RECIBIDA] = new[] { EstadoFactura.AUTORIZADA, EstadoFactura.NO_AUTORIZADA, EstadoFactura.DEVUELTA },
                [EstadoFactura.DEVUELTA] = new[] { EstadoFactura.ENVIADA, EstadoFactura.ANULADA },
                [EstadoFactura.NO_AUTORIZADA] = new[] { EstadoFactura.GENERADA, EstadoFactura.ANULADA },
                [EstadoFactura.AUTORIZADA] = Array.Empty<EstadoFactura>(),
                [EstadoFactura.ANULADA] = Array.Empty<EstadoFactura>()
            };
        private const string RolAdministrador = "Administrador";

        public FacturaService(ApplicationDbContext context, ISecuenciaService secuenciaService, ClaveAccesoGenerator claveAccesoGenerator)
        {
            _context = context;
            _secuenciaService = secuenciaService;
            _claveAccesoGenerator = claveAccesoGenerator;
        }

        /// <summary>
        /// Constructor completo con servicios de firma electrónica (T-064)
        /// </summary>
        public FacturaService(
            ApplicationDbContext context, 
            ISecuenciaService secuenciaService,
            IXmlGeneratorService xmlGeneratorService,
            IFirmaElectronicaService firmaElectronicaService,
            IFacturaRepository facturaRepository,
            ClaveAccesoGenerator claveAccesoGenerator,
            ILogger<FacturaService> logger)
        {
            _context = context;
            _secuenciaService = secuenciaService;
            _xmlGeneratorService = xmlGeneratorService;
            _firmaElectronicaService = firmaElectronicaService;
            _facturaRepository = facturaRepository;
            _claveAccesoGenerator = claveAccesoGenerator;
            _logger = logger;
        }

        // ==================== CREAR FACTURA ====================

        /// <summary>
        /// T-20: Crea una nueva factura con todas las validaciones de negocio
        /// </summary>
        /// 
public async Task<FacturaDto> CrearFacturaAsync(CrearFacturaDto dto, int usuarioId, CancellationToken cancellationToken = default)
{
    if (dto == null)
    {
        throw new ArgumentNullException(nameof(dto));
    }

    // 1. Validar cliente existe y está activo
    var cliente = await _context.Clientes
        .AsNoTracking()
        .FirstOrDefaultAsync(c => c.ClienteId == dto.ClienteId, cancellationToken);

    if (cliente == null)
    {
        throw new KeyNotFoundException($"El cliente con ID {dto.ClienteId} no existe");
    }

    if (!cliente.Estado)
    {
        throw new InvalidOperationException($"El cliente {cliente.NombreCompleto()} está inactivo");
    }

    // 2. Validar que hay al menos un detalle
    if (dto.Detalles == null || !dto.Detalles.Any())
    {
        throw new InvalidOperationException("La factura debe tener al menos un detalle");
    }

    // 3. Validar productos existen, están activos y tienen stock EN EL LOTE
    var productosIds = dto.Detalles.Select(d => d.ProductoId).Distinct().ToList();
    var productos = await _context.Productos
        .AsNoTracking()
        .Include(p => p.Lotes)  // ✅ CRÍTICO: Incluir lotes
        .Where(p => productosIds.Contains(p.Id))
        .ToListAsync(cancellationToken);

    if (productos.Count != productosIds.Count)
    {
        throw new KeyNotFoundException("Uno o más productos no existen");
    }

    foreach (var detalle in dto.Detalles)
    {
        var producto = productos.First(p => p.Id == detalle.ProductoId);

        if (!producto.Activo)
        {
            throw new InvalidOperationException($"El producto {producto.Nombre} está inactivo");
        }

        if (detalle.Cantidad <= 0)
        {
            throw new InvalidOperationException($"La cantidad del producto {producto.Nombre} debe ser mayor a cero");
        }

        if (detalle.PrecioUnitario <= 0)
        {
            throw new InvalidOperationException($"El precio del producto {producto.Nombre} debe ser mayor a cero");
        }

        // ✅ VALIDACIÓN CORRECTA: Stock por lote
        if (detalle.LoteId.HasValue)
        {
            var lote = producto.Lotes.FirstOrDefault(l => l.LoteId == detalle.LoteId.Value);

            if (lote == null)
            {
                throw new KeyNotFoundException(
                    $"El lote con ID {detalle.LoteId.Value} no existe para el producto {producto.Nombre}");
            }

            if (lote.CantidadDisponible < detalle.Cantidad)
            {
                throw new InvalidOperationException(
                    $"Stock insuficiente para {producto.Nombre}. " +
                    $"Disponible: {lote.CantidadDisponible}, Solicitado: {detalle.Cantidad}");
            }
        }
        else
        {
            // Si no se especifica lote, validar stock total
            var stockTotal = producto.Lotes.Sum(l => l.CantidadDisponible);
            
            if (stockTotal < detalle.Cantidad)
            {
                throw new InvalidOperationException(
                    $"Stock insuficiente para {producto.Nombre}. " +
                    $"Disponible: {stockTotal}, Solicitado: {detalle.Cantidad}");
            }
        }

        if (!Enum.IsDefined(typeof(TipoIVA), detalle.CodigoPorcentajeIVA))
        {
            throw new InvalidOperationException($"Tarifa IVA inválida para producto {producto.Nombre}");
        }
    } // ✅ CIERRA el foreach

    // 4. Obtener siguiente número de factura
    string numeroFactura;
    try
    {
        numeroFactura = await _secuenciaService.GenerarNumeroCompletoAsync();
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException($"Error al generar secuencia de factura: {ex.Message}", ex);
    }

        var configEmpresa = await _context.ConfiguracionEmpresa.FirstOrDefaultAsync(cancellationToken);
            if (configEmpresa == null)
            {
                throw new InvalidOperationException(
                    "No se ha configurado la empresa emisora. Configure los datos de la empresa antes de emitir facturas.");
            }

            // ✅ CRÍTICO: Validar RUC
            if (string.IsNullOrWhiteSpace(configEmpresa.RUC) || configEmpresa.RUC.Length != 13)
            {
                throw new InvalidOperationException(
                    $"El RUC de la empresa es inválido. Debe tener 13 dígitos. RUC actual: '{configEmpresa.RUC}' (longitud: {configEmpresa.RUC?.Length ?? 0})");
            }

            // ✅ Limpiar RUC (remover espacios o guiones)
            var rucLimpio = new string(configEmpresa.RUC.Where(char.IsDigit).ToArray());

            if (rucLimpio.Length != 13)
            {
                throw new InvalidOperationException(
                    $"El RUC después de limpieza tiene longitud incorrecta: '{rucLimpio}' (longitud: {rucLimpio.Length})");
            }

            // ✅ CRÍTICO: Extraer correctamente las partes del número de factura
            var partesNumero = numeroFactura.Split('-');
            if (partesNumero.Length != 3)
            {
                throw new InvalidOperationException(
                    $"El número de factura '{numeroFactura}' no tiene el formato esperado 'xxx-xxx-xxxxxxxxx'");
            }

            // ✅ Asegurar formato con padding correcto
            var establecimiento = partesNumero[0].Trim().PadLeft(3, '0');
            var puntoEmision = partesNumero[1].Trim().PadLeft(3, '0');
            var secuencial = partesNumero[2].Trim().PadLeft(9, '0');

            // ✅ CRÍTICO: Validar y usar tipo de emisión correcto
            var tipoEmision = string.IsNullOrWhiteSpace(configEmpresa.TipoEmision)
                ? "1"
                : configEmpresa.TipoEmision.Trim();

            // ✅ Validar que sea 1 o 2
            if (tipoEmision != "1" && tipoEmision != "2")
            {
                throw new InvalidOperationException(
                    $"El tipo de emisión debe ser '1' (Normal) o '2' (Indisponibilidad). Valor actual: '{tipoEmision}'");
            }

            // ✅ Validar ambiente SRI
            var ambiente = configEmpresa.AmbienteSRI?.Trim() ?? "1";

            if (ambiente != "1" && ambiente != "2")
            {
                throw new InvalidOperationException(
                    $"El ambiente SRI debe ser '1' (Pruebas) o '2' (Producción). Valor actual: '{ambiente}'");
            }

            // ✅ Generar Clave de Acceso con datos correctos
            _logger?.LogInformation("\n═══════════════════════════════════════════════════");
            _logger?.LogInformation("🔑 GENERANDO CLAVE DE ACCESO");
            _logger?.LogInformation("═══════════════════════════════════════════════════");
            _logger?.LogInformation("  Fecha:          {Fecha}", DateTime.Now.ToString("dd/MM/yyyy"));
            _logger?.LogInformation("  Tipo Comp:      {TipoComprobante}", "01");
            _logger?.LogInformation("  RUC:            {Ruc}", rucLimpio);
            _logger?.LogInformation("  Ambiente:       {Ambiente} ({Desc})", ambiente, ambiente == "1" ? "Pruebas" : "Producción");
            _logger?.LogInformation("  Tipo Emisión:   {TipoEmision} ({Desc})", tipoEmision, tipoEmision == "1" ? "Normal" : "Contingencia");
            _logger?.LogInformation("  Establecim:     {Establecimiento}", establecimiento);
            _logger?.LogInformation("  Punto Emis:     {PuntoEmision}", puntoEmision);
            _logger?.LogInformation("  Secuencial:     {Secuencial}", secuencial);
            _logger?.LogInformation("  Número Factura: {NumeroFactura}", numeroFactura);
            _logger?.LogInformation("═══════════════════════════════════════════════════");

            // ✅ Generación de la clave de acceso con PARÁMETROS EN ORDEN CORRECTO
            var claveAcceso = _claveAccesoGenerator.GenerarClaveAcceso(
                fechaEmision: DateTime.Now,
                tipoComprobante: "01",
                ruc: rucLimpio,
                ambiente: ambiente,
                tipoEmision: tipoEmision,
                establecimiento: establecimiento,
                puntoEmision: puntoEmision,
                secuencial: secuencial,
                codigoNumerico: null  // Se genera automáticamente
            );

            // ✅ Validación de la clave generada
            if (string.IsNullOrWhiteSpace(claveAcceso) || claveAcceso.Length != 49)
            {
                _logger?.LogError("❌ La clave de acceso generada tiene longitud incorrecta: {Longitud} (debe ser 49)", claveAcceso?.Length ?? 0);
                throw new InvalidOperationException($"La clave de acceso generada tiene longitud incorrecta: {claveAcceso?.Length ?? 0} (debe ser 49)");
            }

            if (!_claveAccesoGenerator.ValidarClaveAcceso(claveAcceso))
            {
                _logger?.LogError("❌ La clave de acceso generada NO pasa la validación del dígito verificador");
                _logger?.LogError("   Clave: {ClaveAcceso}", claveAcceso);
                throw new InvalidOperationException($"La clave de acceso generada es inválida: {claveAcceso}");
            }

            _logger?.LogInformation("✅ Clave de acceso generada y validada correctamente");
            _logger?.LogInformation("   Clave: {ClaveAcceso}", claveAcceso);
            _logger?.LogInformation("═══════════════════════════════════════════════════\n");


            // 5. Crear entidad Factura
            var factura = new Factura
            {
                NumeroFactura = numeroFactura,
                ClaveAcceso = claveAcceso,
                ClienteId = dto.ClienteId,
                UsuarioId = usuarioId,
                FechaEmision = DateTime.UtcNow,
                Ambiente = configEmpresa.AmbienteSRI == "2" ? Ambiente.PRODUCCION : Ambiente.PRUEBAS,
                TipoEmision = TipoEmision.NORMAL,
                Estado = EstadoFactura.BORRADOR,
                Observaciones = dto.Observaciones
            };
            // 6. Crear detalles y calcular totales
            var detalles = new List<DetalleFactura>();
        var subtotales = new Dictionary<TipoIVA, decimal>
        {
            { TipoIVA.IVA_0, 0 },
            { TipoIVA.IVA_15, 0 }
        };

    decimal descuentoTotal = 0;
    decimal ivaTotal = 0;

            Console.WriteLine("🔑 Clave de acceso generada:");
            Console.WriteLine($"  → Número Factura: {numeroFactura}");
            Console.WriteLine($"  → Establecimiento: {establecimiento}");
            Console.WriteLine($"  → Punto Emisión: {puntoEmision}");
            Console.WriteLine($"  → Secuencial: {secuencial}");
            Console.WriteLine($"  → Tipo Emisión: {tipoEmision}");
            Console.WriteLine($"  → Ambiente: {configEmpresa.AmbienteSRI}");
            Console.WriteLine($"  → Clave Completa: {claveAcceso}");


            foreach (var detalleDto in dto.Detalles)
    {
        var productoDetalle = productos.First(p => p.Id == detalleDto.ProductoId);

        decimal precioTotalSinImpuesto = detalleDto.Cantidad * detalleDto.PrecioUnitario;
        decimal descuentoLinea = detalleDto.Descuento;
        decimal baseImponible = precioTotalSinImpuesto - descuentoLinea;

        var tipoIVA = (TipoIVA)detalleDto.CodigoPorcentajeIVA;
        decimal tarifa = ObtenerTarifa(tipoIVA);
        decimal valorIVA = baseImponible * tarifa;
        decimal valorTotal = baseImponible + valorIVA;

        var detalleFactura = new DetalleFactura
        {
            ProductoId = productoDetalle.Id,
            CodigoPrincipal = productoDetalle.Codigo,
            Descripcion = productoDetalle.Nombre,
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

        detalles.Add(detalleFactura);

        subtotales[tipoIVA] += baseImponible;
        descuentoTotal += descuentoLinea;
        ivaTotal += valorIVA;
    }

    // 7. Asignar totales a la factura
    factura.Subtotal0 = subtotales[TipoIVA.IVA_0];
    factura.Subtotal15 = subtotales[TipoIVA.IVA_15];
    factura.SubtotalNoObjetoIVA = 0;
    factura.SubtotalExentoIVA = 0;
    factura.SubtotalConDescuento = subtotales.Values.Sum();
    factura.Descuento = descuentoTotal;
    factura.IVA15 = subtotales[TipoIVA.IVA_15] * 0.15m;
    factura.Propina = 0; // TODO: Implementar propina si es necesario
    factura.ImporteTotal = factura.SubtotalConDescuento + ivaTotal + factura.Propina;

    // Asignar detalles a la factura
    factura.Detalles = detalles;

    // 8. Guardar en base de datos
    try
    {
        _context.Facturas.Add(factura);
        await _context.SaveChangesAsync(cancellationToken);
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException($"Error al guardar factura en base de datos: {ex.Message} - Inner: {ex.InnerException?.Message}", ex);
    }

    // 9. Recargar con relaciones para el DTO
    var facturaCompleta = await _context.Facturas
        .AsNoTracking()
        .Include(f => f.Cliente)
        .Include(f => f.Usuario)
        .Include(f => f.Detalles)!.ThenInclude(d => d.Producto)
        .Include(f => f.InfoAdicional)
        .FirstOrDefaultAsync(f => f.Id == factura.Id, cancellationToken);

    // 10. Mapear a DTO manualmente
    return MapearAFacturaDto(facturaCompleta!);
}

        
        
        
        
   public async Task<PagedResultDto<FacturaDto>> ListarFacturasAsync(FiltroFacturaDto filtro, CancellationToken cancellationToken = default)
        {
            if (filtro == null)
            {
                throw new ArgumentNullException(nameof(filtro));
            }

            var query = _context.Facturas
                .AsNoTracking()
                .AsSplitQuery()
                .AsQueryable();

            if (filtro.ClienteId.HasValue)
            {
                query = query.Where(f => f.ClienteId == filtro.ClienteId.Value);
            }

            if (filtro.UsuarioId.HasValue)
            {
                query = query.Where(f => f.UsuarioId == filtro.UsuarioId.Value);
            }

            if (filtro.Estado.HasValue)
            {
                query = query.Where(f => f.Estado == filtro.Estado.Value);
            }

            if (filtro.FechaDesde.HasValue)
            {
                var desde = filtro.FechaDesde.Value.Date;
                query = query.Where(f => f.FechaEmision >= desde);
            }

            if (filtro.FechaHasta.HasValue)
            {
                var hasta = filtro.FechaHasta.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(f => f.FechaEmision <= hasta);
            }

            if (!string.IsNullOrWhiteSpace(filtro.NumeroFactura))
            {
                var numero = filtro.NumeroFactura.Trim();
                query = query.Where(f => f.NumeroFactura.Contains(numero));
            }

            if (!string.IsNullOrWhiteSpace(filtro.ClaveAcceso))
            {
                var clave = filtro.ClaveAcceso.Trim();
                query = query.Where(f => f.ClaveAcceso != null && f.ClaveAcceso.Contains(clave));
            }

            if (!string.IsNullOrWhiteSpace(filtro.TextoBusqueda))
            {
                var term = filtro.TextoBusqueda.Trim().ToLower();
                query = query.Where(f =>
                    (f.Cliente != null && (f.Cliente.Nombre1 + " " + f.Cliente.Apellido1).ToLower().Contains(term)) ||
                    (f.Usuario != null && f.Usuario.Username.ToLower().Contains(term)) ||
                    f.NumeroFactura.Contains(term));
            }

            var totalItems = await query.CountAsync(cancellationToken);

            var pageNumber = filtro.PageNumber < 1 ? 1 : filtro.PageNumber;
            var pageSize = filtro.PageSize < 1 ? 10 : filtro.PageSize;

            query = AplicarOrdenamiento(query, filtro.OrderBy, filtro.OrderAscending);

            var pagedQuery = query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize);

            var items = await ProyectarFacturaDto(pagedQuery)
                .ToListAsync(cancellationToken);

            CompletarDescripcionEstado(items);

            return new PagedResultDto<FacturaDto>
            {
                Items = items,
                TotalItems = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        // ==================== OBTENER POR ID ====================

        public async Task<FacturaDto?> ObtenerPorIdAsync(int facturaId, CancellationToken cancellationToken = default)
        {
            var dto = await ProyectarFacturaDto(
                    _context.Facturas
                        .AsNoTracking()
                        .AsSplitQuery()
                        .Where(f => f.Id == facturaId))
                .FirstOrDefaultAsync(cancellationToken);

            if (dto != null)
            {
                CompletarDescripcionEstado(new[] { dto });
            }

            return dto;
        }

        // ==================== ACTUALIZAR ESTADO ====================

        public async Task<Factura> ActualizarEstadoAsync(int facturaId, EstadoFactura nuevoEstado, CancellationToken cancellationToken = default)
        {
            var factura = await _context.Facturas.FirstOrDefaultAsync(f => f.Id == facturaId, cancellationToken);
            if (factura == null)
            {
                throw new KeyNotFoundException($"No existe una factura con Id {facturaId}.");
            }

            if (factura.Estado == nuevoEstado)
            {
                throw new InvalidOperationException("La factura ya se encuentra en el estado solicitado.");
            }

            if (!TransicionPermitida(factura.Estado, nuevoEstado))
            {
                throw new InvalidOperationException($"No es posible cambiar la factura {factura.NumeroFactura} de {factura.Estado} a {nuevoEstado}.");
            }

            factura.Estado = nuevoEstado;
            factura.FechaModificacion = DateTime.UtcNow;

            if (nuevoEstado == EstadoFactura.AUTORIZADA)
            {
                var fechaAutorizacion = DateTime.UtcNow;
                factura.FechaAutorizacion = fechaAutorizacion;
                factura.FechaHoraAutorizacion = fechaAutorizacion;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return factura;
        }

        // ==================== ANULAR FACTURA ====================

        public async Task<Factura> AnularFacturaAsync(int facturaId, int usuarioId, string? motivo = null, CancellationToken cancellationToken = default)
        {
            var factura = await _context.Facturas.FirstOrDefaultAsync(f => f.Id == facturaId, cancellationToken);
            if (factura == null)
            {
                throw new KeyNotFoundException($"No existe una factura con Id {facturaId}.");
            }

            if (factura.Estado != EstadoFactura.AUTORIZADA)
            {
                throw new InvalidOperationException("Solo se pueden anular facturas en estado AUTORIZADA.");
            }

            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsuarioId == usuarioId, cancellationToken);

            if (usuario == null)
            {
                throw new KeyNotFoundException($"No existe un usuario con Id {usuarioId}.");
            }

            if (!string.Equals(usuario.Rol?.NombreRol, RolAdministrador, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Solo un administrador puede anular facturas autorizadas.");
            }

            factura.Estado = EstadoFactura.ANULADA;
            factura.FechaModificacion = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(motivo))
            {
                factura.Observaciones = RegistrarMotivoAnulacion(factura.Observaciones, motivo);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return factura;
        }

        // ==================== FIRMAR Y ALMACENAR XML (T-064) ====================

        /// <summary>
        /// T-064: Firma el XML de una factura y almacena el resultado.
        /// Genera el XML si no existe, lo firma con el certificado digital configurado,
        /// guarda el XML firmado en el sistema de archivos y actualiza los campos
        /// XmlPath y XmlFirmadoPath en la base de datos.
        /// </summary>
        public async Task<(string XmlPath, string XmlFirmadoPath)> FirmarYAlmacenarXmlAsync(int facturaId, CancellationToken cancellationToken = default)
        {
            // Validar que los servicios estén inyectados
            if (_xmlGeneratorService == null)
                throw new InvalidOperationException("El servicio de generación de XML no está configurado.");
            
            if (_firmaElectronicaService == null)
                throw new InvalidOperationException("El servicio de firma electrónica no está configurado.");
            
            if (_facturaRepository == null)
                throw new InvalidOperationException("El repositorio de facturas no está configurado.");

            // 1. Obtener la factura
            var factura = await _context.Facturas
                .Include(f => f.Cliente)
                    .ThenInclude(c => c!.TipoIdentificacion)
                .Include(f => f.Detalles)
                .Include(f => f.InfoAdicional)
                .FirstOrDefaultAsync(f => f.Id == facturaId, cancellationToken);

            if (factura == null)
            {
                throw new KeyNotFoundException($"No existe una factura con Id {facturaId}.");
            }

            // 2. Validar estado de la factura (debe estar al menos BORRADOR para firmar)
            var estadosPermitidosParaFirmar = new[] 
            { 
                EstadoFactura.BORRADOR,  // Permitir firmar desde borrador
                EstadoFactura.GENERADA, 
                EstadoFactura.FIRMADA,  // Permitir re-firmar si es necesario
                EstadoFactura.DEVUELTA,
                EstadoFactura.NO_AUTORIZADA 
            };

            if (!estadosPermitidosParaFirmar.Contains(factura.Estado))
            {
                throw new InvalidOperationException(
                    $"La factura debe estar en estado BORRADOR, GENERADA, FIRMADA, DEVUELTA o NO_AUTORIZADA para firmar. " +
                    $"Estado actual: {factura.Estado}");
            }

            // 3. Generar XML si no existe
            string xmlContent;
            string xmlPath = factura.XmlPath ?? string.Empty;

            if (string.IsNullOrEmpty(factura.XmlPath))
            {
                // Crear DTO para generar XML
                var facturaDto = MapearAFacturaDto(factura);
                xmlContent = _xmlGeneratorService.GenerarXmlFactura(facturaDto);
                
                // Guardar XML original
                xmlPath = await _xmlGeneratorService.GuardarXmlEnArchivo(xmlContent, factura.ClaveAcceso);
            }
            else
            {
                // Leer XML existente
                var rutaAbsoluta = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, 
                    "wwwroot", 
                    factura.XmlPath.Replace("/", Path.DirectorySeparatorChar.ToString()));
                
                if (!File.Exists(rutaAbsoluta))
                {
                    throw new FileNotFoundException($"No se encontró el archivo XML en: {factura.XmlPath}");
                }
                
                xmlContent = await File.ReadAllTextAsync(rutaAbsoluta, cancellationToken);
            }

            // 4. Firmar el XML
            var xmlFirmado = await _firmaElectronicaService.FirmarXml(xmlContent);

            // 5. Guardar XML firmado
            var xmlFirmadoPath = await _xmlGeneratorService.GuardarXmlFirmadoEnArchivo(xmlFirmado, factura.ClaveAcceso);

            // 6. Actualizar la factura en la base de datos
            await _facturaRepository.ActualizarRespuestaSRIAsync(
                facturaId,
                numeroAutorizacion: null,
                fechaAutorizacion: null,
                xmlPath: xmlPath,
                xmlFirmadoPath: xmlFirmadoPath,
                pdfPath: null,
                mensajesSRI: null);

            // 7. Cambiar estado a FIRMADA si estaba en GENERADA o BORRADOR
            if (factura.Estado == EstadoFactura.GENERADA || factura.Estado == EstadoFactura.BORRADOR)
            {
                await ActualizarEstadoAsync(facturaId, EstadoFactura.FIRMADA, cancellationToken);
            }

            return (xmlPath, xmlFirmadoPath);
        }

        // ==================== MÉTODOS AUXILIARES ====================

        private static decimal ObtenerTarifa(TipoIVA tipo)
        {
            return tipo switch
            {
                TipoIVA.IVA_0 => 0m,
                TipoIVA.IVA_15 => 0.15m,
                _ => throw new ArgumentException($"Tipo IVA no válido: {tipo}")
            };
        }

        private static IQueryable<Factura> AplicarOrdenamiento(IQueryable<Factura> query, string? orderBy, bool ascending)
        {
            return orderBy?.ToLower() switch
            {
                "numero" or "numerofactura" => ascending ? query.OrderBy(f => f.NumeroFactura) : query.OrderByDescending(f => f.NumeroFactura),
                "cliente" => ascending
                    ? query.OrderBy(f => f.Cliente != null ? f.Cliente.Nombre1 : string.Empty)
                    : query.OrderByDescending(f => f.Cliente != null ? f.Cliente.Nombre1 : string.Empty),
                "total" or "importetotal" => ascending ? query.OrderBy(f => f.ImporteTotal) : query.OrderByDescending(f => f.ImporteTotal),
                _ => ascending ? query.OrderBy(f => f.FechaEmision) : query.OrderByDescending(f => f.FechaEmision)
            };
        }

        private static bool TransicionPermitida(EstadoFactura estadoActual, EstadoFactura nuevoEstado)
        {
            return _transicionesPermitidas.TryGetValue(estadoActual, out var permitidos) && permitidos.Contains(nuevoEstado);
        }

        private static string RegistrarMotivoAnulacion(string? observacionesActuales, string motivo)
        {
            var prefijo = $"ANULADA ({DateTime.UtcNow:yyyy-MM-dd HH:mm}): ";
            var nuevoTexto = prefijo + motivo.Trim();

            if (string.IsNullOrWhiteSpace(observacionesActuales))
            {
                return nuevoTexto;
            }

            return string.Join(Environment.NewLine, observacionesActuales.Trim(), nuevoTexto);
        }

        private static IQueryable<FacturaDto> ProyectarFacturaDto(IQueryable<Factura> query)
        {
            return query.Select(f => new FacturaDto
            {
                Id = f.Id,
                NumeroFactura = f.NumeroFactura,
                ClaveAcceso = f.ClaveAcceso ?? string.Empty,
                FechaEmision = f.FechaEmision,
                Estado = f.Estado.ToString(),
                EstadoDescripcion = string.Empty,
                ClienteId = f.ClienteId,
                Cliente = f.Cliente == null
                    ? null
                    : new ClienteFacturaDto
                    {
                        Id = f.Cliente.ClienteId,
                        TipoIdentificacion = f.Cliente.TipoIdentificacion != null
                            ? f.Cliente.TipoIdentificacion.CodigoSRI
                            : string.Empty,
                        Identificacion = f.Cliente.Identificacion,
                        RazonSocial = (f.Cliente.Nombre1 + " " + (f.Cliente.Nombre2 ?? string.Empty) + " " + f.Cliente.Apellido1 + " " + (f.Cliente.Apellido2 ?? string.Empty)).Trim(),
                        NombreComercial = null,
                        Direccion = f.Cliente.Direccion,
                        Telefono = f.Cliente.Telefono,
                        Email = f.Cliente.Email
                    },
                UsuarioId = f.UsuarioId,
                UsuarioNombre = f.Usuario != null
                    ? (f.Usuario.Nombre1 + " " + (f.Usuario.Nombre2 ?? string.Empty) + " " + f.Usuario.Apellido1 + " " + (f.Usuario.Apellido2 ?? string.Empty)).Trim()
                    : string.Empty,
                Detalles = f.Detalles.Select(d => new DetalleFacturaDto
                {
                    Id = d.Id,
                    ProductoId = d.ProductoId ?? 0,
                    CodigoPrincipal = d.CodigoPrincipal,
                    CodigoAuxiliar = d.CodigoAuxiliar,
                    Descripcion = d.Descripcion,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    Descuento = d.Descuento,
                    PrecioTotalSinImpuesto = d.PrecioTotalSinImpuesto,
                    BaseImponible = d.BaseImponible,
                    CodigoPorcentajeIVA = d.CodigoPorcentajeIVA,
                    Tarifa = d.Tarifa,
                    Valor = d.Valor,
                    ValorTotal = d.ValorTotal,
                    InfoAdicional = null
                }).ToList(),
                Subtotal0 = f.Subtotal0,
                Subtotal15 = f.Subtotal15,
                SubtotalTotal = f.SubtotalConDescuento,
                TotalDescuento = f.Descuento,
                TotalIVA = f.IVA15,
                Total = f.ImporteTotal,
                Observaciones = f.Observaciones,
                InfoAdicional = f.InfoAdicional.Select(i => new InfoAdicionalDto
                {
                    Nombre = i.Nombre,
                    Valor = i.Valor
                }).ToList(),
                XmlPath = f.XmlPath,
                XmlFirmadoPath = f.XmlFirmadoPath,
                PdfPath = f.PdfPath,
                NumeroAutorizacion = f.NumeroAutorizacion,
                FechaAutorizacion = f.FechaAutorizacion ?? f.FechaHoraAutorizacion,
                MensajesSRI = f.MensajesSRI,
                FechaCreacion = f.FechaCreacion,
                FechaModificacion = f.FechaModificacion
            });
        }

        private static void CompletarDescripcionEstado(IEnumerable<FacturaDto> facturas)
        {
            foreach (var dto in facturas)
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Estado))
                {
                    continue;
                }

                if (!Enum.TryParse(dto.Estado, true, out EstadoFactura estado))
                {
                    continue;
                }

                dto.EstadoDescripcion = ObtenerDescripcionEstado(estado);
            }
        }

        /// <summary>
        /// Mapea manualmente una Factura a FacturaDto
        /// </summary>
        private static FacturaDto MapearAFacturaDto(Factura factura)
        {
            return new FacturaDto
            {
                Id = factura.Id,
                ClienteId = factura.ClienteId,
                NumeroFactura = factura.NumeroFactura,
                ClaveAcceso = factura.ClaveAcceso ?? string.Empty,
                FechaEmision = factura.FechaEmision,
                Estado = factura.Estado.ToString(),
                Cliente = factura.Cliente != null ? new ClienteFacturaDto
                {
                    Id = factura.Cliente.ClienteId,
                    TipoIdentificacion = factura.Cliente.TipoIdentificacion?.CodigoSRI ?? string.Empty,
                    Identificacion = factura.Cliente.Identificacion,
                    RazonSocial = factura.Cliente.NombreCompleto(),
                    NombreComercial = null,
                    Direccion = factura.Cliente.Direccion,
                    Email = factura.Cliente.Email,
                    Telefono = factura.Cliente.Telefono
                } : null,
                UsuarioId = factura.UsuarioId,
                UsuarioNombre = factura.Usuario != null 
                    ? string.Join(" ", new[] { factura.Usuario.Nombre1, factura.Usuario.Nombre2, factura.Usuario.Apellido1, factura.Usuario.Apellido2 }.Where(n => !string.IsNullOrWhiteSpace(n)))
                    : string.Empty,
                EstadoDescripcion = ObtenerDescripcionEstado(factura.Estado),
                Subtotal0 = factura.Subtotal0,
                Subtotal15 = factura.Subtotal15,
                SubtotalTotal = factura.SubtotalConDescuento,
                TotalDescuento = factura.Descuento,
                TotalIVA = CalcularTotalIVA(factura),
                Total = factura.ImporteTotal,
                Detalles = factura.Detalles?.Select(d => new DetalleFacturaDto
                {
                    Id = d.Id,
                    ProductoId = d.ProductoId ?? 0,
                    CodigoPrincipal = d.CodigoPrincipal,
                    CodigoAuxiliar = d.CodigoAuxiliar,
                    Descripcion = d.Descripcion,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    Descuento = d.Descuento,
                    PrecioTotalSinImpuesto = d.PrecioTotalSinImpuesto,
                    BaseImponible = d.BaseImponible,
                    CodigoPorcentajeIVA = d.CodigoPorcentajeIVA,
                    Tarifa = d.Tarifa,
                    Valor = d.Valor,
                    ValorTotal = d.ValorTotal,
                    InfoAdicional = null
                }).ToList() ?? new List<DetalleFacturaDto>(),
                InfoAdicional = factura.InfoAdicional?.Select(i => new InfoAdicionalDto
                {
                    Nombre = i.Nombre,
                    Valor = i.Valor
                }).ToList() ?? new List<InfoAdicionalDto>(),
                Observaciones = factura.Observaciones,
                XmlPath = factura.XmlPath,
                XmlFirmadoPath = factura.XmlFirmadoPath,
                PdfPath = factura.PdfPath,
                NumeroAutorizacion = factura.NumeroAutorizacion,
                FechaAutorizacion = factura.FechaAutorizacion ?? factura.FechaHoraAutorizacion,
                MensajesSRI = factura.MensajesSRI,
                FechaCreacion = factura.FechaCreacion,
                FechaModificacion = factura.FechaModificacion
            };
        }

        private static decimal CalcularTotalIVA(Factura factura)
        {
            return factura.IVA15;
        }

        private static string ObtenerDescripcionEstado(EstadoFactura estado)
        {
            return estado switch
            {
                EstadoFactura.BORRADOR => "Borrador",
                EstadoFactura.GENERADA => "XML Generado",
                EstadoFactura.FIRMADA => "Firmada Electrónicamente",
                EstadoFactura.ENVIADA => "Enviada al SRI",
                EstadoFactura.RECIBIDA => "Recibida por SRI",
                EstadoFactura.AUTORIZADA => "Autorizada",
                EstadoFactura.NO_AUTORIZADA => "No Autorizada",
                EstadoFactura.DEVUELTA => "Devuelta (Reenviar)",
                EstadoFactura.ANULADA => "Anulada",
                _ => estado.ToString()
            };
        }
    }
}