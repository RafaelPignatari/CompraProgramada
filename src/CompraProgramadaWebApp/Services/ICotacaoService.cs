using System.Collections.Generic;
using System.Threading.Tasks;

namespace CompraProgramadaWebApp.Services
{
    public interface ICotacaoService
    {
        Task<decimal?> GetPrecoFechamentoMaisRecenteAsync(string ticker);
        Task<Dictionary<string, decimal?>> GetPrecosFechamentoMaisRecentesAsync(IEnumerable<string> tickers);
    }
}
