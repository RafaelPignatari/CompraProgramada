using CompraProgramada.Models;
using CompraProgramadaWebApp.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CompraProgramadaWebApp.Data.Repositories
{
    public class OrdemRepository : IOrdemRepository
    {
        private readonly AppDbContext _context;
        public OrdemRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(OrdemCompraViewModel ordem)
        {
            await _context.OrdensCompra.AddAsync(ordem);
        }

        public async Task AddRangeAsync(IEnumerable<OrdemCompraViewModel> ordens)
        {
            await _context.OrdensCompra.AddRangeAsync(ordens);
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }
    }
}
