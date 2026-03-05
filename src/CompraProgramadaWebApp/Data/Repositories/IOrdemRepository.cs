using CompraProgramada.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CompraProgramadaWebApp.Data.Repositories
{
    public interface IOrdemRepository
    {
        Task AddAsync(OrdemCompraViewModel ordem);
        Task AddRangeAsync(IEnumerable<OrdemCompraViewModel> ordens);
        Task SaveChangesAsync();
    }
}
