using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaFacturacionSRI.Domain.Entities;
<<<<<<< HEAD

namespace SistemaFacturacionSRI.Infrastructure.Data.Configurations;

public class ConfiguracionEmpresaConfiguration : IEntityTypeConfiguration<ConfiguracionEmpresa>
{
    public void Configure(EntityTypeBuilder<ConfiguracionEmpresa> builder)
    {
        // Nombre de la tabla
        builder.ToTable("ConfiguracionEmpresa");

        // Clave primaria
        builder.HasKey(c => c.Id);

        // ==================== DATOS DE LA EMPRESA ====================
        
        builder.Property(c => c.RUC)
            .IsRequired()
            .HasMaxLength(13)
            .HasComment("RUC de la empresa emisora (13 dígitos)");

        builder.Property(c => c.RazonSocial)
            .IsRequired()
            .HasMaxLength(300)
            .HasComment("Razón social de la empresa");

        builder.Property(c => c.NombreComercial)
            .IsRequired()
            .HasMaxLength(300)
            .HasComment("Nombre comercial de la empresa");

        builder.Property(c => c.DirMatriz)
            .IsRequired()
            .HasMaxLength(300)
            .HasComment("Dirección de la matriz");

        builder.Property(c => c.DirEstablecimiento)
            .IsRequired()
            .HasMaxLength(300)
            .HasComment("Dirección del establecimiento");

        builder.Property(c => c.CodigoEstablecimiento)
            .IsRequired()
            .HasMaxLength(3)
            .IsFixedLength()
            .HasComment("Código del establecimiento (ej: 001)");

        builder.Property(c => c.PuntoEmision)
            .IsRequired()
            .HasMaxLength(3)
            .IsFixedLength()
            .HasComment("Punto de emisión (ej: 001)");

        builder.Property(c => c.ObligadoContabilidad)
            .IsRequired()
            .HasComment("Indica si está obligado a llevar contabilidad");

        builder.Property(c => c.AgenteRetencion)
            .HasMaxLength(10)
            .HasComment("Resolución de agente de retención");

        // ==================== DATOS DE CONTACTO ====================

        builder.Property(c => c.Telefono)
            .IsRequired()
            .HasMaxLength(20)
            .HasComment("Teléfono de contacto");

        builder.Property(c => c.Email)
            .IsRequired()
            .HasMaxLength(100)
            .HasComment("Email de contacto");

        // ==================== CONFIGURACIÓN CERTIFICADO DIGITAL ====================

        builder.Property(c => c.RutaCertificadoDigital)
            .HasMaxLength(500)
            .HasComment("Ruta del archivo del certificado digital (.p12)");

        builder.Property(c => c.ClaveCertificadoDigital)
            .HasMaxLength(500)
            .HasComment("Contraseña del certificado (encriptada)");

        // ==================== CONFIGURACIÓN SRI ====================

        builder.Property(c => c.AmbienteSRI)
            .IsRequired()
            .HasMaxLength(1)
            .HasDefaultValue("1")
            .HasComment("1=Pruebas, 2=Producción");

        builder.Property(c => c.TipoEmision)
            .IsRequired()
            .HasMaxLength(1)
            .HasDefaultValue("1")
            .HasComment("1=Normal, 2=Indisponibilidad");

        builder.Property(c => c.UrlRecepcionComprobantes)
            .IsRequired()
            .HasMaxLength(500)
            .HasComment("URL WebService Recepción SRI");

        builder.Property(c => c.UrlAutorizacionComprobantes)
            .IsRequired()
            .HasMaxLength(500)
            .HasComment("URL WebService Autorización SRI");

        // ==================== CONFIGURACIÓN VISUAL ====================

        builder.Property(c => c.LogoPath)
            .HasMaxLength(500)
            .HasComment("Ruta del logo para RIDE");

        builder.Property(c => c.InfoAdicionalDefecto)
            .HasMaxLength(1000)
            .HasComment("Información adicional por defecto");

        // ==================== ÍNDICES ====================

        // RUC único (solo debe haber una configuración de empresa)
        builder.HasIndex(c => c.RUC)
            .IsUnique()
            .HasDatabaseName("IX_ConfiguracionEmpresa_RUC");

        // ==================== AUDITORÍA ====================

        builder.Property(c => c.FechaCreacion)
            .IsRequired()
            .HasComment("Fecha de creación del registro");

        builder.Property(c => c.FechaModificacion)
            .IsRequired()
            .HasComment("Fecha de última modificación");

        builder.Property(c => c.Activo)
            .IsRequired()
            .HasDefaultValue(true)
            .HasComment("Indica si el registro está activo");
    }
}
