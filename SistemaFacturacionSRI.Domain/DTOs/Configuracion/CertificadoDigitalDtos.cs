using System.ComponentModel.DataAnnotations;

namespace SistemaFacturacionSRI.Domain.DTOs.Configuracion;

/// <summary>
/// Payload para guardar el certificado en la base de datos.
/// </summary>
public class GuardarCertificadoRequest
{
    [Required]
    public string NombreArchivo { get; set; } = string.Empty;

    [Required]
    public byte[] ArchivoBytes { get; set; } = Array.Empty<byte>();

    [Required]
    public string Clave { get; set; } = string.Empty;

    public string Tipo { get; set; } = "PRUEBAS";

    public string? Notas { get; set; }
}

/// <summary>
/// Información del certificado almacenado (sin exponer la clave).
/// </summary>
public class CertificadoDigitalActivoDto
{
    public int Id { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
    public string Tipo { get; set; } = "PRUEBAS";
    public long TamanoBytes { get; set; }
    public DateTime? FechaExpiracion { get; set; }
    public DateTime FechaCreacion { get; set; }
    public bool EsActivo { get; set; }
    public byte[] ArchivoBytes { get; set; } = Array.Empty<byte>();
    public string ClavePlano { get; set; } = string.Empty;
}
