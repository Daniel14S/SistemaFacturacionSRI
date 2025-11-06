using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaFacturacionSRI.Domain.Entities;

namespace SistemaFacturacionSRI.Infrastructure.Data.Configurations
{
    public class CategoriaConfiguration : IEntityTypeConfiguration<Categoria>
    {
        public void Configure(EntityTypeBuilder<Categoria> builder)
        {
            builder.ToTable("Categorias");
            builder.HasKey(c => c.CategoriaId);

            builder.Property(c => c.Nombre).IsRequired().HasMaxLength(100);
            builder.Property(c => c.Descripcion).HasMaxLength(250);

            // Seed categoría genérica si aún no existe.
            builder.HasData(new Categoria { CategoriaId = 1, Nombre = "General", Descripcion = "Categoría por defecto" });
        }
    }
}
