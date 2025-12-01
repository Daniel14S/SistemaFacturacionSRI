using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaFacturacionSRI.Domain.Entities;

namespace SistemaFacturacionSRI.Infrastructure.Data.Configurations
{
    public class SecuenciaFacturaConfiguration : IEntityTypeConfiguration<SecuenciaFactura>
    {
        public void Configure(EntityTypeBuilder<SecuenciaFactura> builder)
        {
            builder.ToTable("SecuenciasFactura");
            builder.HasKey(s => s.Id);

            builder.Property(s => s.Establecimiento)
                .IsRequired()
                .HasMaxLength(3);

            builder.Property(s => s.PuntoEmision)
                .IsRequired()
                .HasMaxLength(3);

            builder.Property(s => s.SecuenciaActual)
                .HasColumnType("BIGINT");

            builder.Property(s => s.FechaUltimaEmision)
                .HasColumnType("DATETIME2");

            builder.Property(s => s.Activo)
                .HasDefaultValue(true);

            builder.HasIndex(s => new { s.Establecimiento, s.PuntoEmision })
                .IsUnique();
        }
    }
}
