using AutoMapper;
using SistemaFacturacionSRI.Application.DTOs.Categoria;
using SistemaFacturacionSRI.Domain.Entities;

namespace SistemaFacturacionSRI.Application.Mappings
{
    public class CategoriaMappingProfile : Profile
    {
        public CategoriaMappingProfile()
        {
            // Categoria -> CategoriaDto
            CreateMap<Categoria, CategoriaDto>();

            // CreateCategoriaDto -> Categoria
            CreateMap<CreateCategoriaDto, Categoria>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
                .ForMember(dest => dest.FechaModificacion, opt => opt.Ignore())
                .ForMember(dest => dest.Productos, opt => opt.Ignore());

            // UpdateCategoriaDto -> Categoria
            CreateMap<UpdateCategoriaDto, Categoria>()
                .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
                .ForMember(dest => dest.FechaModificacion, opt => opt.Ignore())
                .ForMember(dest => dest.Productos, opt => opt.Ignore());
        }
    }
}