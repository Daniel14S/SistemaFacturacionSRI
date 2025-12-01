using SistemaFacturacionSRI.Domain.DTOs.Factura;

namespace SistemaFacturacionSRI.Application.DTOs.Facturacion
{
    /// <summary>
    /// DTO para respuesta paginada de facturas
    /// </summary>
    public class ListadoFacturasDto
    {
        /// <summary>
        /// Lista de facturas de la página actual
        /// </summary>
        public List<FacturaListDto> Facturas { get; set; } = new();

        /// <summary>
        /// Total de registros que cumplen los filtros
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// Página actual
        /// </summary>
        public int Pagina { get; set; }

        /// <summary>
        /// Tamaño de página
        /// </summary>
        public int TamanoPagina { get; set; }

        /// <summary>
        /// Total de páginas
        /// </summary>
        public int TotalPaginas => TamanoPagina > 0 ? (int)Math.Ceiling((double)Total / TamanoPagina) : 0;

        /// <summary>
        /// Indica si hay página anterior
        /// </summary>
        public bool TienePaginaAnterior => Pagina > 1;

        /// <summary>
        /// Indica si hay página siguiente
        /// </summary>
        public bool TienePaginaSiguiente => Pagina < TotalPaginas;
    }
}