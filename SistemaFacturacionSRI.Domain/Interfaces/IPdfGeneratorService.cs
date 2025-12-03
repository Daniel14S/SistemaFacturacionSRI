namespace SistemaFacturacionSRI.Domain.Interfaces;

/// <summary>
/// Servicio para generación de documentos PDF y códigos de barras/QR
/// </summary>
public interface IPdfGeneratorService
{
    /// <summary>
    /// Genera el RIDE (Representación Impresa de Documento Electrónico) de una factura en formato PDF
    /// </summary>
    /// <param name="facturaId">ID de la factura</param>
    /// <param name="outputPath">Ruta donde se guardará el PDF generado</param>
    /// <returns>Ruta completa del archivo PDF generado</returns>
    Task<string> GenerarRideAsync(int facturaId, string outputPath);

    /// <summary>
    /// Genera un código de barras o QR para una factura
    /// </summary>
    /// <param name="claveAcceso">Clave de acceso de la factura (49 dígitos)</param>
    /// <param name="outputPath">Ruta donde se guardará la imagen del código</param>
    /// <param name="usarQr">Si es true genera QR, si es false genera código de barras</param>
    /// <returns>Ruta completa del archivo de imagen generado</returns>
    Task<string> GenerarCodigoBarrasAsync(string claveAcceso, string outputPath, bool usarQr = true);
}
