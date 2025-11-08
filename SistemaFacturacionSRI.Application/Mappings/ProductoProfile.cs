using AutoMapper;
using SistemaFacturacionSRI.Application.DTOs.Producto;
using SistemaFacturacionSRI.Application.DTOs.Categoria;
using SistemaFacturacionSRI.Application.DTOs.TipoIVA;
using SistemaFacturacionSRI.Domain.Entities;

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
            // Categoria -> CategoriaDto
            CreateMap<Categoria, CategoriaDto>();
            // TipoIVACatalogo -> TipoIVADto
            CreateMap<TipoIVACatalogo, TipoIVADto>();

            // Mapeo de Entidad → DTO (para lectura/GET)
            CreateMap<Producto, ProductoDto>()
                .ForMember(dest => dest.TipoIVADescripcion,
                    opt => opt.MapFrom(src => src.TipoIVACatalogo != null ? src.TipoIVACatalogo.Descripcion : string.Empty))
                .ForMember(dest => dest.TipoIVAId,
                    opt => opt.MapFrom(src => src.TipoIVAId))
                .ForMember(dest => dest.PorcentajeIVA,
        opt => opt.MapFrom(src => src.TipoIVACatalogo != null ? src.TipoIVACatalogo.Porcentaje / 100m : 0m))
                .ForMember(dest => dest.CategoriaId, opt => opt.MapFrom(src => src.CategoriaId))
                .ForMember(dest => dest.CategoriaNombre, opt => opt.MapFrom(src => src.Categoria != null ? src.Categoria.Nombre : string.Empty))
                .ForMember(dest => dest.Precio,
                    opt => opt.MapFrom(src => src.PrecioActual))
                .ForMember(dest => dest.Stock,
                    opt => opt.MapFrom(src => src.StockDisponible))
                .ForMember(dest => dest.TieneStock,
                    opt => opt.MapFrom(src => src.TieneStock))
                .ForMember(dest => dest.ValorIVA,
                    opt => opt.MapFrom(src => src.ValorIVA))
                .ForMember(dest => dest.PrecioConIVA,
                    opt => opt.MapFrom(src => src.PrecioConIVA))
                .ForMember(dest => dest.ValorInventario,
                    opt => opt.MapFrom(src => src.ValorInventario));

            // Mapeo de CrearProductoDto → Entidad (para creación/POST)
            CreateMap<CrearProductoDto, Producto>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()) // Id se genera automáticamente
                .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore()) // Se establece en DbContext
                .ForMember(dest => dest.FechaModificacion, opt => opt.Ignore())
                .ForMember(dest => dest.Activo, opt => opt.Ignore()) // Se establece en DbContext
                .ForMember(dest => dest.Lotes, opt => opt.Ignore());

            // Mapeo de ActualizarProductoDto → Entidad (para actualización/PUT)
            CreateMap<ActualizarProductoDto, Producto>()
                .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore()) // No se actualiza
                .ForMember(dest => dest.FechaModificacion, opt => opt.Ignore()) // Se establece en DbContext
                .ForMember(dest => dest.Activo, opt => opt.Ignore()) // No se actualiza desde el DTO
                .ForMember(dest => dest.Lotes, opt => opt.Ignore());
        }
    }
}