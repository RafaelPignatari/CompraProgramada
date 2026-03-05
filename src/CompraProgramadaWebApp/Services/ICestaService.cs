using CompraProgramada.Models;
using CompraProgramadaWebApp.Models.DTOs;

namespace CompraProgramadaWebApp.Services
{
    public interface ICestaService
    {
        Task<CestaResponseDTO> CriarOuAtualizarCestaAsync(CestaRequestDTO cestaDTO);
        Task<CestaGetResponseDTO> GetAtualAsync();
        Task<IEnumerable<HistoricoCestaResponseDTO>> GetHistoricoAsync();
    }
}
