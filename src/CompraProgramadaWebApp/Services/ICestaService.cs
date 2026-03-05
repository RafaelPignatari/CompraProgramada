using CompraProgramada.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CompraProgramadaWebApp.Services
{
    public interface ICestaService
    {
        Task<CestaRecomendacaoViewModel> CriarOuAtualizarCestaAsync(string nome, IEnumerable<ItemCestaViewModel> itens);
        Task<CestaRecomendacaoViewModel?> GetAtualAsync();
        Task<IEnumerable<CestaRecomendacaoViewModel>> GetHistoricoAsync();
    }
}
