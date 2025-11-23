using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaFacturacionSRI.Domain.Entities;

namespace SistemaFacturacionSRI.Infrastructure.Data.Configurations
{
    public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
    {
        public void Configure(EntityTypeBuilder<Cliente> builder)
        {
            builder.ToTable("Clientes");
            builder.HasKey(c => c.ClienteId);

            builder.Property(c => c.Identificacion).IsRequired().HasMaxLength(20);
            builder.Property(c => c.Nombre1).IsRequired().HasMaxLength(100);
            builder.Property(c => c.Nombre2).HasMaxLength(100);
            builder.Property(c => c.Apellido1).IsRequired().HasMaxLength(100);
            builder.Property(c => c.Apellido2).HasMaxLength(100);
            builder.Property(c => c.Direccion).HasMaxLength(500);
            builder.Property(c => c.Telefono).HasMaxLength(10);
            builder.Property(c => c.Email).HasMaxLength(100);
            builder.Property(c => c.Estado).HasDefaultValue(true);

            builder.HasIndex(c => c.Identificacion).IsUnique();

            builder.HasOne(c => c.TipoIdentificacion)
                .WithMany(t => t.Clientes)
                .HasForeignKey(c => c.TipoIdentificacionId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Cliente_TipoIdentificacion");

            builder.HasData(
                new Cliente
                {
                    ClienteId = 1,
                    TipoIdentificacionId = 1,
                    Identificacion = "0102030405",
                    Nombre1 = "Juan",
                    Apellido1 = "Perez",
                    Direccion = "Av. Amazonas 123, Quito",
                    Telefono = "0991110001",
                    Email = "juan.perez@example.com",
                    Estado = true
                },
                new Cliente
                {
                    ClienteId = 2,
                    TipoIdentificacionId = 1,
                    Identificacion = "0607080910",
                    Nombre1 = "Maria",
                    Apellido1 = "Garcia",
                    Direccion = "Calle 10 de Agosto 456, Cuenca",
                    Telefono = "0991110002",
                    Email = "maria.garcia@example.com",
                    Estado = true
                },
                new Cliente
                {
                    ClienteId = 3,
                    TipoIdentificacionId = 2,
                    Identificacion = "1718192021",
                    Nombre1 = "Carlos",
                    Apellido1 = "Lopez",
                    Direccion = "Av. 9 de Octubre 789, Guayaquil",
                    Telefono = "0991110003",
                    Email = "carlos.lopez@example.com",
                    Estado = true
                },
                new Cliente
                {
                    ClienteId = 4,
                    TipoIdentificacionId = 1,
                    Identificacion = "2122232425",
                    Nombre1 = "Andrea",
                    Apellido1 = "Salazar",
                    Direccion = "Av. de los Shyris 321, Quito",
                    Telefono = "0991110004",
                    Email = "andrea.salazar@example.com",
                    Estado = true
                },
                new Cliente
                {
                    ClienteId = 5,
                    TipoIdentificacionId = 2,
                    Identificacion = "2627282930",
                    Nombre1 = "Miguel",
                    Apellido1 = "Vera",
                    Direccion = "Km 6.5 Via Daule, Guayaquil",
                    Telefono = "0991110005",
                    Email = "miguel.vera@example.com",
                    Estado = true
                },
                new Cliente
                {
                    ClienteId = 6,
                    TipoIdentificacionId = 1,
                    Identificacion = "3132333435",
                    Nombre1 = "Fernanda",
                    Apellido1 = "Ortiz",
                    Direccion = "Av. Loja 12-34, Loja",
                    Telefono = "0991110006",
                    Email = "fernanda.ortiz@example.com",
                    Estado = true
                },
                new Cliente
                {
                    ClienteId = 7,
                    TipoIdentificacionId = 2,
                    Identificacion = "3637383940",
                    Nombre1 = "Jorge",
                    Apellido1 = "Cedeno",
                    Direccion = "Malecon 2000, Guayaquil",
                    Telefono = "0991110007",
                    Email = "jorge.cedeno@example.com",
                    Estado = true
                },
                new Cliente
                {
                    ClienteId = 8,
                    TipoIdentificacionId = 1,
                    Identificacion = "4142434445",
                    Nombre1 = "Lucia",
                    Apellido1 = "Morales",
                    Direccion = "Av. Pedro Vicente Maldonado 123, Latacunga",
                    Telefono = "0991110008",
                    Email = "lucia.morales@example.com",
                    Estado = true
                },
                new Cliente
                {
                    ClienteId = 9,
                    TipoIdentificacionId = 1,
                    Identificacion = "4647484950",
                    Nombre1 = "Ricardo",
                    Apellido1 = "Suarez",
                    Direccion = "Av. Solano 987, Cuenca",
                    Telefono = "0991110009",
                    Email = "ricardo.suarez@example.com",
                    Estado = true
                },
                new Cliente
                {
                    ClienteId = 10,
                    TipoIdentificacionId = 2,
                    Identificacion = "5152535455",
                    Nombre1 = "Veronica",
                    Apellido1 = "Martinez",
                    Direccion = "Av. Esmeraldas 456, Esmeraldas",
                    Telefono = "0991110010",
                    Email = "veronica.martinez@example.com",
                    Estado = true
                },
                new Cliente
                {
                    ClienteId = 11,
                    TipoIdentificacionId = 1,
                    Identificacion = "5657585960",
                    Nombre1 = "Patricio",
                    Apellido1 = "Aguirre",
                    Direccion = "Av. 6 de Diciembre 100, Quito",
                    Telefono = "0991110011",
                    Email = "patricio.aguirre@example.com",
                    Estado = true
                },
                new Cliente
                {
                    ClienteId = 12,
                    TipoIdentificacionId = 2,
                    Identificacion = "6162636465",
                    Nombre1 = "Gabriela",
                    Apellido1 = "Reyes",
                    Direccion = "Ruta del Sol, Santa Elena",
                    Telefono = "0991110012",
                    Email = "gabriela.reyes@example.com",
                    Estado = true
                },
                new Cliente
                {
                    ClienteId = 13,
                    TipoIdentificacionId = 1,
                    Identificacion = "6667686970",
                    Nombre1 = "Hector",
                    Apellido1 = "Villalba",
                    Direccion = "Av. Universitaria 567, Ambato",
                    Telefono = "0991110013",
                    Email = "hector.villalba@example.com",
                    Estado = true
                },
                new Cliente
                {
                    ClienteId = 14,
                    TipoIdentificacionId = 1,
                    Identificacion = "7172737475",
                    Nombre1 = "Paola",
                    Apellido1 = "Correa",
                    Direccion = "Av. Gonzalez Suarez 135, Quito",
                    Telefono = "0991110014",
                    Email = "paola.correa@example.com",
                    Estado = true
                },
                new Cliente
                {
                    ClienteId = 15,
                    TipoIdentificacionId = 2,
                    Identificacion = "7677787980",
                    Nombre1 = "Diego",
                    Apellido1 = "Paredes",
                    Direccion = "Av. Amazonas y Republica, Quito",
                    Telefono = "0991110015",
                    Email = "diego.paredes@example.com",
                    Estado = true
                },
                new Cliente
                {
                    ClienteId = 16,
                    TipoIdentificacionId = 1,
                    Identificacion = "8182838485",
                    Nombre1 = "Natalia",
                    Apellido1 = "Carrillo",
                    Direccion = "Av. Bolivariana 345, Riobamba",
                    Telefono = "0991110016",
                    Email = "natalia.carrillo@example.com",
                    Estado = true
                },
                new Cliente
                {
                    ClienteId = 17,
                    TipoIdentificacionId = 2,
                    Identificacion = "8687888990",
                    Nombre1 = "Sebastian",
                    Apellido1 = "Yanez",
                    Direccion = "Av. Manabi 456, Portoviejo",
                    Telefono = "0991110017",
                    Email = "sebastian.yanez@example.com",
                    Estado = true
                },
                new Cliente
                {
                    ClienteId = 18,
                    TipoIdentificacionId = 1,
                    Identificacion = "9192939495",
                    Nombre1 = "Daniela",
                    Apellido1 = "Pico",
                    Direccion = "Av. 24 de Mayo 678, Ibarra",
                    Telefono = "0991110018",
                    Email = "daniela.pico@example.com",
                    Estado = true
                },
                new Cliente
                {
                    ClienteId = 19,
                    TipoIdentificacionId = 1,
                    Identificacion = "9697989991",
                    Nombre1 = "Gustavo",
                    Apellido1 = "Montero",
                    Direccion = "Av. Quito 123, Santo Domingo",
                    Telefono = "0991110019",
                    Email = "gustavo.montero@example.com",
                    Estado = true
                },
                new Cliente
                {
                    ClienteId = 20,
                    TipoIdentificacionId = 2,
                    Identificacion = "0192837465",
                    Nombre1 = "Silvia",
                    Apellido1 = "Penafiel",
                    Direccion = "Av. Olmedo 345, Machala",
                    Telefono = "0991110020",
                    Email = "silvia.penafiel@example.com",
                    Estado = true
                }
            );
        }
    }
}
