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
            builder.HasKey(c => c.Id);

            // Configuración del campo Codigo
            builder.Property(c => c.Codigo)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("NVARCHAR");
                
            builder.HasIndex(c => c.Codigo)
                .IsUnique()                      
                .HasDatabaseName("IX_Categorias_Codigo");

            builder.Property(c => c.Nombre).IsRequired().HasMaxLength(100).HasColumnType("NVARCHAR");
            builder.Property(c => c.Descripcion).HasMaxLength(250).HasColumnType("NVARCHAR");

            // Seed de categorías de ejemplo (al menos 10)
            builder.HasData(
                new Categoria { Id = 1, Nombre = "General", Descripcion = "Categoría por defecto" },
                new Categoria { Id = 2, Nombre = "Lacteos", Descripcion = "Productos lácteos" },
                new Categoria { Id = 3, Nombre = "Embutidos", Descripcion = "Carnes procesadas y embutidos" },
                new Categoria { Id = 4, Nombre = "Refrigerados", Descripcion = "Productos de cadena de frío" },
                new Categoria { Id = 5, Nombre = "Electronicos", Descripcion = "Dispositivos y accesorios electrónicos" },
                new Categoria { Id = 6, Nombre = "Bebidas", Descripcion = "Bebidas alcohólicas y no alcohólicas" },
                new Categoria { Id = 7, Nombre = "Abarrotes", Descripcion = "Despensa y abarrotes" },
                new Categoria { Id = 8, Nombre = "Limpieza", Descripcion = "Productos de limpieza" },
                new Categoria { Id = 9, Nombre = "Higiene", Descripcion = "Cuidado personal e higiene" },
                new Categoria { Id = 10, Nombre = "Panaderia", Descripcion = "Panes y repostería" },
                new Categoria { Id = 11, Nombre = "Frutas y Verduras", Descripcion = "Productos frescos" }
            );

            builder.HasMany(c => c.Productos)
                .WithOne(p => p.Categoria)       
                .HasForeignKey(p => p.CategoriaId)  
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
