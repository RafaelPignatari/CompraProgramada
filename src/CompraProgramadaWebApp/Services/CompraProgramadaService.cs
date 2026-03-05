using CompraProgramada.Models;
using CompraProgramadaWebApp.Data.Repositories;
using CompraProgramadaWebApp.Helpers;
using CompraProgramadaWebApp.Models.DTOs;
using CompraProgramadaWebApp.Models.Enums;

namespace CompraProgramadaWebApp.Services
{
    public class CompraProgramadaService : ICompraProgramadaService
    {
        private readonly IClienteRepository _clienteRepo;
        private readonly ICestaService _cestaService;
        private readonly ICotacaoService _cotacaoService;
        private readonly IContaMasterRepository _contaMasterRepo;
        private readonly IOrdemRepository _ordemRepo;
        private readonly IContaGraficaRepository _contaGraficaRepo;
        private readonly ICustodiaRepository _custodiaRepo;

        public CompraProgramadaService(
            IClienteRepository clienteRepo,
            ICestaService cestaService,
            ICotacaoService cotacaoService,
            IContaMasterRepository contaMasterRepo,
            IOrdemRepository ordemRepo,
            IContaGraficaRepository contaGraficaRepo,
            ICustodiaRepository custodiaRepo)
        {
            _clienteRepo = clienteRepo;
            _cestaService = cestaService;
            _cotacaoService = cotacaoService;
            _contaMasterRepo = contaMasterRepo;
            _ordemRepo = ordemRepo;
            _contaGraficaRepo = contaGraficaRepo;
            _custodiaRepo = custodiaRepo;
        }

        public async Task<int> ExecutarAsync(DateTime? dataExecucao = null)
        {
            dataExecucao = ValidaDataExecucao(dataExecucao);

            var (clientes, totalConsolidado) = await GetClientesEConsolidadoAsync();
            if (totalConsolidado <= 0)
                return 0;

            var cesta = await _cestaService.GetAtualAsync();
            if (cesta == null || cesta.Itens == null)
                throw new InvalidOperationException(Constantes.CESTA_NAO_ENCONTRADA);

            var ativos = cesta.Itens.Select(i => new ItemCestaResponseDTO { Ticker = i.Ticker.Trim(), Percentual = i.Percentual }).ToList();
            var tickers = ativos.Select(a => a.Ticker).Distinct().ToList();

            var precosTicker = await _cotacaoService.GetPrecosFechamentoMaisRecentesAsync(tickers);

            // Obter custodia master (residuos)
            var custodia = (await _contaMasterRepo.GetCustodiaAsync()).ToList();
            var residuos = custodia.ToDictionary(c => c.Ticker.Trim(), c => c.Quantidade);

            var (ordens, statusTickers) = ProcessaOrdens(ativos, precosTicker, residuos, totalConsolidado, dataExecucao.Value);

            if (ordens.Count == 0 && statusTickers.Count == 0)
                return 0;

            if (ordens.Count > 0)
            {
                await _ordemRepo.AddRangeAsync(ordens);
                await _ordemRepo.SaveChangesAsync();
            }

            await DistribuirParaContasAsync(statusTickers, clientes, totalConsolidado, dataExecucao.Value);

            return ordens.Count;
        }

        private DateTime ValidaDataExecucao(DateTime? dataRecebida)
        {
            var dataExecucao = dataRecebida?.Date ?? DateTime.UtcNow.Date;
            var execDates = GetExecutionDates(dataExecucao.Year, dataExecucao.Month);

            if (!execDates.Contains(dataExecucao))
                throw new InvalidOperationException("Data informada não é uma data de execução programada (5/15/25 ou adiadas para próximo dia útil).");

            return dataExecucao;
        }

        private async Task<(List<ClienteViewModel> clientes, decimal totalConsolidado)> GetClientesEConsolidadoAsync()
        {
            var clientes = (await _clienteRepo.GetClientesAtivosAsync()).ToList();
            var aportes = clientes.Select(c => c.ValorMensal / 3m).ToList();
            var totalConsolidado = aportes.Sum();

            return (clientes, totalConsolidado);
        }

        private (List<OrdemCompraViewModel> ordens, Dictionary<string, DetalhesTickerDTO> statusTickers) ProcessaOrdens(
                                                    List<ItemCestaResponseDTO>? ativos,
                                                    Dictionary<string, decimal?> precosTicker,
                                                    Dictionary<string, int> residuos,
                                                    decimal totalConsolidado,
                                                    DateTime execDate)
        {
            var ordens = new List<OrdemCompraViewModel>();
            var statusTickers = new Dictionary<string, DetalhesTickerDTO>();

            foreach (var ativo in ativos)
            {
                precosTicker.TryGetValue(ativo.Ticker, out var preco);

                if (preco == null || preco <= 0)
                    continue;

                var valorASerGastoAtivo = totalConsolidado * (ativo.Percentual / 100);
                var quantidadeASerComprada = (int)Math.Floor(valorASerGastoAtivo / preco.Value);

                residuos.TryGetValue(ativo.Ticker, out var qtdResiduoTicker);
                var residuoUsado = Math.Min(qtdResiduoTicker, quantidadeASerComprada);
                var quantidadeComprada = Math.Max(quantidadeASerComprada - qtdResiduoTicker, 0);

                // atualiza o mapa de residuos em memória (residuo remanescente antes da distribuição)
                residuos[ativo.Ticker] = qtdResiduoTicker - residuoUsado;

                // armazenar estatísticas para distribuição
                statusTickers[ativo.Ticker] = new DetalhesTickerDTO(preco.Value, quantidadeASerComprada, quantidadeComprada, residuoUsado);

                if (quantidadeComprada <= 0)
                    continue;

                // separar lote padrao vs fracionario para as quantidades efetivamente compradas
                var qtdLotes = quantidadeComprada / 100;
                var qtdAcoes = qtdLotes * 100; //Quando uma ordem é feita, compramos ações em lotes de 100 por padrão.
                var qtdLotesFracionados = quantidadeComprada % 100;

                if (qtdAcoes > 0)
                {
                    ordens.Add(new OrdemCompraViewModel
                    {
                        ContaMasterId = 1,
                        Ticker = ativo.Ticker,
                        Quantidade = qtdAcoes,
                        PrecoUnitario = preco.Value,
                        TipoMercado = EnumTipoMercado.LOTE,
                        DataExecucao = execDate
                    });
                }

                if (qtdLotesFracionados > 0)
                {
                    ordens.Add(new OrdemCompraViewModel
                    {
                        ContaMasterId = 1,
                        Ticker = ativo.Ticker + "F",
                        Quantidade = qtdLotesFracionados,
                        PrecoUnitario = preco.Value,
                        TipoMercado = EnumTipoMercado.FRACIONARIO,
                        DataExecucao = execDate
                    });
                }
            }

            return (ordens, statusTickers);
        }

        private async Task DistribuirParaContasAsync(Dictionary<string, DetalhesTickerDTO> statusTickers, List<ClienteViewModel> clientes, decimal totalConsolidado, DateTime execDate)
        {
            foreach (var statTicker in statusTickers)
            {
                var ticker = statTicker.Key;
                var preco = statTicker.Value.Preco;
                var quantidadeSolicitada = statTicker.Value.QuantidadeSolicitada;
                var quantidadeComprada = statTicker.Value.QuantidadeComprada;
                var residuoUsado = statTicker.Value.ResiduoUsado;

                var totalDisponivel = quantidadeComprada + residuoUsado;
                if (totalDisponivel <= 0)
                    continue;

                // Distribuir proporcionalmente aos clientes ativos
                var distribuicoes = new Dictionary<long, int>(); // clienteId -> quantidade
                var totalDistribuido = 0;

                foreach (var cliente in clientes)
                {
                    var aporte = cliente.ValorMensal / 3m;
                    var proporcao = aporte / totalConsolidado;
                    var qtdParaCliente = (int)Math.Floor(totalDisponivel * proporcao);

                    if (qtdParaCliente > 0)
                    {
                        distribuicoes[cliente.Id] = qtdParaCliente;
                        totalDistribuido += qtdParaCliente;
                    }
                }

                await DistribuiParaFilhotes(distribuicoes, ticker, preco, execDate);
                await DistribuiParaMaster(totalDisponivel, totalDistribuido, ticker, preco, execDate);

                await _custodiaRepo.SaveChangesAsync();               
            }
        }

        private async Task DistribuiParaFilhotes(Dictionary<long, int> distribuicoes, string ticker, decimal preco, DateTime execDate)
        {
            // Aplicar as distribuições nas custodias filhotes
            foreach (var kvCliente in distribuicoes)
            {
                var clienteId = kvCliente.Key;
                var qtd = kvCliente.Value;

                if (qtd <= 0)
                    continue;

                var conta = await _contaGraficaRepo.GetByClienteIdAsync(clienteId);

                if (conta == null)
                    continue; // sem conta filhote

                var cust = await _custodiaRepo.GetByContaAndTickerAsync(conta.Id, (string)ticker);

                if (cust == null)
                {
                    // criar nova custódia
                    var nova = new CustodiaViewModel
                    {
                        ContaGraficaId = conta.Id,
                        Ticker = ticker,
                        Quantidade = qtd,
                        PrecoMedio = preco,
                        DataUltimaAtualizacao = execDate
                    };

                    await _custodiaRepo.AddAsync(nova);
                }
                else
                {
                    var qtdAnterior = cust.Quantidade;
                    var precoMedioAnterior = cust.PrecoMedio;
                    var novoQtd = qtdAnterior + qtd;
                    var novoPM = CalculaPrecoMedio(qtdAnterior, precoMedioAnterior, novoQtd, preco);

                    cust.Quantidade = novoQtd;
                    cust.PrecoMedio = novoPM;
                    cust.DataUltimaAtualizacao = execDate;

                    await _custodiaRepo.UpdateAsync(cust);
                }
            }
        }

        // Atualizar a custódia master com o que sobrou
        private async Task DistribuiParaMaster(int totalDisponivel, int totalDistribuido, string ticker, decimal preco, DateTime execDate)
        {            
            var masterContaId = 1; // conta master fixa
            var qtdResiduo = totalDisponivel - totalDistribuido;
            var custodiaMaster = await _custodiaRepo.GetByContaAndTickerAsync(masterContaId, (string)ticker);

            if (custodiaMaster == null)
            {
                if (qtdResiduo > 0)
                {
                    var novaMaster = new CustodiaViewModel
                    {
                        ContaGraficaId = masterContaId,
                        Ticker = ticker,
                        Quantidade = qtdResiduo,
                        PrecoMedio = preco,
                        DataUltimaAtualizacao = execDate
                    };

                    await _custodiaRepo.AddAsync(novaMaster);
                    await _custodiaRepo.SaveChangesAsync();
                }
            }
            else
            {
                custodiaMaster.PrecoMedio = CalculaPrecoMedio(custodiaMaster.Quantidade, custodiaMaster.PrecoMedio, qtdResiduo, preco);
                custodiaMaster.Quantidade = custodiaMaster.Quantidade + qtdResiduo;                
                custodiaMaster.DataUltimaAtualizacao = execDate;

                await _custodiaRepo.UpdateAsync(custodiaMaster);
                await _custodiaRepo.SaveChangesAsync();
            }
        }

        private decimal CalculaPrecoMedio(int qtdAnterior, decimal precoMedioAnterior, int qtdNova, decimal precoNovo)
        {
            if (qtdAnterior <= 0)
                return precoNovo;

            return ((qtdAnterior * precoMedioAnterior) + (qtdNova * precoNovo)) / (qtdAnterior + qtdNova);
        }

        private static List<DateTime> GetExecutionDates(int year, int month)
        {
            var datasExecucao = new[] { 5, 15, 25 };
            var list = new List<DateTime>();

            foreach (var d in datasExecucao)
            {
                var dt = new DateTime(year, month, Math.Min(d, DateTime.DaysInMonth(year, month)));

                // se cair no fim de semana, avançar até próxima segunda
                while (dt.DayOfWeek == DayOfWeek.Saturday || dt.DayOfWeek == DayOfWeek.Sunday)
                    dt = dt.AddDays(1);

                list.Add(dt.Date);
            }

            return list;
        }
    }
}
