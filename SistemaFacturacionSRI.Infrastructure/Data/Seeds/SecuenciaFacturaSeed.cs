using Microsoft.EntityFrameworkCore;
using SistemaFacturacionSRI.Domain.Entities;

namespace SistemaFacturacionSRI.Infrastructure.Data.Seeds
{
    public static class SecuenciaFacturaSeed
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SecuenciaFactura>().HasData(
                new SecuenciaFactura
                {
                    Id = 1,
                    Establecimiento = "001",
                    PuntoEmision = "001",
                    SecuenciaActual = 0, // La primera factura será 000000001
                    FechaUltimaEmision = null,
                    Activo = true
                }
            );
        }
    }
}