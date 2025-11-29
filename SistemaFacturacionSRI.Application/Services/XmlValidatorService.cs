using System.Xml;
using System.Xml.Schema;

namespace SistemaFacturacionSRI.Application.Services;

/// <summary>
/// Servicio para validar XMLs contra esquemas XSD del SRI
/// T-034: SPRINT 3 - DÍA 2
/// </summary>
public class XmlValidatorService
{
    private readonly string _xsdPath;
    private readonly List<string> _erroresValidacion;

    public XmlValidatorService()
    {
        // Ruta al esquema XSD de facturas
        _xsdPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "docs", "xsd", "factura_v1.1.0.xsd");
        _erroresValidacion = new List<string>();
    }

    /// <summary>
    /// Valida un XML contra el esquema XSD de facturas
    /// </summary>
    /// <param name="xmlContent">Contenido XML a validar</param>
    /// <returns>True si es válido, False si tiene errores</returns>
    public bool ValidarXmlContraEsquema(string xmlContent, out List<string> errores)
    {
        _erroresValidacion.Clear();
        errores = new List<string>();

        try
        {
            // Verificar que existe el archivo XSD
            if (!File.Exists(_xsdPath))
            {
                errores.Add($"No se encontró el archivo XSD en: {_xsdPath}");
                return false;
            }

            // Cargar el esquema XSD
            XmlSchemaSet schemas = new XmlSchemaSet();
            schemas.Add("", _xsdPath);

            // Configurar opciones de validación
            XmlReaderSettings settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.Schema,
                Schemas = schemas
            };

            // Agregar manejador de eventos para errores
            settings.ValidationEventHandler += ValidationCallback;

            // Crear reader para el XML
            using (StringReader stringReader = new StringReader(xmlContent))
            using (XmlReader reader = XmlReader.Create(stringReader, settings))
            {
                // Leer todo el documento para disparar validación
                while (reader.Read()) { }
            }

            // Si hay errores, copiarlos a la lista de salida
            if (_erroresValidacion.Any())
            {
                errores.AddRange(_erroresValidacion);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            errores.Add($"Error al validar XML: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Callback para errores de validación
    /// </summary>
    private void ValidationCallback(object? sender, ValidationEventArgs e)
    {
        string mensaje = e.Severity == XmlSeverityType.Warning
            ? $"ADVERTENCIA: {e.Message}"
            : $"ERROR: {e.Message}";

        _erroresValidacion.Add(mensaje);
    }

    /// <summary>
    /// Valida un archivo XML contra el esquema
    /// </summary>
    /// <param name="xmlFilePath">Ruta del archivo XML</param>
    /// <param name="errores">Lista de errores encontrados</param>
    /// <returns>True si es válido</returns>
    public bool ValidarArchivoXml(string xmlFilePath, out List<string> errores)
    {
        errores = new List<string>();

        try
        {
            if (!File.Exists(xmlFilePath))
            {
                errores.Add($"No se encontró el archivo XML: {xmlFilePath}");
                return false;
            }

            string xmlContent = File.ReadAllText(xmlFilePath);
            return ValidarXmlContraEsquema(xmlContent, out errores);
        }
        catch (Exception ex)
        {
            errores.Add($"Error al leer archivo XML: {ex.Message}");
            return false;
        }
    }
}