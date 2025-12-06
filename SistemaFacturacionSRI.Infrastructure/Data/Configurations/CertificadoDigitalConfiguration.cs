using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaFacturacionSRI.Domain.Entities;

namespace SistemaFacturacionSRI.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Configuración EF Core para la tabla de certificados digitales.
    /// </summary>
    public class CertificadoDigitalConfiguration : IEntityTypeConfiguration<CertificadoDigital>
    {
        public void Configure(EntityTypeBuilder<CertificadoDigital> builder)
        {
            builder.ToTable("CertificadosDigitales");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.NombreArchivo)
                .IsRequired()
                .HasMaxLength(260);

            builder.Property(c => c.ArchivoEncriptado)
                .IsRequired();

            builder.Property(c => c.ClaveEncriptada)
                .IsRequired();

            builder.Property(c => c.HashSha256)
                .IsRequired()
                .HasMaxLength(64)
                .IsUnicode(false);

            builder.Property(c => c.TamanoBytes)
                .IsRequired();

            builder.Property(c => c.Tipo)
                .HasMaxLength(20)
                .HasDefaultValue("PRUEBAS");

            builder.Property(c => c.FechaExpiracion)
                .HasColumnType("datetime2");

            builder.Property(c => c.EsActivo)
                .HasDefaultValue(true);

            builder.Property(c => c.Notas)
                .HasMaxLength(500);

            builder.Property(c => c.FechaCreacion)
                .HasDefaultValueSql("GETDATE()");

            builder.Property(c => c.Activo)
                .HasDefaultValue(true);

            builder.HasIndex(c => new { c.EsActivo, c.Tipo })
                .HasDatabaseName("IX_CertificadosDigitales_ActivosPorTipo");
        }
    }
}
