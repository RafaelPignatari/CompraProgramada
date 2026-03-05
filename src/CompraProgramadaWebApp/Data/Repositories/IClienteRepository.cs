using CompraProgramada.Models;
using System.Threading.Tasks;

namespace CompraProgramadaWebApp.Data.Repositories
{
    public interface IClienteRepository
    {
        Task<ClienteViewModel?> GetByIdAsync(long id);
        Task<ClienteViewModel?> GetByCpfAsync(string cpf);
        Task AddAsync(ClienteViewModel cliente);
        Task UpdateAsync(ClienteViewModel cliente);
        Task SaveChangesAsync();
    }
}
