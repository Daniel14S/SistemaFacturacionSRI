using Microsoft.EntityFrameworkCore;
using SistemaFacturacionSRI.Domain.Entities;
using SistemaFacturacionSRI.Domain.Enums;

namespace SistemaFacturacionSRI.Infrastructure.Data.Seeds
{
    /// <summary>
    /// Registra facturas de ejemplo para ambientes de desarrollo.
    /// Marca los comprobantes como rechazados/no autorizados para facilitar pruebas de endpoints.
    /// </summary>
    public static class FacturaSeed
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            var fechaRechazo = new DateTime(2024, 11, 15, 10, 0, 0, DateTimeKind.Utc);
            var fechaDevuelta = new DateTime(2024, 11, 18, 14, 30, 0, DateTimeKind.Utc);

            modelBuilder.Entity<Factura>().HasData(
                new Factura
                {
                    Id = 1001,
                    NumeroFactura = "001-001-000000101",
                    ClaveAcceso = "1234567890123456789012345678901234567890123456789",
                    ClienteId = 1,
                    UsuarioId = 1,
                    FechaEmision = fechaRechazo,
                    Ambiente = Ambiente.PRUEBAS,
                    TipoEmision = TipoEmision.NORMAL,
                    Estado = EstadoFactura.NO_AUTORIZADA,
                    Subtotal0 = 0m,
                    Subtotal15 = 100m,
                    SubtotalNoObjetoIVA = 0m,
                    SubtotalExentoIVA = 0m,
                    SubtotalConDescuento = 100m,
                    Descuento = 0m,
                    IVA15 = 15m,
                    Propina = 0m,
                    ImporteTotal = 115m,
                    NumeroAutorizacion = null,
                    FechaAutorizacion = null,
                    FechaHoraAutorizacion = null,
                    XmlPath = null,
                    XmlFirmadoPath = null,
                    PdfPath = "comprobantes/pdf/RIDE_1234567890123456789012345678901234567890123456789.pdf",
                    MensajesSRI = "ERROR 70: Clave de acceso inválida",
                    Observaciones = "Factura de prueba rechazada por el SRI",
                    FechaCreacion = fechaRechazo,
                    FechaModificacion = fechaRechazo
                },
                new Factura
                {
                    Id = 1002,
                    NumeroFactura = "001-001-000000102",
                    ClaveAcceso = "9876543210987654321098765432109876543210987654321",
                    ClienteId = 2,
                    UsuarioId = 2,
                    FechaEmision = fechaDevuelta,
                    Ambiente = Ambiente.PRUEBAS,
                    TipoEmision = TipoEmision.NORMAL,
                    Estado = EstadoFactura.DEVUELTA,
                    Subtotal0 = 0m,
                    Subtotal15 = 90m,
                    SubtotalNoObjetoIVA = 0m,
                    SubtotalExentoIVA = 0m,
                    SubtotalConDescuento = 85m,
                    Descuento = 5m,
                    IVA15 = 12.75m,
                    Propina = 0m,
                    ImporteTotal = 97.75m,
                    NumeroAutorizacion = null,
                    FechaAutorizacion = null,
                    FechaHoraAutorizacion = null,
                    XmlPath = null,
                    XmlFirmadoPath = null,
                    PdfPath = null,
                    MensajesSRI = "DEVUELTA: Falta detalle de impuestos",
                    Observaciones = "Factura de prueba devuelta para corrección",
                    FechaCreacion = fechaDevuelta,
                    FechaModificacion = fechaDevuelta
                }
            );

            modelBuilder.Entity<DetalleFactura>().HasData(
                new DetalleFactura
                {
                    Id = 2001,
                    FacturaId = 1001,
                    ProductoId = null,
                    CodigoPrincipal = "PRD-001",
                    CodigoAuxiliar = "SKU-001",
                    Descripcion = "Producto demo rechazado",
                    Cantidad = 2m,
                    PrecioUnitario = 50m,
                    Descuento = 0m,
                    PrecioTotalSinImpuesto = 100m,
                    CodigoPorcentajeIVA = (int)TipoIVA.IVA_15,
                    Tarifa = 15m,
                    BaseImponible = 100m,
                    Valor = 15m,
                    ValorTotal = 115m,
                    FechaCreacion = fechaRechazo,
                    FechaModificacion = null
                },
                new DetalleFactura
                {
                    Id = 2002,
                    FacturaId = 1002,
                    ProductoId = null,
                    CodigoPrincipal = "PRD-002",
                    CodigoAuxiliar = "SKU-002",
                    Descripcion = "Producto demo devuelto",
                    Cantidad = 3m,
                    PrecioUnitario = 30m,
                    Descuento = 5m,
                    PrecioTotalSinImpuesto = 90m,
                    CodigoPorcentajeIVA = (int)TipoIVA.IVA_15,
                    Tarifa = 15m,
                    BaseImponible = 85m,
                    Valor = 12.75m,
                    ValorTotal = 97.75m,
                    FechaCreacion = fechaDevuelta,
                    FechaModificacion = null
                }
            );

            modelBuilder.Entity<InfoAdicional>().HasData(
                new InfoAdicional
                {
                    Id = 3001,
                    FacturaId = 1001,
                    Nombre = "motivo",
                    Valor = "Clave de acceso observada"
                },
                new InfoAdicional
                {
                    Id = 3002,
                    FacturaId = 1002,
                    Nombre = "motivo",
                    Valor = "Documento devuelto para corrección"
                }
            );
        }
    }
}
