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
        }
    }
}
