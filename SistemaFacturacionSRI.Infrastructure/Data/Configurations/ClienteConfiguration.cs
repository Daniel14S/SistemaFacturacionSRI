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
            builder.Property(c => c.Nombres).IsRequired().HasMaxLength(150);
            builder.Property(c => c.Apellidos).IsRequired().HasMaxLength(150);
            builder.Property(c => c.Direccion).HasMaxLength(500);
            builder.Property(c => c.Telefono).HasMaxLength(20);
            builder.Property(c => c.Email).HasMaxLength(100);

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
                    Nombres = "Juan",
                    Apellidos = "Pérez",
                    Direccion = "Av. Amazonas 123, Quito",
                    Telefono = "+593999999999",
                    Email = "juan.perez@example.com"
                },
                new Cliente
                {
                    ClienteId = 2,
                    TipoIdentificacionId = 1,
                    Identificacion = "0607080910",
                    Nombres = "María",
                    Apellidos = "García",
                    Direccion = "Calle 10 de Agosto 456, Cuenca",
                    Telefono = "+593988888888",
                    Email = "maria.garcia@example.com"
                },
                new Cliente
                {
                    ClienteId = 3,
                    TipoIdentificacionId = 2,
                    Identificacion = "1718192021",
                    Nombres = "Carlos",
                    Apellidos = "Lopez",
                    Direccion = "Av. 9 de Octubre 789, Guayaquil",
                    Telefono = "+593977777777",
                    Email = "carlos.lopez@example.com"
                },
                new Cliente
                {
                    ClienteId = 4,
                    TipoIdentificacionId = 1,
                    Identificacion = "2122232425",
                    Nombres = "Andrea",
                    Apellidos = "Salazar",
                    Direccion = "Av. de los Shyris 321, Quito",
                    Telefono = "+593966666666",
                    Email = "andrea.salazar@example.com"
                },
                new Cliente
                {
                    ClienteId = 5,
                    TipoIdentificacionId = 2,
                    Identificacion = "2627282930",
                    Nombres = "Miguel",
                    Apellidos = "Vera",
                    Direccion = "Km 6.5 Via Daule, Guayaquil",
                    Telefono = "+593955555555",
                    Email = "miguel.vera@example.com"
                },
                new Cliente
                {
                    ClienteId = 6,
                    TipoIdentificacionId = 1,
                    Identificacion = "3132333435",
                    Nombres = "Fernanda",
                    Apellidos = "Ortiz",
                    Direccion = "Av. Loja 12-34, Loja",
                    Telefono = "+593944444444",
                    Email = "fernanda.ortiz@example.com"
                },
                new Cliente
                {
                    ClienteId = 7,
                    TipoIdentificacionId = 2,
                    Identificacion = "3637383940",
                    Nombres = "Jorge",
                    Apellidos = "Cedeño",
                    Direccion = "Malecón 2000, Guayaquil",
                    Telefono = "+593933333333",
                    Email = "jorge.cedeno@example.com"
                },
                new Cliente
                {
                    ClienteId = 8,
                    TipoIdentificacionId = 1,
                    Identificacion = "4142434445",
                    Nombres = "Lucía",
                    Apellidos = "Morales",
                    Direccion = "Av. Pedro Vicente Maldonado 123, Latacunga",
                    Telefono = "+593922222222",
                    Email = "lucia.morales@example.com"
                },
                new Cliente
                {
                    ClienteId = 9,
                    TipoIdentificacionId = 1,
                    Identificacion = "4647484950",
                    Nombres = "Ricardo",
                    Apellidos = "Suárez",
                    Direccion = "Av. Solano 987, Cuenca",
                    Telefono = "+593911111111",
                    Email = "ricardo.suarez@example.com"
                },
                new Cliente
                {
                    ClienteId = 10,
                    TipoIdentificacionId = 2,
                    Identificacion = "5152535455",
                    Nombres = "Verónica",
                    Apellidos = "Martínez",
                    Direccion = "Av. Esmeraldas 456, Esmeraldas",
                    Telefono = "+593900000000",
                    Email = "veronica.martinez@example.com"
                },
                new Cliente
                {
                    ClienteId = 11,
                    TipoIdentificacionId = 1,
                    Identificacion = "5657585960",
                    Nombres = "Patricio",
                    Apellidos = "Aguirre",
                    Direccion = "Av. 6 de Diciembre 100, Quito",
                    Telefono = "+593989898989",
                    Email = "patricio.aguirre@example.com"
                },
                new Cliente
                {
                    ClienteId = 12,
                    TipoIdentificacionId = 2,
                    Identificacion = "6162636465",
                    Nombres = "Gabriela",
                    Apellidos = "Reyes",
                    Direccion = "Ruta del Sol, Santa Elena",
                    Telefono = "+593979797979",
                    Email = "gabriela.reyes@example.com"
                },
                new Cliente
                {
                    ClienteId = 13,
                    TipoIdentificacionId = 1,
                    Identificacion = "6667686970",
                    Nombres = "Héctor",
                    Apellidos = "Villalba",
                    Direccion = "Av. Universitaria 567, Ambato",
                    Telefono = "+593969696969",
                    Email = "hector.villalba@example.com"
                },
                new Cliente
                {
                    ClienteId = 14,
                    TipoIdentificacionId = 1,
                    Identificacion = "7172737475",
                    Nombres = "Paola",
                    Apellidos = "Correa",
                    Direccion = "Av. González Suárez 135, Quito",
                    Telefono = "+593959595959",
                    Email = "paola.correa@example.com"
                },
                new Cliente
                {
                    ClienteId = 15,
                    TipoIdentificacionId = 2,
                    Identificacion = "7677787980",
                    Nombres = "Diego",
                    Apellidos = "Paredes",
                    Direccion = "Av. Amazonas y República, Quito",
                    Telefono = "+593949494949",
                    Email = "diego.paredes@example.com"
                },
                new Cliente
                {
                    ClienteId = 16,
                    TipoIdentificacionId = 1,
                    Identificacion = "8182838485",
                    Nombres = "Natalia",
                    Apellidos = "Carrillo",
                    Direccion = "Av. Bolivariana 345, Riobamba",
                    Telefono = "+593939393939",
                    Email = "natalia.carrillo@example.com"
                },
                new Cliente
                {
                    ClienteId = 17,
                    TipoIdentificacionId = 2,
                    Identificacion = "8687888990",
                    Nombres = "Sebastián",
                    Apellidos = "Yánez",
                    Direccion = "Av. Manabí 456, Portoviejo",
                    Telefono = "+593929292929",
                    Email = "sebastian.yanez@example.com"
                },
                new Cliente
                {
                    ClienteId = 18,
                    TipoIdentificacionId = 1,
                    Identificacion = "9192939495",
                    Nombres = "Daniela",
                    Apellidos = "Pico",
                    Direccion = "Av. 24 de Mayo 678, Ibarra",
                    Telefono = "+593919191919",
                    Email = "daniela.pico@example.com"
                },
                new Cliente
                {
                    ClienteId = 19,
                    TipoIdentificacionId = 1,
                    Identificacion = "9697989991",
                    Nombres = "Gustavo",
                    Apellidos = "Montero",
                    Direccion = "Av. Quito 123, Santo Domingo",
                    Telefono = "+593909090909",
                    Email = "gustavo.montero@example.com"
                },
                new Cliente
                {
                    ClienteId = 20,
                    TipoIdentificacionId = 2,
                    Identificacion = "0192837465",
                    Nombres = "Silvia",
                    Apellidos = "Peñafiel",
                    Direccion = "Av. Olmedo 345, Machala",
                    Telefono = "+593998877665",
                    Email = "silvia.penafiel@example.com"
                }
            );
        }
    }
}
