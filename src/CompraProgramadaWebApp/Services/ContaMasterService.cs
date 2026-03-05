using System.Linq;
using System.Threading.Tasks;
using CompraProgramadaWebApp.Data.Repositories;

namespace CompraProgramadaWebApp.Services
{
    public class ContaMasterService : IContaMasterService
    {
        private readonly IContaMasterRepository _repo;
        public ContaMasterService(IContaMasterRepository repo)
        {
            _repo = repo;
        }

        public async Task<object> GetCustodiaAsync()
        {
            var custodia = await _repo.GetCustodiaAsync();
            var valorTotalResiduo = 0m;

            // calcula valor total com preco medio quando disponivel
            foreach (var c in custodia)
            {
                valorTotalResiduo += c.PrecoMedio * c.Quantidade;
            }

            var contaMaster = new { id = 1, numeroConta = "MST-000001", tipo = "MASTER" };
            var itens = custodia.Select(c => new { ticker = c.Ticker, quantidade = c.Quantidade, precoMedio = c.PrecoMedio, valorAtual = c.PrecoMedio }).ToArray();

            return new { contaMaster, custodia = itens, valorTotalResiduo };
        }
    }
}
