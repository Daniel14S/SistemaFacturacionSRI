using System.Xml.Serialization;

namespace SistemaFacturacionSRI.Application.Models.XML;

/// <summary>
/// Modelo XML para información adicional de la factura
/// T-040: SPRINT 3 - DÍA 4
/// 
/// Permite agregar hasta 15 campos adicionales personalizados a la factura
/// Ejemplos: Email, Teléfono, Dirección de entrega, Observaciones, etc.
/// </summary>
[XmlRoot("infoAdicional")]
public class InfoAdicional
{
    /// <summary>
    /// Lista de campos adicionales
    /// Máximo 15 campos
    /// </summary>
    [XmlElement("campoAdicional")]
    public List<CampoAdicional> Campos { get; set; } = new();

    /// <summary>
    /// Constructor por defecto
    /// </summary>
    public InfoAdicional()
    {
        Campos = new List<CampoAdicional>();
    }

    /// <summary>
    /// Agrega un campo adicional si no existe
    /// </summary>
    /// <param name="nombre">Nombre del campo</param>
    /// <param name="valor">Valor del campo</param>
    public void AgregarCampo(string nombre, string valor)
    {
        if (Campos.Count >= 15)
        {
            throw new InvalidOperationException("No se pueden agregar más de 15 campos adicionales");
        }

        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("El nombre del campo no puede estar vacío", nameof(nombre));
        }

        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ArgumentException("El valor del campo no puede estar vacío", nameof(valor));
        }

        // Verificar que no exista ya un campo con el mismo nombre
        if (Campos.Any(c => c.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Ya existe un campo con el nombre '{nombre}'");
        }

        Campos.Add(new CampoAdicional
        {
            Nombre = nombre,
            Valor = valor
        });
    }

    /// <summary>
    /// Obtiene el valor de un campo por su nombre
    /// </summary>
    /// <param name="nombre">Nombre del campo</param>
    /// <returns>Valor del campo o null si no existe</returns>
    public string? ObtenerValor(string nombre)
    {
        return Campos.FirstOrDefault(c => 
            c.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase))?.Valor;
    }

    /// <summary>
    /// Elimina un campo por su nombre
    /// </summary>
    /// <param name="nombre">Nombre del campo a eliminar</param>
    /// <returns>True si se eliminó, False si no existía</returns>
    public bool EliminarCampo(string nombre)
    {
        var campo = Campos.FirstOrDefault(c => 
            c.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase));

        if (campo != null)
        {
            Campos.Remove(campo);
            return true;
        }

        return false;
    }
}

/// <summary>
/// Modelo para un campo adicional individual
/// </summary>
public class CampoAdicional
{
    /// <summary>
    /// Nombre del campo adicional
    /// Máximo 300 caracteres
    /// Ejemplos: "Email", "Teléfono", "Dirección de Entrega", "Observaciones"
    /// </summary>
    [XmlAttribute("nombre")]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Valor del campo adicional
    /// Este es el contenido del nodo XML (no un atributo)
    /// Máximo 300 caracteres
    /// </summary>
    [XmlText]
    public string Valor { get; set; } = string.Empty;

    /// <summary>
    /// Constructor por defecto
    /// </summary>
    public CampoAdicional()
    {
    }

    /// <summary>
    /// Constructor con parámetros
    /// </summary>
    /// <param name="nombre">Nombre del campo</param>
    /// <param name="valor">Valor del campo</param>
    public CampoAdicional(string nombre, string valor)
    {
        Nombre = nombre;
        Valor = valor;
    }

    /// <summary>
    /// Validación del campo
    /// </summary>
    /// <returns>True si es válido</returns>
    public bool EsValido()
    {
        if (string.IsNullOrWhiteSpace(Nombre) || Nombre.Length > 300)
            return false;

        if (string.IsNullOrWhiteSpace(Valor) || Valor.Length > 300)
            return false;

        return true;
    }

    public override string ToString()
    {
        return $"{Nombre}: {Valor}";
    }
}

/// <summary>
/// Clase helper para crear campos adicionales comunes
/// </summary>
public static class CamposAdicionalesHelper
{
    /// <summary>
    /// Crea un campo de email
    /// </summary>
    public static CampoAdicional CrearEmail(string email)
    {
        return new CampoAdicional("Email", email);
    }

    /// <summary>
    /// Crea un campo de teléfono
    /// </summary>
    public static CampoAdicional CrearTelefono(string telefono)
    {
        return new CampoAdicional("Teléfono", telefono);
    }

    /// <summary>
    /// Crea un campo de dirección de entrega
    /// </summary>
    public static CampoAdicional CrearDireccionEntrega(string direccion)
    {
        return new CampoAdicional("Dirección de Entrega", direccion);
    }

    /// <summary>
    /// Crea un campo de observaciones
    /// </summary>
    public static CampoAdicional CrearObservaciones(string observaciones)
    {
        return new CampoAdicional("Observaciones", observaciones);
    }

    /// <summary>
    /// Crea un campo de referencia
    /// </summary>
    public static CampoAdicional CrearReferencia(string referencia)
    {
        return new CampoAdicional("Referencia", referencia);
    }

    /// <summary>
    /// Crea un campo de vendedor
    /// </summary>
    public static CampoAdicional CrearVendedor(string vendedor)
    {
        return new CampoAdicional("Vendedor", vendedor);
    }
}