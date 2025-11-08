using SistemaFacturacionSRI.Application.DTOs.Categoria;

namespace SistemaFacturacionSRI.WebUI.Services
{
    /// <summary>
    /// Interfaz del servicio HTTP para consumir la API de Categorías.
    /// Define el contrato para la comunicación con el backend.
    /// </summary>
    public interface ICategoriaHttpService
    {
        /// <summary>
        /// Obtiene todas las categorías activas.
        /// GET /api/categorias
        /// </summary>
        Task<List<CategoriaDto>> ObtenerTodasAsync();
    }
}