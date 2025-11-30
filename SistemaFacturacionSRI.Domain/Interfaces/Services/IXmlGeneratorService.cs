using SistemaFacturacionSRI.Domain.Models.XML;

namespace SistemaFacturacionSRI.Domain.Interfaces.Services;

/// <summary>
/// Interfaz para el servicio de generación de XML de facturas electrónicas
/// T-042: SPRINT 3 - DÍA 5
/// 
/// Responsable de convertir entidades de Factura a XML según el estándar del SRI
/// </summary>
public interface IXmlGeneratorService
{
    /// <summary>
    /// Genera el XML completo de una factura a partir de su ID
    /// </summary>
    /// <param name="facturaId">ID de la factura en la base de datos</param>
    /// <returns>Contenido XML como string</returns>
    Task<string> GenerarXmlFacturaAsync(int facturaId);

    /// <summary>
    /// Genera el objeto FacturaXML a partir de una factura
    /// Útil para testing o manipulación antes de serializar
    /// </summary>
    /// <param name="facturaId">ID de la factura</param>
    /// <returns>Objeto FacturaXML completo</returns>
    Task<FacturaXML> GenerarObjetoFacturaXmlAsync(int facturaId);

    /// <summary>
    /// Valida un XML contra el esquema XSD del SRI
    /// </summary>
    /// <param name="xmlContent">Contenido XML a validar</param>
    /// <returns>True si es válido, False si tiene errores</returns>
    Task<(bool EsValido, List<string> Errores)> ValidarXmlContraEsquemaAsync(string xmlContent);

    /// <summary>
    /// Guarda el XML generado en un archivo físico
    /// </summary>
    /// <param name="xmlContent">Contenido XML</param>
    /// <param name="claveAcceso">Clave de acceso para nombrar el archivo</param>
    /// <returns>Ruta del archivo guardado</returns>
    Task<string> GuardarXmlEnArchivoAsync(string xmlContent, string claveAcceso);

    /// <summary>
    /// Serializa un objeto FacturaXML a string XML
    /// </summary>
    /// <param name="facturaXml">Objeto FacturaXML</param>
    /// <returns>XML serializado como string</returns>
    string SerializarFacturaXml(FacturaXML facturaXml);

    /// <summary>
    /// Deserializa un XML string a objeto FacturaXML
    /// </summary>
    /// <param name="xmlContent">XML como string</param>
    /// <returns>Objeto FacturaXML</returns>
    FacturaXML DeserializarFacturaXml(string xmlContent);

    /// <summary>
    /// Genera y guarda el XML de una factura en un solo paso
    /// </summary>
    /// <param name="facturaId">ID de la factura</param>
    /// <returns>Tupla con el XML generado y la ruta del archivo</returns>
    Task<(string XmlContent, string RutaArchivo)> GenerarYGuardarXmlAsync(int facturaId);
}