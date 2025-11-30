using SistemaFacturacionSRI.Domain.DTOs.Configuracion;

namespace SistemaFacturacionSRI.Domain.Interfaces.Services;

/// <summary>
/// Interfaz para el servicio de gestión de configuración empresarial
/// T-016: SPRINT 3 - DÍA 4
/// 
/// Maneja la configuración única de la empresa para facturación electrónica
/// Solo debe existir UN registro de configuración en el sistema
/// </summary>
public interface IConfiguracionService
{
    /// <summary>
    /// Obtiene la configuración actual de la empresa
    /// Si no existe, debe crearla con valores por defecto
    /// </summary>
    /// <returns>DTO con la configuración de la empresa</returns>
    Task<ConfiguracionEmpresaDto> ObtenerConfiguracionAsync();

    /// <summary>
    /// Actualiza la configuración de la empresa
    /// Solo usuarios con rol Admin pueden ejecutar esta operación
    /// </summary>
    /// <param name="dto">DTO con los datos a actualizar</param>
    /// <returns>DTO con la configuración actualizada</returns>
    Task<ConfiguracionEmpresaDto> ActualizarConfiguracionAsync(ActualizarConfiguracionDto dto);

    /// <summary>
    /// Actualiza solo el certificado digital
    /// </summary>
    /// <param name="rutaCertificado">Ruta física del certificado .p12</param>
    /// <param name="claveCertificado">Contraseña del certificado (se encriptará)</param>
    /// <returns>True si se actualizó correctamente</returns>
    Task<bool> ActualizarCertificadoDigitalAsync(string rutaCertificado, string claveCertificado);

    /// <summary>
    /// Actualiza solo el logo de la empresa
    /// </summary>
    /// <param name="rutaLogo">Ruta física del logo</param>
    /// <returns>True si se actualizó correctamente</returns>
    Task<bool> ActualizarLogoAsync(string rutaLogo);

    /// <summary>
    /// Valida que el certificado digital sea válido y no haya expirado
    /// </summary>
    /// <returns>True si el certificado es válido</returns>
    Task<bool> ValidarCertificadoDigitalAsync();

    /// <summary>
    /// Obtiene información del certificado digital actual
    /// </summary>
    /// <returns>DTO con información del certificado (titular, vigencia, etc.)</returns>
    Task<InfoCertificadoDto?> ObtenerInfoCertificadoAsync();

    /// <summary>
    /// Cambia el ambiente del SRI (Pruebas/Producción)
    /// Esta operación es crítica y debe registrarse en el log
    /// </summary>
    /// <param name="nuevoAmbiente">1 = Pruebas, 2 = Producción</param>
    /// <returns>True si se cambió correctamente</returns>
    Task<bool> CambiarAmbienteSRIAsync(string nuevoAmbiente);

    /// <summary>
    /// Verifica si la configuración está completa y lista para emitir facturas
    /// </summary>
    /// <returns>True si está lista, False si falta información</returns>
    Task<(bool EstaCompleta, List<string> CamposFaltantes)> ValidarConfiguracionCompletaAsync();
}