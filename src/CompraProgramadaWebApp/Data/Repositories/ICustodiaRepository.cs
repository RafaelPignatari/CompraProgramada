using CompraProgramada.Models;
using System.Threading.Tasks;

namespace CompraProgramadaWebApp.Data.Repositories
{
    public interface ICustodiaRepository
    {
        Task AddAsync(CustodiaViewModel custodia);
        Task SaveChangesAsync();
        Task<CustodiaViewModel?> GetByContaAndTickerAsync(long contaId, string ticker);
        Task UpdateAsync(CustodiaViewModel custodia);
        Task<IEnumerable<CustodiaViewModel>> GetByContaIdAsync(long contaId);
    }
}
