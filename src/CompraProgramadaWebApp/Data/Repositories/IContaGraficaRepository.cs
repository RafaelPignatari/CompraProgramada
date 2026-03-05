using CompraProgramada.Models;
using System.Threading.Tasks;

namespace CompraProgramadaWebApp.Data.Repositories
{
    public interface IContaGraficaRepository
    {
        Task AddAsync(ContasGraficasViewModel conta);
        Task<ContasGraficasViewModel?> GetByIdAsync(long id);
        Task SaveChangesAsync();
    }
}
