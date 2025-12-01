using SistemaFacturacionSRI.Domain.DTOs.Factura;

namespace SistemaFacturacionSRI.Application.DTOs.Facturacion
{
    /// <summary>
    /// DTO completo de factura con todos los detalles
    /// </summary>
    public class FacturaDetalleCompletaDto
    {
        public int Id { get; set; }
        public string NumeroFactura { get; set; } = string.Empty;
        public string ClaveAcceso { get; set; } = string.Empty;

        // Información del cliente
        public ClienteDto Cliente { get; set; } = new();

        // Información del usuario emisor
        public UsuarioDto Usuario { get; set; } = new();

        // Fechas
        public DateTime FechaEmision { get; set; }
        public DateTime? FechaAutorizacion { get; set; }

        // Configuración SRI
        public string Ambiente { get; set; } = string.Empty;
        public string TipoEmision { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;

        // Totales
        public decimal Subtotal0 { get; set; }
        public decimal Subtotal12 { get; set; }
        public decimal Subtotal15 { get; set; }
        public decimal SubtotalConDescuento { get; set; }
        public decimal Descuento { get; set; }
        public decimal IVA15 { get; set; }
        public decimal Propina { get; set; }
        public decimal ImporteTotal { get; set; }

        // Detalles
        public List<DetalleFacturaCompletaDto> Detalles { get; set; } = new();

        // Información adicional
        public List<InfoAdicionalDto> InfosAdicionales { get; set; } = new();

        // Respuesta SRI
        public string? NumeroAutorizacion { get; set; }
        public DateTime? FechaHoraAutorizacion { get; set; }
        public string? MensajesSRI { get; set; }
        public string? Observaciones { get; set; }

        // Rutas a archivos
        public string? XmlPath { get; set; }
        public string? XmlFirmadoPath { get; set; }
        public string? PdfPath { get; set; }
    }

    /// <summary>
    /// DTO de detalle completo para visualización
    /// </summary>
    public class DetalleFacturaCompletaDto
    {
        public int Id { get; set; }
        public string CodigoPrincipal { get; set; } = string.Empty;
        public string? CodigoAuxiliar { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Descuento { get; set; }
        public decimal PrecioTotalSinImpuesto { get; set; }
        public int CodigoPorcentajeIVA { get; set; }
        public decimal Tarifa { get; set; }
        public decimal BaseImponible { get; set; }
        public decimal Valor { get; set; }
        public decimal ValorTotal { get; set; }
    }

    /// <summary>
    /// DTO simplificado de Cliente
    /// </summary>
    public class ClienteDto
    {
        public int Id { get; set; }
        public string TipoIdentificacion { get; set; } = string.Empty;
        public string Identificacion { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string? Direccion { get; set; }
        public string? Email { get; set; }
        public string? Telefono { get; set; }
    }

    /// <summary>
    /// DTO simplificado de Usuario
    /// </summary>
    public class UsuarioDto
    {
        public int Id { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
    }
}