using CompraProgramada.Models;
using CompraProgramadaWebApp.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace CompraProgramadaWebApp.Data.Repositories
{
    public class ContaGraficaRepository : IContaGraficaRepository
    {
        private readonly AppDbContext _context;
        public ContaGraficaRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(ContasGraficasViewModel conta)
        {
            await _context.ContaGraficas.AddAsync(conta);
        }

        public async Task<ContasGraficasViewModel?> GetByIdAsync(long id)
        {
            return await _context.ContaGraficas.FindAsync(id);
        }

        public async Task<ContasGraficasViewModel?> GetByClienteIdAsync(long clienteId)
        {
            return await _context.ContaGraficas.FirstOrDefaultAsync(c => c.ClienteId == clienteId);
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }
    }
}
