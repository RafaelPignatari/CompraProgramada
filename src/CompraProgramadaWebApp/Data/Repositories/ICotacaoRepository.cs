using System.Collections.Generic;
using System.Threading.Tasks;
using CompraProgramada.Models;

namespace CompraProgramadaWebApp.Data.Repositories
{
    public interface ICotacaoRepository
    {
        Task<CotacaoViewModel?> GetLatestByTickerAsync(string ticker);
        Task<Dictionary<string, CotacaoViewModel?>> GetLatestByTickersAsync(IEnumerable<string> tickers);
    }
}
