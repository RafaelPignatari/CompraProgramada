using CompraProgramada.Models;
using CompraProgramadaWebApp.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace CompraProgramadaWebApp.Data.Repositories
{
    public class CustodiaRepository : ICustodiaRepository
    {
        private readonly AppDbContext _context;
        public CustodiaRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(CustodiaViewModel custodia)
        {
            await _context.Custodias.AddAsync(custodia);
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }

        public async Task<CustodiaViewModel?> GetByContaAndTickerAsync(long contaId, string ticker)
        {
            return await _context.Custodias.FirstOrDefaultAsync(c => c.ContaGraficaId == contaId && c.Ticker == ticker);
        }

        public Task UpdateAsync(CustodiaViewModel custodia)
        {
            _context.Custodias.Update(custodia);
            return Task.CompletedTask;
        }

        public async Task<IEnumerable<CustodiaViewModel>> GetByContaIdAsync(long contaId)
        {
            return await _context.Custodias.Where(c => c.ContaGraficaId == contaId).ToListAsync();
        }
    }
}
