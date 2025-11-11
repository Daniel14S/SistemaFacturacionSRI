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

            builder.Property(t => t.Descripcion).IsRequired().HasMaxLength(100);
            builder.Property(t => t.Porcentaje).IsRequired().HasColumnType("DECIMAL(5,2)");

            builder.HasData(
                new TipoIVACatalogo { TipoIVAId = 1, Descripcion = "IVA 0%", Porcentaje = 0m },
                new TipoIVACatalogo { TipoIVAId = 2, Descripcion = "IVA 5%", Porcentaje = 5m },
                new TipoIVACatalogo { TipoIVAId = 3, Descripcion = "IVA 8%", Porcentaje = 8m },
                new TipoIVACatalogo { TipoIVAId = 6, Descripcion = "IVA 15%", Porcentaje = 15m }
            );
        }
    }
}
