namespace SistemaFacturacionSRI.Domain.Configuration;

/// <summary>
/// Opciones de configuración para Resend (API de correo).
/// </summary>
public class ResendOptions
{
    public const string SectionName = "Resend";

    /// <summary>
    /// API Key de Resend (Bearer token).
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Dirección remitente, ej: "Facturación <no-reply@midominio.com>".
    /// </summary>
    public string From { get; set; } = string.Empty;
}
