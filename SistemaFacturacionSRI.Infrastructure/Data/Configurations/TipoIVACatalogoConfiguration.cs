using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaFacturacionSRI.Domain.Entities;

namespace SistemaFacturacionSRI.Infrastructure.Data.Configurations
{
    public class TipoIVACatalogoConfiguration : IEntityTypeConfiguration<TipoIVACatalogo>
    {
        public void Configure(EntityTypeBuilder<TipoIVACatalogo> builder)
        {
            builder.ToTable("TiposIVA");
            builder.HasKey(t => t.TipoIVAId);

            builder.Property(t => t.CodigoSRI).IsRequired().HasMaxLength(1).IsFixedLength();
            builder.Property(t => t.Descripcion).IsRequired().HasMaxLength(100);
            builder.Property(t => t.Porcentaje).IsRequired().HasColumnType("DECIMAL(5,2)");

            // Seed básico (asumiendo códigos SRI: 0=0%, 2=12%, 3=15%).
            builder.HasData(
                new TipoIVACatalogo { TipoIVAId = 1, CodigoSRI = "0", Descripcion = "IVA 0%", Porcentaje = 0m },
                new TipoIVACatalogo { TipoIVAId = 2, CodigoSRI = "2", Descripcion = "IVA 12%", Porcentaje = 12m },
                new TipoIVACatalogo { TipoIVAId = 3, CodigoSRI = "3", Descripcion = "IVA 15%", Porcentaje = 15m }
            );
        }
    }
}
