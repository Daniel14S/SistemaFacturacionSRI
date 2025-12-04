using SistemaFacturacionSRI.Domain.DTOs.Common;
using SistemaFacturacionSRI.Domain.DTOs.Factura;
using SistemaFacturacionSRI.Domain.Entities;
using SistemaFacturacionSRI.Domain.Enums;

namespace SistemaFacturacionSRI.Domain.Interfaces.Services
{
    /// <summary>
    /// Servicio para gestión completa de facturas electrónicas.
    /// T-017: Definición de operaciones del módulo de facturación.
    /// </summary>
    public interface IFacturaService
    {
        /// <summary>
        /// T-020: Crea una nueva factura con todas las validaciones de negocio.
        /// Valida cliente activo, productos activos y con stock, calcula totales por tarifa IVA,
        /// obtiene secuencia automática y guarda en estado BORRADOR.
        /// </summary>
        /// <param name="dto">Datos de la factura a crear</param>
        /// <param name="usuarioId">ID del usuario que crea la factura (del token JWT)</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>DTO de la factura creada con todos los cálculos</returns>
        /// <exception cref="InvalidOperationException">Si las validaciones de negocio fallan</exception>
        /// <exception cref="KeyNotFoundException">Si cliente o productos no existen</exception>
        Task<FacturaDto> CrearFacturaAsync(CrearFacturaDto dto, int usuarioId, CancellationToken cancellationToken = default);

        /// <summary>
        /// T-022: Lista facturas con filtros dinámicos y paginación.
        /// Soporta filtros por cliente, usuario, estado, rango de fechas y número.
        /// </summary>
        /// <param name="filtro">Criterios de filtrado y paginación</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado paginado con las facturas que cumplen los criterios</returns>
        Task<PagedResultDto<FacturaDto>> ListarFacturasAsync(FiltroFacturaDto filtro, CancellationToken cancellationToken = default);

        /// <summary>
        /// T-022: Obtiene una factura completa por ID.
        /// Incluye: cliente, usuario, detalles completos e información adicional.
        /// </summary>
        /// <param name="facturaId">Identificador de la factura</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Factura completa o null si no existe</returns>
        Task<FacturaDto?> ObtenerPorIdAsync(int facturaId, CancellationToken cancellationToken = default);

        /// <summary>
        /// T-023: Cambia el estado de una factura validando las transiciones permitidas.
        /// También registra la fecha del cambio y, si aplica, la fecha de autorización.
        /// </summary>
        /// <param name="facturaId">Identificador de la factura</param>
        /// <param name="nuevoEstado">Estado al que se desea mover la factura</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Factura actualizada con el nuevo estado</returns>
        /// <exception cref="KeyNotFoundException">Si la factura no existe</exception>
        /// <exception cref="InvalidOperationException">Si la transición no está permitida</exception>
        Task<Factura> ActualizarEstadoAsync(int facturaId, EstadoFactura nuevoEstado, CancellationToken cancellationToken = default);

        /// <summary>
        /// T-024: Anula una factura previamente autorizada validando que el usuario tenga permisos.
        /// Solo los administradores pueden anular facturas.
        /// </summary>
        /// <param name="facturaId">Identificador de la factura a anular</param>
        /// <param name="usuarioId">Usuario que solicita la anulación (se valida que sea administrador)</param>
        /// <param name="motivo">Motivo opcional para registrar en las observaciones</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Factura en estado ANULADA</returns>
        /// <exception cref="KeyNotFoundException">Si la factura o el usuario no existen</exception>
        /// <exception cref="InvalidOperationException">Si la factura no está autorizada</exception>
        /// <exception cref="UnauthorizedAccessException">Si el usuario no tiene permisos suficientes</exception>
        Task<Factura> AnularFacturaAsync(int facturaId, int usuarioId, string? motivo = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// T-064: Firma el XML de una factura y almacena el resultado.
        /// Genera el XML si no existe, lo firma con el certificado digital configurado,
        /// guarda el XML firmado en el sistema de archivos y actualiza los campos
        /// XmlPath y XmlFirmadoPath en la base de datos.
        /// </summary>
        /// <param name="facturaId">Identificador de la factura</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Tupla con (RutaXmlOriginal, RutaXmlFirmado)</returns>
        /// <exception cref="KeyNotFoundException">Si la factura no existe</exception>
        /// <exception cref="InvalidOperationException">Si la factura no está en estado válido para firmar</exception>
        Task<(string XmlPath, string XmlFirmadoPath)> FirmarYAlmacenarXmlAsync(int facturaId, CancellationToken cancellationToken = default);
    }
}