using CompraProgramadaWebApp.Data.Repositories;
using CompraProgramadaWebApp.Models.DTOs;
using System.Linq;
using System.Threading.Tasks;

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

            var retorno = new CustodiaResponseDTO();
            retorno.ContaMaster = new { id = 1, numeroConta = "MST-000001", tipo = "MASTER" };
            retorno.Custodia = custodia.Select(c => new { ticker = c.Ticker, quantidade = c.Quantidade, precoMedio = c.PrecoMedio, valorAtual = c.PrecoMedio }).ToArray();
            retorno.ValorTotalResiduo = valorTotalResiduo;

            return retorno;
        }
    }
}
