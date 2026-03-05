using CompraProgramada.Models;
using CompraProgramadaWebApp.Models.DTOs;
using System.Threading.Tasks;

namespace CompraProgramadaWebApp.Services
{
    public interface IClienteService
    {
        Task<AdesaoResponseDTO> AdesaoAsync(ClienteDTO model);
        Task<ClienteViewModel?> SaidaAsync(long clienteId);
        Task<AlteracaoResponseDTO?> AlterarValorMensalAsync(long clienteId, decimal novoValor);
        Task<ClienteViewModel?> GetByIdAsync(long clienteId);
        Task<int> GetQtdClientesAtivosAsync();
    }
}
