using CompraProgramada.Models;
using CompraProgramadaWebApp.Data;
using System.Threading.Tasks;

namespace CompraProgramadaWebApp.Data.Repositories
{
    public class RebalanceamentoRepository : IRebalanceamentoRepository
    {
        private readonly AppDbContext _context;

        public RebalanceamentoRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(RebalanceamentoViewModel rebalanceamento)
        {
            await _context.Rebalanceamentos.AddAsync(rebalanceamento);
        }

        public async Task AddRangeAsync(IEnumerable<RebalanceamentoViewModel> rebalanceamentos)
        {
            await _context.Rebalanceamentos.AddRangeAsync(rebalanceamentos);
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }
    }
}
