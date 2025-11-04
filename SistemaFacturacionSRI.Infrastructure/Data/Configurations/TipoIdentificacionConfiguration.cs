using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaFacturacionSRI.Domain.Entities;

namespace SistemaFacturacionSRI.Infrastructure.Data.Configurations
{
    public class TipoIdentificacionConfiguration : IEntityTypeConfiguration<TipoIdentificacion>
    {
        public void Configure(EntityTypeBuilder<TipoIdentificacion> builder)
        {
            builder.ToTable("TiposIdentificacion");
            builder.HasKey(t => t.TipoIdentificacionId);

            builder.Property(t => t.CodigoSRI).IsRequired().HasMaxLength(2).IsFixedLength();
            builder.Property(t => t.Nombre).IsRequired().HasMaxLength(100);
        }
    }
}
