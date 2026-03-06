using CompraProgramada.Models;
using CompraProgramadaWebApp.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace CompraProgramadaWebApp.Data.Repositories
{
    public class ClienteRepository : IClienteRepository
    {
        private readonly AppDbContext _context;
        public ClienteRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(ClienteViewModel cliente)
        {
            await _context.Clientes.AddAsync(cliente);
        }

        public async Task<ClienteViewModel?> GetByCpfAsync(string cpf)
        {
            return await _context.Clientes.FirstOrDefaultAsync(c => c.CPF == cpf);
        }

        public async Task<ClienteViewModel?> GetByIdAsync(long id)
        {
            return await _context.Clientes.FindAsync(id);
        }

        public async Task<int> GetQtdClientesAtivosAsync()
        {
            return await _context.Clientes.CountAsync(c => c.Ativo);
        }

        public async Task<IEnumerable<ClienteViewModel>> GetClientesAtivosAsync()
        {
            return await _context.Clientes.Where(c => c.Ativo).ToListAsync();
        }

        public async Task<IEnumerable<ClienteViewModel>> GetAllClientesAsync()
        {
            return await _context.Clientes.OrderByDescending(c => c.Ativo).ThenBy(c => c.Nome).ToListAsync();
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }

        public Task UpdateAsync(ClienteViewModel cliente)
        {
            _context.Clientes.Update(cliente);
            return Task.CompletedTask;
        }
    }
}
