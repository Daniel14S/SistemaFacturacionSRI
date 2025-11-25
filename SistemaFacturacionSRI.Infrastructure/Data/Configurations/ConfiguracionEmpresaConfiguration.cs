using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaFacturacionSRI.Domain.Entities;
using SistemaFacturacionSRI.Domain.Enums;

namespace SistemaFacturacionSRI.Infrastructure.Data.Configurations
{
    public class ConfiguracionEmpresaConfiguration : IEntityTypeConfiguration<ConfiguracionEmpresa>
    {
        public void Configure(EntityTypeBuilder<ConfiguracionEmpresa> builder)
        {
            builder.ToTable("ConfiguracionesEmpresa");
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Ruc)
                .IsRequired()
                .HasMaxLength(13);

            builder.Property(c => c.RazonSocial)
                .IsRequired()
                .HasMaxLength(300);

            builder.Property(c => c.NombreComercial)
                .HasMaxLength(300);

            builder.Property(c => c.DirMatriz)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(c => c.DirEstablecimiento)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(c => c.ContribuyenteEspecial)
                .HasMaxLength(50);

            builder.Property(c => c.Establecimiento)
                .IsRequired()
                .HasMaxLength(3);

            builder.Property(c => c.PuntoEmision)
                .IsRequired()
                .HasMaxLength(3);

            builder.Property(c => c.AmbienteSRI)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(Ambiente.PRUEBAS);

            builder.Property(c => c.TipoEmision)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(TipoEmision.NORMAL);

            builder.Property(c => c.RutaCertificado)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(c => c.ClaveCertificado)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(c => c.Logo)
                .HasColumnType("VARBINARY(MAX)");

            builder.HasIndex(c => c.Ruc)
                .IsUnique();

            builder.HasIndex(c => new { c.Establecimiento, c.PuntoEmision })
                .IsUnique();
        }
    }
}
