using CompraProgramada.Models;
using CompraProgramadaWebApp.Data.Repositories;
using CompraProgramadaWebApp.Models.Enums;
using System.Text.Json;

namespace CompraProgramadaWebApp.Services
{
    public class RebalanceamentoService : IRebalanceamentoService
    {
        private readonly IClienteRepository _clienteRepo;
        private readonly IContaGraficaRepository _contaGraficaRepo;
        private readonly ICustodiaRepository _custodiaRepo;
        private readonly ICestaRepository _cestaRepo;
        private readonly ICotacaoService _cotacaoService;
        private readonly IRebalanceamentoRepository _rebalanceamentoRepo;
        private readonly IKafkaProducerService _kafkaProducer;

        public RebalanceamentoService(
            IClienteRepository clienteRepo,
            IContaGraficaRepository contaGraficaRepo,
            ICustodiaRepository custodiaRepo,
            ICestaRepository cestaRepo,
            ICotacaoService cotacaoService,
            IRebalanceamentoRepository rebalanceamentoRepo,
            IKafkaProducerService kafkaProducer)
        {
            _clienteRepo = clienteRepo;
            _contaGraficaRepo = contaGraficaRepo;
            _custodiaRepo = custodiaRepo;
            _cestaRepo = cestaRepo;
            _cotacaoService = cotacaoService;
            _rebalanceamentoRepo = rebalanceamentoRepo;
            _kafkaProducer = kafkaProducer;
        }

        public async Task RebalancearPorMudancaDeCestaAsync(long cestaAnteriorId, long cestaNovaId)
        {
            // RN-045: Disparado quando o administrador altera a composição da cesta
            var itensAntigos = (await _cestaRepo.GetItensByCestaIdAsync(cestaAnteriorId)).ToList();
            var itensNovos = (await _cestaRepo.GetItensByCestaIdAsync(cestaNovaId)).ToList();

            var tickersAntigos = itensAntigos.Select(i => i.Ticker.Trim()).ToList();
            var tickersNovos = itensNovos.Select(i => i.Ticker.Trim()).ToList();

            // RN-046: Identificar ativos que saíram e entraram
            var tickersRemovidos = tickersAntigos.Except(tickersNovos, StringComparer.OrdinalIgnoreCase).ToList();
            var tickersAdicionados = tickersNovos.Except(tickersAntigos, StringComparer.OrdinalIgnoreCase).ToList();
            var tickersMantidos = tickersAntigos.Intersect(tickersNovos, StringComparer.OrdinalIgnoreCase).ToList();

            // RN-024: Apenas clientes ativos participam
            var clientesAtivos = await _clienteRepo.GetClientesAtivosAsync();

            foreach (var cliente in clientesAtivos)
            {
                var conta = await _contaGraficaRepo.GetByClienteIdAsync(cliente.Id);
                if (conta == null) 
                    continue;

                var custodias = (await _custodiaRepo.GetByContaIdAsync(conta.Id))
                    .Where(c => !string.IsNullOrWhiteSpace(c.Ticker))
                    .ToList();

                decimal valorTotalVendas = 0m;
                var rebalanceamentos = new List<RebalanceamentoViewModel>();
                var vendasDetalhadas = new List<VendaDetalhada>();

                // RN-047: Vender toda a posição dos ativos que saíram
                foreach (var ticker in tickersRemovidos)
                {
                    var custodia = custodias.FirstOrDefault(c => 
                        c.Ticker.Trim().Equals(ticker, StringComparison.OrdinalIgnoreCase));

                    if (custodia == null || custodia.Quantidade == 0) 
                        continue;

                    var cotacaoAtual = await _cotacaoService.GetPrecoFechamentoMaisRecenteAsync(ticker) ?? 0m;
                    var valorVenda = custodia.Quantidade * cotacaoAtual;
                    valorTotalVendas += valorVenda;

                    // Registrar venda para cálculo de IR
                    vendasDetalhadas.Add(new VendaDetalhada
                    {
                        Ticker = ticker,
                        Quantidade = custodia.Quantidade,
                        PrecoVenda = cotacaoAtual,
                        PrecoMedio = custodia.PrecoMedio
                    });

                    // Registrar rebalanceamento
                    rebalanceamentos.Add(new RebalanceamentoViewModel
                    {
                        ClienteId = cliente.Id,
                        Tipo = EnumRebalanceamentoTipo.MUDANCA_CESTA,
                        TickerVendido = ticker,
                        ValorVenda = valorVenda,
                        DataRebalanceamento = DateTime.UtcNow
                    });

                    // Zerar a posição na custódia
                    custodia.Quantidade = 0;
                    custodia.DataUltimaAtualizacao = DateTime.UtcNow;
                    await _custodiaRepo.UpdateAsync(custodia);
                }

                // RN-048: Comprar os novos ativos
                if (valorTotalVendas > 0 && tickersAdicionados.Any())
                {
                    var percentualTotal = itensNovos
                        .Where(i => tickersAdicionados.Contains(i.Ticker.Trim(), StringComparer.OrdinalIgnoreCase))
                        .Sum(i => i.Percentual);

                    foreach (var ticker in tickersAdicionados)
                    {
                        var itemNovo = itensNovos.FirstOrDefault(i => 
                            i.Ticker.Trim().Equals(ticker, StringComparison.OrdinalIgnoreCase));
                        
                        if (itemNovo == null) 
                            continue;

                        var proporcao = itemNovo.Percentual / percentualTotal;
                        var valorParaCompra = valorTotalVendas * proporcao;

                        var cotacaoAtual = await _cotacaoService.GetPrecoFechamentoMaisRecenteAsync(ticker) ?? 0m;
                        if (cotacaoAtual == 0) 
                            continue;

                        var quantidade = (int)Math.Truncate(valorParaCompra / cotacaoAtual);
                        if (quantidade == 0) 
                            continue;

                        // Atualizar custódia
                        var custodia = custodias.FirstOrDefault(c => 
                            c.Ticker.Trim().Equals(ticker, StringComparison.OrdinalIgnoreCase));

                        if (custodia == null)
                        {
                            custodia = new CustodiaViewModel
                            {
                                ContaGraficaId = conta.Id,
                                Ticker = ticker,
                                Quantidade = quantidade,
                                PrecoMedio = cotacaoAtual,
                                DataUltimaAtualizacao = DateTime.UtcNow
                            };
                            await _custodiaRepo.AddAsync(custodia);
                        }
                        else
                        {
                            // RN-042: Calcular novo preço médio
                            var novoPrecoMedio = ((custodia.Quantidade * custodia.PrecoMedio) + 
                                                  (quantidade * cotacaoAtual)) / 
                                                 (custodia.Quantidade + quantidade);
                            
                            custodia.Quantidade += quantidade;
                            custodia.PrecoMedio = novoPrecoMedio;
                            custodia.DataUltimaAtualizacao = DateTime.UtcNow;
                            await _custodiaRepo.UpdateAsync(custodia);
                        }

                        rebalanceamentos.Add(new RebalanceamentoViewModel
                        {
                            ClienteId = cliente.Id,
                            Tipo = EnumRebalanceamentoTipo.MUDANCA_CESTA,
                            TickerComprado = ticker,
                            DataRebalanceamento = DateTime.UtcNow
                        });

                        // RN-053 a RN-056: Publicar IR dedo-duro para compra
                        await PublicarIRDedoDuroAsync(cliente, ticker, quantidade, cotacaoAtual, "COMPRA");
                    }
                }

                // RN-049: Rebalancear ativos que mudaram de percentual
                await RebalancearAtivosMantidosAsync(cliente, conta.Id, tickersMantidos, itensAntigos, itensNovos, rebalanceamentos, vendasDetalhadas);

                // Salvar rebalanceamentos
                if (rebalanceamentos.Any())
                {
                    await _rebalanceamentoRepo.AddRangeAsync(rebalanceamentos);
                }

                // RN-057 a RN-062: Calcular e publicar IR sobre vendas
                if (vendasDetalhadas.Any())
                {
                    await CalcularEPublicarIRVendasAsync(cliente, vendasDetalhadas);
                }
            }

            await _custodiaRepo.SaveChangesAsync();
            await _rebalanceamentoRepo.SaveChangesAsync();
        }

        public async Task RebalancearPorDesvioAsync(long clienteId, decimal limiarDesvio = 5m)
        {
            // RN-050 a RN-052: Rebalanceamento por desvio de proporção
            var cliente = await _clienteRepo.GetByIdAsync(clienteId);
            if (cliente == null || !cliente.Ativo) 
                return;

            var conta = await _contaGraficaRepo.GetByClienteIdAsync(clienteId);
            if (conta == null) 
                return;

            var cestaAtual = await _cestaRepo.GetAtualAsync();
            if (cestaAtual == null) 
                return;

            var itensCesta = (await _cestaRepo.GetItensByCestaIdAsync(cestaAtual.Id)).ToList();
            var custodias = (await _custodiaRepo.GetByContaIdAsync(conta.Id))
                .Where(c => !string.IsNullOrWhiteSpace(c.Ticker))
                .ToList();

            decimal valorTotalCarteira = 0m;
            var valoresPorAtivo = new Dictionary<string, decimal>();

            foreach (var custodia in custodias)
            {
                if (custodia.Quantidade == 0) 
                    continue;

                var cotacao = await _cotacaoService.GetPrecoFechamentoMaisRecenteAsync(custodia.Ticker) ?? 0m;
                var valor = custodia.Quantidade * cotacao;

                valorTotalCarteira += valor;
                valoresPorAtivo[custodia.Ticker.Trim()] = valor;
            }

            if (valorTotalCarteira == 0) 
                return;

            var rebalanceamentos = new List<RebalanceamentoViewModel>();
            var vendasDetalhadas = new List<VendaDetalhada>();

            // Identificar desvios
            foreach (var item in itensCesta)
            {
                var ticker = item.Ticker.Trim();
                var percentualAlvo = item.Percentual;
                var valorAlvo = valorTotalCarteira * (percentualAlvo / 100m);

                var valorAtual = valoresPorAtivo.ContainsKey(ticker) ? valoresPorAtivo[ticker] : 0m;
                var percentualAtual = valorTotalCarteira > 0 ? (valorAtual / valorTotalCarteira * 100m) : 0m;

                var desvio = Math.Abs(percentualAtual - percentualAlvo);

                if (desvio > limiarDesvio)
                {
                    var custodia = custodias.FirstOrDefault(c => 
                        c.Ticker.Trim().Equals(ticker, StringComparison.OrdinalIgnoreCase));

                    var cotacao = await _cotacaoService.GetPrecoFechamentoMaisRecenteAsync(ticker) ?? 0m;

                    if (cotacao == 0) 
                        continue;

                    if (percentualAtual > percentualAlvo)
                    {
                        // Sobre-alocado: vender excesso
                        var valorExcesso = valorAtual - valorAlvo;
                        var quantidadeVender = (int)Math.Truncate(valorExcesso / cotacao);

                        if (quantidadeVender > 0 && custodia != null)
                        {
                            vendasDetalhadas.Add(new VendaDetalhada
                            {
                                Ticker = ticker,
                                Quantidade = quantidadeVender,
                                PrecoVenda = cotacao,
                                PrecoMedio = custodia.PrecoMedio
                            });

                            custodia.Quantidade -= quantidadeVender;
                            custodia.DataUltimaAtualizacao = DateTime.UtcNow;
                            await _custodiaRepo.UpdateAsync(custodia);

                            rebalanceamentos.Add(new RebalanceamentoViewModel
                            {
                                ClienteId = clienteId,
                                Tipo = EnumRebalanceamentoTipo.DESVIO,
                                TickerVendido = ticker,
                                ValorVenda = quantidadeVender * cotacao,
                                DataRebalanceamento = DateTime.UtcNow
                            });
                        }
                    }
                    else
                    {
                        // Sub-alocado: comprar deficit
                        var valorDeficit = valorAlvo - valorAtual;
                        var quantidadeComprar = (int)Math.Truncate(valorDeficit / cotacao);

                        if (quantidadeComprar > 0)
                        {
                            if (custodia == null)
                            {
                                custodia = new CustodiaViewModel
                                {
                                    ContaGraficaId = conta.Id,
                                    Ticker = ticker,
                                    Quantidade = quantidadeComprar,
                                    PrecoMedio = cotacao,
                                    DataUltimaAtualizacao = DateTime.UtcNow
                                };
                                await _custodiaRepo.AddAsync(custodia);
                            }
                            else
                            {
                                var novoPrecoMedio = ((custodia.Quantidade * custodia.PrecoMedio) + 
                                                      (quantidadeComprar * cotacao)) / 
                                                     (custodia.Quantidade + quantidadeComprar);
                                
                                custodia.Quantidade += quantidadeComprar;
                                custodia.PrecoMedio = novoPrecoMedio;
                                custodia.DataUltimaAtualizacao = DateTime.UtcNow;
                                await _custodiaRepo.UpdateAsync(custodia);
                            }

                            rebalanceamentos.Add(new RebalanceamentoViewModel
                            {
                                ClienteId = clienteId,
                                Tipo = EnumRebalanceamentoTipo.DESVIO,
                                TickerComprado = ticker,
                                DataRebalanceamento = DateTime.UtcNow
                            });

                            await PublicarIRDedoDuroAsync(cliente, ticker, quantidadeComprar, cotacao, "COMPRA");
                        }
                    }
                }
            }

            if (rebalanceamentos.Any())
            {
                await _rebalanceamentoRepo.AddRangeAsync(rebalanceamentos);
            }

            if (vendasDetalhadas.Any())
            {
                await CalcularEPublicarIRVendasAsync(cliente, vendasDetalhadas);
            }

            await _custodiaRepo.SaveChangesAsync();
            await _rebalanceamentoRepo.SaveChangesAsync();
        }

        private async Task RebalancearAtivosMantidosAsync(
            ClienteViewModel cliente,
            long contaId,
            List<string> tickersMantidos,
            List<ItemCestaViewModel> itensAntigos,
            List<ItemCestaViewModel> itensNovos,
            List<RebalanceamentoViewModel> rebalanceamentos,
            List<VendaDetalhada> vendasDetalhadas)
        {
            foreach (var ticker in tickersMantidos)
            {
                var itemAntigo = itensAntigos.FirstOrDefault(i => 
                    i.Ticker.Trim().Equals(ticker, StringComparison.OrdinalIgnoreCase));

                var itemNovo = itensNovos.FirstOrDefault(i => 
                    i.Ticker.Trim().Equals(ticker, StringComparison.OrdinalIgnoreCase));

                if (itemAntigo == null || itemNovo == null) 
                    continue;

                if (itemAntigo.Percentual == itemNovo.Percentual) 
                    continue;

                var custodia = await _custodiaRepo.GetByContaAndTickerAsync(contaId, ticker);
                if (custodia == null || custodia.Quantidade == 0) 
                    continue;

                var cotacaoAtual = await _cotacaoService.GetPrecoFechamentoMaisRecenteAsync(ticker) ?? 0m;
                if (cotacaoAtual == 0) 
                    continue;

                var valorAtual = custodia.Quantidade * cotacaoAtual;

                // Calcular valor alvo baseado no novo percentual
                // Aqui usamos o valor atual como base para o ajuste proporcional
                var fatorAjuste = itemNovo.Percentual / itemAntigo.Percentual;
                var quantidadeAlvo = (int)Math.Truncate(custodia.Quantidade * fatorAjuste);

                if (quantidadeAlvo > custodia.Quantidade)
                {
                    // Comprar mais
                    var quantidadeComprar = quantidadeAlvo - custodia.Quantidade;
                    var novoPrecoMedio = ((custodia.Quantidade * custodia.PrecoMedio) + 
                                          (quantidadeComprar * cotacaoAtual)) / 
                                         (custodia.Quantidade + quantidadeComprar);
                    
                    custodia.Quantidade += quantidadeComprar;
                    custodia.PrecoMedio = novoPrecoMedio;
                    custodia.DataUltimaAtualizacao = DateTime.UtcNow;
                    await _custodiaRepo.UpdateAsync(custodia);

                    rebalanceamentos.Add(new RebalanceamentoViewModel
                    {
                        ClienteId = cliente.Id,
                        Tipo = EnumRebalanceamentoTipo.MUDANCA_CESTA,
                        TickerComprado = ticker,
                        DataRebalanceamento = DateTime.UtcNow
                    });

                    await PublicarIRDedoDuroAsync(cliente, ticker, quantidadeComprar, cotacaoAtual, "COMPRA");
                }
                else if (quantidadeAlvo < custodia.Quantidade)
                {
                    // Vender excesso
                    var quantidadeVender = custodia.Quantidade - quantidadeAlvo;
                    var valorVenda = quantidadeVender * cotacaoAtual;

                    vendasDetalhadas.Add(new VendaDetalhada
                    {
                        Ticker = ticker,
                        Quantidade = quantidadeVender,
                        PrecoVenda = cotacaoAtual,
                        PrecoMedio = custodia.PrecoMedio
                    });

                    custodia.Quantidade -= quantidadeVender;
                    custodia.DataUltimaAtualizacao = DateTime.UtcNow;
                    await _custodiaRepo.UpdateAsync(custodia);

                    rebalanceamentos.Add(new RebalanceamentoViewModel
                    {
                        ClienteId = cliente.Id,
                        Tipo = EnumRebalanceamentoTipo.MUDANCA_CESTA,
                        TickerVendido = ticker,
                        ValorVenda = valorVenda,
                        DataRebalanceamento = DateTime.UtcNow
                    });
                }
            }
        }

        private async Task PublicarIRDedoDuroAsync(
            ClienteViewModel cliente,
            string ticker,
            int quantidade,
            decimal precoUnitario,
            string tipoOperacao)
        {
            // RN-053 a RN-056: IR Dedo-Duro
            var valorOperacao = quantidade * precoUnitario;
            var valorIR = valorOperacao * 0.00005m; // 0.005%

            var mensagem = new
            {
                tipo = "IR_DEDO_DURO",
                clienteId = cliente.Id,
                cpf = cliente.CPF,
                ticker = ticker,
                tipoOperacao = tipoOperacao,
                quantidade = quantidade,
                precoUnitario = precoUnitario,
                valorOperacao = valorOperacao,
                aliquota = 0.00005m,
                valorIR = Math.Round(valorIR, 2),
                dataOperacao = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(mensagem);
            await _kafkaProducer.PublishAsync("ir-events", json);
        }

        private async Task CalcularEPublicarIRVendasAsync(
            ClienteViewModel cliente,
            List<VendaDetalhada> vendasDetalhadas)
        {
            // RN-057: Somar todas as vendas do mês
            var totalVendas = vendasDetalhadas.Sum(v => v.Quantidade * v.PrecoVenda);

            // RN-058: Verificar isenção
            if (totalVendas <= 20000m)
            {
                return; // Isento
            }

            // RN-059 a RN-061: Calcular lucro
            decimal lucroTotal = 0m;
            var detalhes = new List<object>();

            foreach (var venda in vendasDetalhadas)
            {
                var lucro = venda.Quantidade * (venda.PrecoVenda - venda.PrecoMedio);
                lucroTotal += lucro;

                detalhes.Add(new
                {
                    ticker = venda.Ticker,
                    quantidade = venda.Quantidade,
                    precoVenda = venda.PrecoVenda,
                    precoMedio = venda.PrecoMedio,
                    lucro = Math.Round(lucro, 2)
                });
            }

            if (lucroTotal <= 0)
            {
                return; // Sem lucro, sem IR
            }

            var valorIR = lucroTotal * 0.20m; // 20%

            var mensagem = new
            {
                tipo = "IR_VENDA",
                clienteId = cliente.Id,
                cpf = cliente.CPF,
                mesReferencia = DateTime.UtcNow.ToString("yyyy-MM"),
                totalVendasMes = Math.Round(totalVendas, 2),
                lucroLiquido = Math.Round(lucroTotal, 2),
                aliquota = 0.20m,
                valorIR = Math.Round(valorIR, 2),
                detalhes = detalhes,
                dataCalculo = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(mensagem);
            await _kafkaProducer.PublishAsync("ir-events", json);
        }

        private class VendaDetalhada
        {
            public string Ticker { get; set; } = string.Empty;
            public int Quantidade { get; set; }
            public decimal PrecoVenda { get; set; }
            public decimal PrecoMedio { get; set; }
        }
    }
}
