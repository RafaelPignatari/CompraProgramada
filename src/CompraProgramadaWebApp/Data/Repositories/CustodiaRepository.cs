using CompraProgramada.Models;
using CompraProgramadaWebApp.Data;
using System.Threading.Tasks;

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
    }
}
