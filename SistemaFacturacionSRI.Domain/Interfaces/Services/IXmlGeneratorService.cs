using SistemaFacturacionSRI.Domain.Models.XML;
using SistemaFacturacionSRI.Domain.DTOs.Factura;

namespace SistemaFacturacionSRI.Domain.Interfaces.Services;

public interface IXmlGeneratorService
{
    Task<string> GenerarXmlFacturaAsync(int facturaId);

    Task<FacturaXML> GenerarObjetoFacturaXmlAsync(int facturaId);

    Task<(bool EsValido, List<string> Errores)> ValidarXmlContraEsquemaAsync(string xmlContent);

    Task<string> GuardarXmlEnArchivoAsync(string xmlContent, string claveAcceso);

    string SerializarFacturaXml(FacturaXML facturaXml);

    FacturaXML DeserializarFacturaXml(string xmlContent);

    Task<(string XmlContent, string RutaArchivo)> GenerarYGuardarXmlAsync(int facturaId);
        string GenerarXmlFactura(FacturaDto factura);

    Task<ResultadoValidacion> ValidarXmlContraEsquema(string xmlContent, string? xsdPath = default);
    Task<string> GuardarXmlEnArchivo(string xmlContent, string claveAcceso);
    Task<string> GenerarYValidarXml(FacturaDto factura);
    
    /// <summary>
    /// T-064: Guarda el XML firmado en el sistema de archivos
    /// </summary>
    /// <param name="xmlFirmado">Contenido del XML firmado</param>
    /// <param name="claveAcceso">Clave de acceso de 49 dígitos</param>
    /// <returns>Ruta relativa donde se guardó el archivo</returns>
    Task<string> GuardarXmlFirmadoEnArchivo(string xmlFirmado, string claveAcceso);
}