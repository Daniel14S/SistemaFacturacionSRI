using Microsoft.EntityFrameworkCore;
using SistemaFacturacionSRI.Domain.Entities;

namespace SistemaFacturacionSRI.Infrastructure.Data.Seeds;

public static class ConfiguracionEmpresaSeed
{
    public static void Seed(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConfiguracionEmpresa>().HasData(
            new ConfiguracionEmpresa
            {
                Id = 1,
                RUC = "1804183794001",
                RazonSocial = "EMPRESA DEMO FACTURACIÓN ELECTRÓNICA S.A.",
                NombreComercial = "DEMO FACTURACIÓN",
                DirMatriz = "Av. Principal 123 y Secundaria, Edificio Central",
                DirEstablecimiento = "Av. Principal 123 y Secundaria, Local 001",
                CodigoEstablecimiento = "001",
                PuntoEmision = "001",
                ObligadoContabilidad = true,
                AgenteRetencion = "1",
                
                // Contacto
                Telefono = "03-2345678",
                Email = "facturacion@empresademo.com",
                
                // Configuración firma digital
                RutaCertificadoDigital = null, // Se configura después
                ClaveCertificadoDigital = null, // Encriptada
                
                // Configuración SRI
                AmbienteSRI = "1", // 1 = Pruebas, 2 = Producción
                TipoEmision = "1", // 1 = Normal, 2 = Contingencia
                
                // URLs WebService SRI (Ambiente de pruebas)
                UrlRecepcionComprobantes = "https://celportal.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline?wsdl",
                UrlAutorizacionComprobantes = "https://celportal.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline?wsdl",
                
                // Logo
                LogoPath = null, // Se sube después
                
                // Información adicional por defecto
                InfoAdicionalDefecto = "Gracias por su compra|Términos y condiciones: www.empresademo.com/terminos",
                
                // ✅ CORREGIDO: Usar fecha estática en lugar de DateTime.UtcNow
                FechaCreacion = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                FechaModificacion = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}