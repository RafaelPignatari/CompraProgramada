using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CompraProgramadaWebApp.Data.Repositories;

namespace CompraProgramadaWebApp.Services
{
    public class CotacaoService : ICotacaoService
    {
        private readonly ICotacaoRepository _repo;

        public CotacaoService(ICotacaoRepository repo)
        {
            _repo = repo;
        }

        public async Task<decimal?> GetPrecoFechamentoMaisRecenteAsync(string ticker)
        {
            var cot = await _repo.GetLatestByTickerAsync(ticker?.Trim() ?? string.Empty);
            return cot?.PrecoFechamento;
        }

        public async Task<Dictionary<string, decimal?>> GetPrecosFechamentoMaisRecentesAsync(IEnumerable<string> tickers)
        {
            var dict = await _repo.GetLatestByTickersAsync(tickers ?? Enumerable.Empty<string>());
            return dict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.PrecoFechamento);
        }
    }
}
