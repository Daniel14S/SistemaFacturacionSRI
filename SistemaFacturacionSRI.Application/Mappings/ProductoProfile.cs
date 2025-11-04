using AutoMapper;
using SistemaFacturacionSRI.Application.DTOs.Producto;
using SistemaFacturacionSRI.Domain.Entities;
using SistemaFacturacionSRI.Domain.Enums;

namespace SistemaFacturacionSRI.Application.Mappings
{
    /// <summary>
    /// Perfil de AutoMapper para la entidad Producto.
    /// Define cómo convertir entre Producto (entidad) y sus DTOs.
    /// </summary>
    public class ProductoProfile : Profile
    {
        public ProductoProfile()
        {
            // Mapeo de Entidad → DTO (para lectura/GET)
            CreateMap<Producto, ProductoDto>()
                .ForMember(dest => dest.TipoIVADescripcion, 
                    opt => opt.MapFrom(src => src.TipoIVA.ObtenerDescripcion()))
                .ForMember(dest => dest.ValorIVA, 
                    opt => opt.MapFrom(src => src.ValorIVA))
                .ForMember(dest => dest.PrecioConIVA, 
                    opt => opt.MapFrom(src => src.PrecioConIVA))
                .ForMember(dest => dest.TieneStock, 
                    opt => opt.MapFrom(src => src.TieneStock))
                .ForMember(dest => dest.ValorInventario, 
                    opt => opt.MapFrom(src => src.ValorInventario));

            // Mapeo de CrearProductoDto → Entidad (para creación/POST)
            CreateMap<CrearProductoDto, Producto>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()) // Id se genera automáticamente
                .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore()) // Se establece en DbContext
                .ForMember(dest => dest.FechaModificacion, opt => opt.Ignore())
                .ForMember(dest => dest.Activo, opt => opt.Ignore()); // Se establece en DbContext

            // Mapeo de ActualizarProductoDto → Entidad (para actualización/PUT)
            CreateMap<ActualizarProductoDto, Producto>()
                .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore()) // No se actualiza
                .ForMember(dest => dest.FechaModificacion, opt => opt.Ignore()) // Se establece en DbContext
                .ForMember(dest => dest.Activo, opt => opt.Ignore()); // No se actualiza desde el DTO
        }
    }
}