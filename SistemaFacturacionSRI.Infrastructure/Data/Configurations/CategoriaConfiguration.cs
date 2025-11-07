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

            // Seed de categorías de ejemplo (al menos 10)
            builder.HasData(
                new Categoria { CategoriaId = 1, Nombre = "General", Descripcion = "Categoría por defecto" },
                new Categoria { CategoriaId = 2, Nombre = "Lacteos", Descripcion = "Productos lácteos" },
                new Categoria { CategoriaId = 3, Nombre = "Embutidos", Descripcion = "Carnes procesadas y embutidos" },
                new Categoria { CategoriaId = 4, Nombre = "Refrigerados", Descripcion = "Productos de cadena de frío" },
                new Categoria { CategoriaId = 5, Nombre = "Electronicos", Descripcion = "Dispositivos y accesorios electrónicos" },
                new Categoria { CategoriaId = 6, Nombre = "Bebidas", Descripcion = "Bebidas alcohólicas y no alcohólicas" },
                new Categoria { CategoriaId = 7, Nombre = "Abarrotes", Descripcion = "Despensa y abarrotes" },
                new Categoria { CategoriaId = 8, Nombre = "Limpieza", Descripcion = "Productos de limpieza" },
                new Categoria { CategoriaId = 9, Nombre = "Higiene", Descripcion = "Cuidado personal e higiene" },
                new Categoria { CategoriaId = 10, Nombre = "Panaderia", Descripcion = "Panes y repostería" },
                new Categoria { CategoriaId = 11, Nombre = "Frutas y Verduras", Descripcion = "Productos frescos" }
            );
        }
    }
}
