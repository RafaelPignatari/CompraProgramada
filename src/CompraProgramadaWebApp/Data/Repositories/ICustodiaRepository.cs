using CompraProgramada.Models;
using System.Threading.Tasks;

namespace CompraProgramadaWebApp.Data.Repositories
{
    public interface ICustodiaRepository
    {
        Task AddAsync(CustodiaViewModel custodia);
        Task SaveChangesAsync();
    }
}
