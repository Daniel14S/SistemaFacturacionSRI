using AutoMapper;
using SistemaFacturacionSRI.Domain.DTOs.Factura;
using SistemaFacturacionSRI.Domain.Entities;
using SistemaFacturacionSRI.Domain.Enums;

namespace SistemaFacturacionSRI.Application.Mappings
{
    /// <summary>
    /// Profile de AutoMapper para mapeo de Facturas
    /// </summary>
    public class FacturaMappingProfile : Profile
    {
        public FacturaMappingProfile()
        {
            // Factura → FacturaDto
            CreateMap<Factura, FacturaDto>()
                // Cliente (nested)
                .ForMember(dest => dest.Cliente, 
                    opt => opt.MapFrom(src => src.Cliente))
                
                // Usuario - construir nombre completo desde campos separados
                .ForMember(dest => dest.UsuarioNombre, 
                    opt => opt.MapFrom(src => src.Usuario != null 
                        ? ObtenerNombreCompletoUsuario(src.Usuario)
                        : string.Empty))
                
                // Estado
                .ForMember(dest => dest.Estado, 
                    opt => opt.MapFrom(src => src.Estado.ToString()))
                .ForMember(dest => dest.EstadoDescripcion, 
                    opt => opt.MapFrom(src => ObtenerDescripcionEstado(src.Estado)))
                
                // Totales (mapear de nombres en entidad a nombres en DTO)
                .ForMember(dest => dest.SubtotalTotal, 
                    opt => opt.MapFrom(src => src.SubtotalConDescuento))
                .ForMember(dest => dest.TotalDescuento, 
                    opt => opt.MapFrom(src => src.Descuento))
                .ForMember(dest => dest.TotalIVA, 
                    opt => opt.MapFrom(src => src.IVA12 + src.IVA15))
                .ForMember(dest => dest.Total, 
                    opt => opt.MapFrom(src => src.ImporteTotal))
                
                // Detalles e info adicional
                .ForMember(dest => dest.Detalles, 
                    opt => opt.MapFrom(src => src.Detalles))
                .ForMember(dest => dest.InfoAdicional, 
                    opt => opt.MapFrom(src => src.InformacionAdicional));

            // Cliente → ClienteFacturaDto
            CreateMap<Cliente, ClienteFacturaDto>()
                .ForMember(dest => dest.RazonSocial, 
                    opt => opt.MapFrom(src => src.NombreCompleto()));

            // DetalleFactura → DetalleFacturaDto
            CreateMap<DetalleFactura, DetalleFacturaDto>();

            // InfoAdicional → InfoAdicionalDto
            CreateMap<InfoAdicional, InfoAdicionalDto>();
        }

        /// <summary>
        /// Construye el nombre completo del usuario desde sus campos separados
        /// </summary>
        private static string ObtenerNombreCompletoUsuario(Usuario usuario)
        {
            var nombres = new[] 
            { 
                usuario.Nombre1, 
                usuario.Nombre2, 
                usuario.Apellido1, 
                usuario.Apellido2 
            }
            .Where(n => !string.IsNullOrWhiteSpace(n));
            
            return string.Join(" ", nombres);
        }

        /// <summary>
        /// Obtiene la descripción amigable del estado
        /// </summary>
        private static string ObtenerDescripcionEstado(EstadoFactura estado)
        {
            return estado switch
            {
                EstadoFactura.BORRADOR => "Borrador",
                EstadoFactura.GENERADA => "XML Generado",
                EstadoFactura.FIRMADA => "Firmada Electrónicamente",
                EstadoFactura.ENVIADA => "Enviada al SRI",
                EstadoFactura.RECIBIDA => "Recibida por SRI",
                EstadoFactura.AUTORIZADA => "Autorizada",
                EstadoFactura.NO_AUTORIZADA => "No Autorizada",
                EstadoFactura.DEVUELTA => "Devuelta (Reenviar)",
                EstadoFactura.ANULADA => "Anulada",
                _ => estado.ToString()
            };
        }
    }
}