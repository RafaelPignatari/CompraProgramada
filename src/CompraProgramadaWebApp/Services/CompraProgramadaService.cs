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
        private readonly IKafkaProducerService _kafkaProducer;

        public CompraProgramadaService(
            IClienteRepository clienteRepo,
            ICestaService cestaService,
            ICotacaoService cotacaoService,
            IContaMasterRepository contaMasterRepo,
            IOrdemRepository ordemRepo,
            IContaGraficaRepository contaGraficaRepo,
            ICustodiaRepository custodiaRepo,
            IKafkaProducerService kafkaProducer)
        {
            _clienteRepo = clienteRepo;
            _cestaService = cestaService;
            _cotacaoService = cotacaoService;
            _contaMasterRepo = contaMasterRepo;
            _ordemRepo = ordemRepo;
            _contaGraficaRepo = contaGraficaRepo;
            _custodiaRepo = custodiaRepo;
            _kafkaProducer = kafkaProducer;
        }

        public async Task<CompraProgramadaResultDTO> ExecutarAsync(DateTime? dataRecebida = null)
        {
            var dataExecucao = ValidaDataExecucao(dataRecebida);

            var (clientes, totalConsolidado) = await GetClientesEConsolidadoAsync();
            if (totalConsolidado <= 0)
                return new CompraProgramadaResultDTO { DataExecucao = dataExecucao, TotalClientes = clientes.Count, TotalConsolidado = totalConsolidado, Mensagem = "Nenhum aporte a processar." };

            var cesta = await _cestaService.GetAtualAsync();
            if (cesta == null || cesta.Itens == null)
                throw new InvalidOperationException(Constantes.CESTA_NAO_ENCONTRADA);

            var ativos = cesta.Itens.Select(i => new ItemCestaResponseDTO { Ticker = i.Ticker.Trim(), Percentual = i.Percentual }).ToList();
            var tickers = ativos.Select(a => a.Ticker).Distinct().ToList();

            var precosTicker = await _cotacaoService.GetPrecosFechamentoMaisRecentesAsync(tickers);

            // Obter custodia master (residuos)
            var custodia = (await _contaMasterRepo.GetCustodiaAsync()).ToList();
            var residuos = custodia.ToDictionary(c => c.Ticker.Trim(), c => c.Quantidade);

            var (ordens, statusTickers) = ProcessaOrdens(ativos, precosTicker, residuos, totalConsolidado, dataExecucao);

            if (ordens.Count == 0 && statusTickers.Count == 0)
                return new CompraProgramadaResultDTO { DataExecucao = dataExecucao, TotalClientes = clientes.Count, TotalConsolidado = totalConsolidado, Mensagem = "Nenhuma ordem gerada." };

            await SaveOrdensIfAnyAsync(ordens);

            var distribuicaoResult = await DistribuirParaContasAsync(statusTickers, clientes, totalConsolidado, dataExecucao);

            return BuildResultado(dataExecucao, clientes.Count, totalConsolidado, ordens, statusTickers, distribuicaoResult);
        }

        private async Task SaveOrdensIfAnyAsync(List<OrdemCompraViewModel> ordens)
        {
            if (ordens == null || ordens.Count == 0)
                return;

            await _ordemRepo.AddRangeAsync(ordens);
            await _ordemRepo.SaveChangesAsync();
        }

        private CompraProgramadaResultDTO BuildResultado(DateTime dataExecucao,
            int totalClientes,
            decimal totalConsolidado,
            List<OrdemCompraViewModel> ordens,
            Dictionary<string, DetalhesTickerDTO> statusTickers,
            (List<DistribuicaoClienteDTO> Distribuicoes, List<ResiduoDTO> Residuos, int EventosIRPublicados) distribuicaoResult)
        {
            var retorno = new CompraProgramadaResultDTO
            {
                DataExecucao = dataExecucao,
                TotalClientes = totalClientes,
                TotalConsolidado = totalConsolidado,
                EventosIRPublicados = distribuicaoResult.EventosIRPublicados,
                Mensagem = $"Compra programada executada com sucesso para {totalClientes} clientes."
            };

            var ordensPorBase = ordens
                .GroupBy(o => o.Ticker.EndsWith("F") ? o.Ticker.Substring(0, o.Ticker.Length - 1) : o.Ticker)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var kv in statusTickers)
            {
                var ticker = kv.Key;
                var detalhes = kv.Value;
                var dtoOrdem = new OrdemCompraDTO
                {
                    Ticker = ticker,
                    QuantidadeTotal = detalhes.QuantidadeComprada + detalhes.ResiduoUsado,
                    PrecoUnitario = detalhes.Preco,
                    ValorTotal = (detalhes.QuantidadeComprada + detalhes.ResiduoUsado) * detalhes.Preco
                };

                if (ordensPorBase.TryGetValue(ticker, out var listaOrdens))
                {
                    foreach (var o in listaOrdens)
                    {
                        var tipo = o.TipoMercado == EnumTipoMercado.LOTE ? "LOTE" : "FRACIONARIO";
                        dtoOrdem.Detalhes.Add(new OrdemDetalheDTO { Tipo = tipo, Ticker = o.Ticker, Quantidade = o.Quantidade });
                    }
                }

                retorno.OrdensCompra.Add(dtoOrdem);
            }

            retorno.Distribuicoes = distribuicaoResult.Distribuicoes;
            retorno.ResiduosCustMaster = distribuicaoResult.Residuos;

            return retorno;
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
                precosTicker.TryGetValue(ativo.Ticker, out decimal? preco);

                if (preco == null || preco <= 0)
                    continue;

                var valorASerGastoAtivo = totalConsolidado * (ativo.Percentual / 100);
                var quantidadeASerComprada = (int)Math.Floor(valorASerGastoAtivo / preco.Value);

                residuos.TryGetValue(ativo.Ticker, out int qtdResiduoTicker);
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

        private async Task<(List<DistribuicaoClienteDTO> Distribuicoes, List<ResiduoDTO> Residuos, int EventosIRPublicados)> DistribuirParaContasAsync(Dictionary<string, DetalhesTickerDTO> statusTickers, List<ClienteViewModel> clientes, decimal totalConsolidado, DateTime execDate)
        {
            var distribuicoesPorCliente = new Dictionary<long, DistribuicaoClienteDTO>();
            var residuos = new List<ResiduoDTO>();
            var eventosIR = 0;

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

                        if (!distribuicoesPorCliente.TryGetValue(cliente.Id, out var dc))
                        {
                            dc = new DistribuicaoClienteDTO { ClienteId = cliente.Id, Nome = cliente.Nome, ValorAporte = cliente.ValorMensal / 3m };
                            distribuicoesPorCliente[cliente.Id] = dc;
                        }

                        dc.Ativos.Add(new AtivoDistribuicaoDTO { Ticker = ticker, Quantidade = qtdParaCliente });
                    }
                }
                
                await DistribuiParaFilhotes(distribuicoes, ticker, preco, execDate);
                int qtdResiduoMaster = await DistribuiParaMaster(totalDisponivel, totalDistribuido, ticker, preco, execDate);

                await _custodiaRepo.SaveChangesAsync();

                residuos.Add(new ResiduoDTO { Ticker = ticker, Quantidade = qtdResiduoMaster });
            }

            return (distribuicoesPorCliente.Values.ToList(), residuos, eventosIR);
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
                    await PublicarIrCompraAsync(clienteId, conta, ticker, qtd, preco, execDate);
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
                    await PublicarIrCompraAsync(clienteId, conta, ticker, qtd, preco, execDate);
                }
            }
        }

        // Atualizar a custódia master com o que sobrou
        private async Task<int> DistribuiParaMaster(int totalDisponivel, int totalDistribuido, string ticker, decimal preco, DateTime execDate)
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

            return qtdResiduo;            
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

        private async Task PublicarIrCompraAsync(long clienteId, ContasGraficasViewModel conta, string ticker, int quantidade, decimal precoUnitario, DateTime dataOperacao)
        {
            try
            {
                if (_kafkaProducer == null)
                    return;

                var cliente = await _clienteRepo.GetByIdAsync(clienteId);
                if (cliente == null)
                    return;

                var valorOperacao = Math.Round(quantidade * precoUnitario, 2);
                const decimal aliquota = 0.00005m; // 0.005%
                var valorIr = Math.Round(valorOperacao * aliquota, 2);

                var msg = new
                {
                    tipo = "IR_DEDO_DURO",
                    clienteId = cliente.Id,
                    cpf = cliente.CPF,
                    ticker = ticker,
                    tipoOperacao = "COMPRA",
                    quantidade = quantidade,
                    precoUnitario = Math.Round(precoUnitario, 2),
                    valorOperacao = valorOperacao,
                    aliquota = aliquota,
                    valorIR = valorIr,
                    dataOperacao = dataOperacao.ToString("o")
                };

                var json = System.Text.Json.JsonSerializer.Serialize(msg);
                await _kafkaProducer.PublishAsync("IR_DEDO_DURO", json);
            }
            catch
            {
                // Não propagar exceção para não interromper o fluxo. TODO: Adicionar logs
            }
        }
    }
}
