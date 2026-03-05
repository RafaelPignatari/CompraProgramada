using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CompraProgramada.Models;
using CompraProgramadaWebApp.Data;
using Microsoft.EntityFrameworkCore;

namespace CompraProgramadaWebApp.Data.Repositories
{
    public class CotacaoRepository : ICotacaoRepository
    {
        private readonly AppDbContext _context;
        public CotacaoRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<CotacaoViewModel?> GetLatestByTickerAsync(string ticker)
        {
            if (string.IsNullOrWhiteSpace(ticker))
                return null;

            return await _context.Cotacoes
                .AsNoTracking()
                .Where(c => c.Ticker == ticker)
                .OrderByDescending(c => c.DataPregao)
                .FirstOrDefaultAsync();
        }

        public async Task<Dictionary<string, CotacaoViewModel?>> GetLatestByTickersAsync(IEnumerable<string> tickers)
        {
            var list = (tickers ?? Enumerable.Empty<string>()).Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).Distinct();

            var cotacoes = await _context.Cotacoes
                .AsNoTracking()
                .Where(c => list.Contains(c.Ticker))
                .ToListAsync();

            var dict = cotacoes
                .GroupBy(c => c.Ticker)
                .ToDictionary(g => g.Key, g => (CotacaoViewModel?)g.OrderByDescending(c => c.DataPregao).FirstOrDefault());

            var result = new Dictionary<string, CotacaoViewModel?>();
            foreach (var t in list)
            {
                dict.TryGetValue(t, out var val);
                result[t] = val;
            }

            return result;
        }
    }
}
