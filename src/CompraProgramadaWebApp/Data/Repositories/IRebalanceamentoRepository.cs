using CompraProgramada.Models;
using System.Threading.Tasks;

namespace CompraProgramadaWebApp.Data.Repositories
{
    public interface IRebalanceamentoRepository
    {
        Task AddAsync(RebalanceamentoViewModel rebalanceamento);
        Task AddRangeAsync(IEnumerable<RebalanceamentoViewModel> rebalanceamentos);
        Task SaveChangesAsync();
    }
}
