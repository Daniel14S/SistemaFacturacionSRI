// SistemaFacturacionSRI.WebUI/Services/IConfiguracionHttpService.cs
// T-131: Service HTTP para gestión de configuración empresarial

using SistemaFacturacionSRI.Domain.DTOs.Configuracion;

namespace SistemaFacturacionSRI.WebUI.Services;

/// <summary>
/// Servicio HTTP para consumir la API de configuración empresarial
/// Solo accesible por Administradores
/// </summary>
public interface IConfiguracionHttpService
{
    /// <summary>
    /// Obtiene la configuración actual de la empresa
    /// GET /api/configuracion
    /// </summary>
    Task<ConfiguracionEmpresaDto?> ObtenerConfiguracionAsync();

    /// <summary>
    /// Actualiza la configuración de la empresa
    /// PUT /api/configuracion
    /// </summary>
    Task<ConfiguracionEmpresaDto?> ActualizarConfiguracionAsync(ActualizarConfiguracionDto dto);

    /// <summary>
    /// Valida si el certificado digital es válido
    /// GET /api/configuracion/certificado/validar
    /// </summary>
    Task<(bool EsValido, string Mensaje)> ValidarCertificadoAsync();

    /// <summary>
    /// Obtiene información del certificado digital actual
    /// GET /api/configuracion/certificado/info
    /// </summary>
    Task<InfoCertificadoDto?> ObtenerInfoCertificadoAsync();

    /// <summary>
    /// Cambia el ambiente del SRI (1=Pruebas, 2=Producción)
    /// PATCH /api/configuracion/ambiente
    /// </summary>
    Task<bool> CambiarAmbienteSRIAsync(string nuevoAmbiente);

    /// <summary>
    /// Verifica si la configuración está completa para emitir facturas
    /// GET /api/configuracion/validar-completa
    /// </summary>
    Task<(bool EstaCompleta, List<string> CamposFaltantes)> ValidarConfiguracionCompletaAsync();

    /// <summary>
    /// Sube el logo de la empresa (futuro)
    /// POST /api/configuracion/logo
    /// </summary>
    Task<string?> SubirLogoAsync(byte[] archivoBytes, string nombreArchivo);

    /// <summary>
    /// Sube el certificado digital .p12 (futuro)
    /// POST /api/configuracion/certificado
    /// </summary>
    Task<bool> SubirCertificadoAsync(byte[] archivoBytes, string nombreArchivo, string clave);
}