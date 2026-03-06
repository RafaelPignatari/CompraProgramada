using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CompraProgramada.Models;
using CompraProgramadaWebApp.Data.Repositories;
using CompraProgramadaWebApp.Services;

namespace CompraProgramadaWebApp.Controllers
{
    public class RentabilidadeController : Controller
    {
        private readonly IClienteRepository _clienteRepo;
        private readonly IContaGraficaRepository _contaGraficaRepo;
        private readonly ICustodiaRepository _custodiaRepo;
        private readonly ICotacaoService _cotacaoService;

        public RentabilidadeController(
            IClienteRepository clienteRepo,
            IContaGraficaRepository contaGraficaRepo,
            ICustodiaRepository custodiaRepo,
            ICotacaoService cotacaoService)
        {
            _clienteRepo = clienteRepo;
            _contaGraficaRepo = contaGraficaRepo;
            _custodiaRepo = custodiaRepo;
            _cotacaoService = cotacaoService;
        }

        // GET: /Rentabilidade/Index - Lista todos os clientes para selecao
        public async Task<IActionResult> Index()
        {
            var todosClientes = await _clienteRepo.GetAllClientesAsync();
            var clientesExibiveis = todosClientes.Where(c => c.Id != 1).ToList();
            return View(clientesExibiveis);
        }

        // GET: /Rentabilidade/Detalhes/{id}
        public async Task<IActionResult> Detalhes(long id)
        {
            var cliente = await _clienteRepo.GetByIdAsync(id);
            if (cliente == null)
                return NotFound();

            var conta = await _contaGraficaRepo.GetByClienteIdAsync(id);
            var model = new RentabilidadeViewModel
            {
                ClienteId = cliente.Id,
                ClienteNome = cliente.Nome
            };

            if (conta == null)
            {
                return View(model);
            }

            var custodias = (await _custodiaRepo.GetByContaIdAsync(conta.Id)).ToList();
            if (!custodias.Any())
                return View(model);

            var tickers = custodias.Select(c => c.Ticker).Distinct().ToList();
            var precos = await _cotacaoService.GetPrecosFechamentoMaisRecentesAsync(tickers);

            decimal totalInvestido = 0m;
            decimal totalAtual = 0m;

            foreach (var c in custodias)
            {
                var precoAtualNullable = precos.ContainsKey(c.Ticker) ? precos[c.Ticker] : null;
                var precoAtual = precoAtualNullable ?? 0m;
                var valorInvestido = c.Quantidade * c.PrecoMedio;
                var valorAtual = c.Quantidade * precoAtual;
                var pl = (precoAtual - c.PrecoMedio) * c.Quantidade;

                totalInvestido += valorInvestido;
                totalAtual += valorAtual;

                model.Ativos.Add(new RentabilidadeAtivoViewModel
                {
                    Ticker = c.Ticker,
                    Quantidade = c.Quantidade,
                    PrecoMedio = c.PrecoMedio,
                    CotacaoAtual = precoAtual,
                    ValorAtual = valorAtual,
                    PL = pl,
                    ComposicaoPercentual = 0m // set later
                });
            }

            // compute composition percentages
            foreach (var a in model.Ativos)
            {
                a.ComposicaoPercentual = totalAtual > 0 ? (a.ValorAtual / totalAtual) * 100m : 0m;
            }

            model.ValorInvestidoTotal = totalInvestido;
            model.ValorAtualTotal = totalAtual;
            model.PLTotal = totalAtual - totalInvestido;
            model.RentabilidadePercentual = totalInvestido > 0 ? ((totalAtual - totalInvestido) / totalInvestido) * 100m : 0m;

            return View(model);
        }
    }
}
