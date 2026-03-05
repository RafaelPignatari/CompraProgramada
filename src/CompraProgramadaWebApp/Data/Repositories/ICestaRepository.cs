using CompraProgramada.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CompraProgramadaWebApp.Data.Repositories
{
    public interface ICestaRepository
    {
        Task<CestaRecomendacaoViewModel> AddAsync(CestaRecomendacaoViewModel cesta, IEnumerable<ItemCestaViewModel> itens);
        Task<CestaRecomendacaoViewModel?> GetAtualAsync();
        Task<IEnumerable<ItemCestaViewModel>> GetItensByCestaIdAsync(long cestaId);
        Task<IEnumerable<CestaRecomendacaoViewModel>> GetHistoricoAsync();
        Task SaveChangesAsync();
    }
}
