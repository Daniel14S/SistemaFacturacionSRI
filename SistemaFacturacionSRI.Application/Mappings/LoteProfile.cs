using AutoMapper;
using SistemaFacturacionSRI.Application.DTOs.Lote;
using SistemaFacturacionSRI.Domain.Entities;

namespace SistemaFacturacionSRI.Application.Mappings
{
    /// <summary>
    /// Configura el mapeo entre la entidad Lote y sus DTOs.
    /// </summary>
    public class LoteProfile : Profile
    {
        public LoteProfile()
        {
            CreateMap<Lote, LoteDto>()
                .ForMember(dest => dest.LoteId, opt => opt.MapFrom(src => src.LoteId))
                .ForMember(dest => dest.ProductoId, opt => opt.MapFrom(src => src.ProductoId))
                .ForMember(dest => dest.ProductoCodigo, opt => opt.MapFrom(src => src.Producto != null ? src.Producto.Codigo : string.Empty))
                .ForMember(dest => dest.ProductoNombre, opt => opt.MapFrom(src => src.Producto != null ? src.Producto.Nombre : string.Empty))
                .ForMember(dest => dest.ProductoCategoria, opt => opt.MapFrom(src => src.Producto != null && src.Producto.Categoria != null ? src.Producto.Categoria.Nombre : string.Empty));

            CreateMap<CrearLoteDto, Lote>()
                .ForMember(dest => dest.LoteId, opt => opt.Ignore())
                .ForMember(dest => dest.CantidadDisponible, opt => opt.MapFrom(src => src.CantidadInicial))
                .ForMember(dest => dest.Producto, opt => opt.Ignore());
        }
    }
}
