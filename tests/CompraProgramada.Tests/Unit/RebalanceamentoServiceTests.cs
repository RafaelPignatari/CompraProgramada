using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CompraProgramada.Models;
using CompraProgramadaWebApp.Data.Repositories;
using CompraProgramadaWebApp.Models.Enums;
using CompraProgramadaWebApp.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace CompraProgramada.Tests.Unit
{
    public class RebalanceamentoServiceTests
    {
        private readonly Mock<IClienteRepository> _clienteRepoMock;
        private readonly Mock<IContaGraficaRepository> _contaGraficaRepoMock;
        private readonly Mock<ICustodiaRepository> _custodiaRepoMock;
        private readonly Mock<ICestaRepository> _cestaRepoMock;
        private readonly Mock<ICotacaoService> _cotacaoServiceMock;
        private readonly Mock<IRebalanceamentoRepository> _rebalanceamentoRepoMock;
        private readonly Mock<IKafkaProducerService> _kafkaProducerMock;
        private readonly RebalanceamentoService _service;

        public RebalanceamentoServiceTests()
        {
            _clienteRepoMock = new Mock<IClienteRepository>();
            _contaGraficaRepoMock = new Mock<IContaGraficaRepository>();
            _custodiaRepoMock = new Mock<ICustodiaRepository>();
            _cestaRepoMock = new Mock<ICestaRepository>();
            _cotacaoServiceMock = new Mock<ICotacaoService>();
            _rebalanceamentoRepoMock = new Mock<IRebalanceamentoRepository>();
            _kafkaProducerMock = new Mock<IKafkaProducerService>();

            _service = new RebalanceamentoService(
                _clienteRepoMock.Object,
                _contaGraficaRepoMock.Object,
                _custodiaRepoMock.Object,
                _cestaRepoMock.Object,
                _cotacaoServiceMock.Object,
                _rebalanceamentoRepoMock.Object,
                _kafkaProducerMock.Object
            );
        }

        #region RebalancearPorMudancaDeCestaAsync Tests

        [Fact]
        public async Task RebalancearPorMudancaDeCestaAsync_RemoveTickerFromBasket_SellsEntirePosition()
        {
            // Arrange
            var cestaAnteriorId = 1L;
            var cestaNovaId = 2L;

            var itensAntigos = new List<ItemCestaViewModel>
            {
                new ItemCestaViewModel { CestaId = cestaAnteriorId, Ticker = "PETR4", Percentual = 50 },
                new ItemCestaViewModel { CestaId = cestaAnteriorId, Ticker = "VALE3", Percentual = 50 }
            };

            var itensNovos = new List<ItemCestaViewModel>
            {
                new ItemCestaViewModel { CestaId = cestaNovaId, Ticker = "PETR4", Percentual = 100 }
                // VALE3 foi removido
            };

            var cliente = new ClienteViewModel { Id = 1, Nome = "Cliente Teste", Ativo = true };
            var conta = new ContasGraficasViewModel { Id = 10, ClienteId = 1 };

            var custodias = new List<CustodiaViewModel>
            {
                new CustodiaViewModel
                {
                    Id = 1,
                    ContaGraficaId = 10,
                    Ticker = "PETR4",
                    Quantidade = 100,
                    PrecoMedio = 30.00m
                },
                new CustodiaViewModel
                {
                    Id = 2,
                    ContaGraficaId = 10,
                    Ticker = "VALE3",
                    Quantidade = 50,
                    PrecoMedio = 60.00m
                }
            };

            _cestaRepoMock.Setup(r => r.GetItensByCestaIdAsync(cestaAnteriorId))
                .ReturnsAsync(itensAntigos);
            _cestaRepoMock.Setup(r => r.GetItensByCestaIdAsync(cestaNovaId))
                .ReturnsAsync(itensNovos);
            _clienteRepoMock.Setup(r => r.GetClientesAtivosAsync())
                .ReturnsAsync(new List<ClienteViewModel> { cliente });
            _contaGraficaRepoMock.Setup(r => r.GetByClienteIdAsync(1))
                .ReturnsAsync(conta);
            _custodiaRepoMock.Setup(r => r.GetByContaIdAsync(10))
                .ReturnsAsync(custodias);

            // Cotações
            _cotacaoServiceMock.Setup(s => s.GetPrecoFechamentoMaisRecenteAsync("VALE3"))
                .ReturnsAsync(65.00m); // VALE3 valorizou

            // Act
            await _service.RebalancearPorMudancaDeCestaAsync(cestaAnteriorId, cestaNovaId);

            // Assert
            // Verificar que VALE3 foi zerada
            var vale3Custodia = custodias.First(c => c.Ticker == "VALE3");
            vale3Custodia.Quantidade.Should().Be(0);

            // Verificar que rebalanceamento foi registrado
            _rebalanceamentoRepoMock.Verify(r => r.AddRangeAsync(
                It.Is<IEnumerable<RebalanceamentoViewModel>>(list =>
                    list.Any(rb => rb.TickerVendido == "VALE3" &&
                                   rb.ValorVenda == 50 * 65.00m &&
                                   rb.Tipo == EnumRebalanceamentoTipo.MUDANCA_CESTA)
                )
            ), Times.Once);

            _custodiaRepoMock.Verify(r => r.UpdateAsync(It.Is<CustodiaViewModel>(c =>
                c.Ticker == "VALE3" && c.Quantidade == 0
            )), Times.Once);

            _custodiaRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
            _rebalanceamentoRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task RebalancearPorMudancaDeCestaAsync_AddNewTickerToBasket_BuysWithSaleProceeds()
        {
            // Arrange
            var cestaAnteriorId = 1L;
            var cestaNovaId = 2L;

            var itensAntigos = new List<ItemCestaViewModel>
            {
                new ItemCestaViewModel { CestaId = cestaAnteriorId, Ticker = "PETR4", Percentual = 50 },
                new ItemCestaViewModel { CestaId = cestaAnteriorId, Ticker = "VALE3", Percentual = 50 }
            };

            var itensNovos = new List<ItemCestaViewModel>
            {
                new ItemCestaViewModel { CestaId = cestaNovaId, Ticker = "PETR4", Percentual = 50 },
                new ItemCestaViewModel { CestaId = cestaNovaId, Ticker = "BBDC4", Percentual = 50 } // Novo, substitui VALE3
            };

            var cliente = new ClienteViewModel { Id = 1, Nome = "Cliente Teste", Ativo = true, CPF = "12345678900" };
            var conta = new ContasGraficasViewModel { Id = 10, ClienteId = 1 };

            var custodias = new List<CustodiaViewModel>
            {
                new CustodiaViewModel
                {
                    Id = 1,
                    ContaGraficaId = 10,
                    Ticker = "PETR4",
                    Quantidade = 100,
                    PrecoMedio = 30.00m
                },
                new CustodiaViewModel
                {
                    Id = 2,
                    ContaGraficaId = 10,
                    Ticker = "VALE3",
                    Quantidade = 50,
                    PrecoMedio = 60.00m
                }
            };

            var custodiaAdicionada = false;

            _cestaRepoMock.Setup(r => r.GetItensByCestaIdAsync(cestaAnteriorId))
                .ReturnsAsync(itensAntigos);
            _cestaRepoMock.Setup(r => r.GetItensByCestaIdAsync(cestaNovaId))
                .ReturnsAsync(itensNovos);
            _clienteRepoMock.Setup(r => r.GetClientesAtivosAsync())
                .ReturnsAsync(new List<ClienteViewModel> { cliente });
            _contaGraficaRepoMock.Setup(r => r.GetByClienteIdAsync(1))
                .ReturnsAsync(conta);
            _custodiaRepoMock.Setup(r => r.GetByContaIdAsync(10))
                .ReturnsAsync(custodias);

            // Cotações
            _cotacaoServiceMock.Setup(s => s.GetPrecoFechamentoMaisRecenteAsync("PETR4"))
                .ReturnsAsync(35.00m);
            _cotacaoServiceMock.Setup(s => s.GetPrecoFechamentoMaisRecenteAsync("VALE3"))
                .ReturnsAsync(65.00m);
            _cotacaoServiceMock.Setup(s => s.GetPrecoFechamentoMaisRecenteAsync("BBDC4"))
                .ReturnsAsync(25.00m);

            _custodiaRepoMock.Setup(r => r.AddAsync(It.IsAny<CustodiaViewModel>()))
                .Callback<CustodiaViewModel>(c => custodiaAdicionada = true);

            // Act
            await _service.RebalancearPorMudancaDeCestaAsync(cestaAnteriorId, cestaNovaId);

            // Assert
            // Verificar que VALE3 foi vendido e BBDC4 foi comprado com os recursos da venda
            _rebalanceamentoRepoMock.Verify(r => r.AddRangeAsync(
                It.Is<IEnumerable<RebalanceamentoViewModel>>(list =>
                    list.Any(rb => rb.TickerVendido == "VALE3" && rb.ValorVenda == 50 * 65.00m) &&
                    list.Any(rb => rb.TickerComprado == "BBDC4")
                )
            ), Times.Once);

            // Verificar que houve compra de BBDC4 com recursos da venda de VALE3
            custodiaAdicionada.Should().BeTrue();
        }

        [Fact]
        public async Task RebalancearPorMudancaDeCestaAsync_NoActiveClients_DoesNothing()
        {
            // Arrange
            var cestaAnteriorId = 1L;
            var cestaNovaId = 2L;

            _cestaRepoMock.Setup(r => r.GetItensByCestaIdAsync(It.IsAny<long>()))
                .ReturnsAsync(new List<ItemCestaViewModel>());
            _clienteRepoMock.Setup(r => r.GetClientesAtivosAsync())
                .ReturnsAsync(new List<ClienteViewModel>()); // Nenhum cliente ativo

            // Act
            await _service.RebalancearPorMudancaDeCestaAsync(cestaAnteriorId, cestaNovaId);

            // Assert
            _contaGraficaRepoMock.Verify(r => r.GetByClienteIdAsync(It.IsAny<long>()), Times.Never);
            _custodiaRepoMock.Verify(r => r.UpdateAsync(It.IsAny<CustodiaViewModel>()), Times.Never);
            _rebalanceamentoRepoMock.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<RebalanceamentoViewModel>>()), Times.Never);
        }

        [Fact]
        public async Task RebalancearPorMudancaDeCestaAsync_ClientWithoutAccount_SkipsClient()
        {
            // Arrange
            var cestaAnteriorId = 1L;
            var cestaNovaId = 2L;
            var cliente = new ClienteViewModel { Id = 1, Nome = "Cliente Teste", Ativo = true };

            _cestaRepoMock.Setup(r => r.GetItensByCestaIdAsync(It.IsAny<long>()))
                .ReturnsAsync(new List<ItemCestaViewModel>());
            _clienteRepoMock.Setup(r => r.GetClientesAtivosAsync())
                .ReturnsAsync(new List<ClienteViewModel> { cliente });
            _contaGraficaRepoMock.Setup(r => r.GetByClienteIdAsync(1))
                .ReturnsAsync((ContasGraficasViewModel?)null); // Cliente sem conta

            // Act
            await _service.RebalancearPorMudancaDeCestaAsync(cestaAnteriorId, cestaNovaId);

            // Assert
            _custodiaRepoMock.Verify(r => r.GetByContaIdAsync(It.IsAny<long>()), Times.Never);
        }

        [Fact]
        public async Task RebalancearPorMudancaDeCestaAsync_PublishesIRDedoDuroForPurchases()
        {
            // Arrange
            var cestaAnteriorId = 1L;
            var cestaNovaId = 2L;

            var itensAntigos = new List<ItemCestaViewModel>
            {
                new ItemCestaViewModel { CestaId = cestaAnteriorId, Ticker = "PETR4", Percentual = 100 }
            };

            var itensNovos = new List<ItemCestaViewModel>
            {
                new ItemCestaViewModel { CestaId = cestaNovaId, Ticker = "VALE3", Percentual = 100 } // Trocou completamente
            };

            var cliente = new ClienteViewModel
            {
                Id = 1,
                Nome = "Cliente Teste",
                CPF = "12345678900",
                Ativo = true
            };
            var conta = new ContasGraficasViewModel { Id = 10, ClienteId = 1 };

            var custodias = new List<CustodiaViewModel>
            {
                new CustodiaViewModel
                {
                    Id = 1,
                    ContaGraficaId = 10,
                    Ticker = "PETR4",
                    Quantidade = 100,
                    PrecoMedio = 30.00m
                }
            };

            _cestaRepoMock.Setup(r => r.GetItensByCestaIdAsync(cestaAnteriorId))
                .ReturnsAsync(itensAntigos);
            _cestaRepoMock.Setup(r => r.GetItensByCestaIdAsync(cestaNovaId))
                .ReturnsAsync(itensNovos);
            _clienteRepoMock.Setup(r => r.GetClientesAtivosAsync())
                .ReturnsAsync(new List<ClienteViewModel> { cliente });
            _contaGraficaRepoMock.Setup(r => r.GetByClienteIdAsync(1))
                .ReturnsAsync(conta);
            _custodiaRepoMock.Setup(r => r.GetByContaIdAsync(10))
                .ReturnsAsync(custodias);

            _cotacaoServiceMock.Setup(s => s.GetPrecoFechamentoMaisRecenteAsync("PETR4"))
                .ReturnsAsync(35.00m);
            _cotacaoServiceMock.Setup(s => s.GetPrecoFechamentoMaisRecenteAsync("VALE3"))
                .ReturnsAsync(70.00m);

            // Act
            await _service.RebalancearPorMudancaDeCestaAsync(cestaAnteriorId, cestaNovaId);

            // Assert
            // Verificar que publicou IR dedo-duro para compra de VALE3
            _kafkaProducerMock.Verify(k => k.PublishAsync(
                It.Is<string>(topic => topic == "ir-events"),
                It.IsAny<string>()
            ), Times.AtLeastOnce);
        }

        #endregion

        #region RebalancearPorDesvioAsync Tests

        [Fact]
        public async Task RebalancearPorDesvioAsync_InactiveClient_DoesNothing()
        {
            // Arrange
            var clienteId = 1L;
            var cliente = new ClienteViewModel { Id = 1, Nome = "Cliente Teste", Ativo = false };

            _clienteRepoMock.Setup(r => r.GetByIdAsync(clienteId))
                .ReturnsAsync(cliente);

            // Act
            await _service.RebalancearPorDesvioAsync(clienteId);

            // Assert
            _contaGraficaRepoMock.Verify(r => r.GetByClienteIdAsync(It.IsAny<long>()), Times.Never);
            _custodiaRepoMock.Verify(r => r.UpdateAsync(It.IsAny<CustodiaViewModel>()), Times.Never);
        }

        [Fact]
        public async Task RebalancearPorDesvioAsync_ClientNotFound_DoesNothing()
        {
            // Arrange
            var clienteId = 1L;
            _clienteRepoMock.Setup(r => r.GetByIdAsync(clienteId))
                .ReturnsAsync((ClienteViewModel?)null);

            // Act
            await _service.RebalancearPorDesvioAsync(clienteId);

            // Assert
            _contaGraficaRepoMock.Verify(r => r.GetByClienteIdAsync(It.IsAny<long>()), Times.Never);
        }

        [Fact]
        public async Task RebalancearPorDesvioAsync_ClientWithoutAccount_DoesNothing()
        {
            // Arrange
            var clienteId = 1L;
            var cliente = new ClienteViewModel { Id = 1, Nome = "Cliente Teste", Ativo = true };

            _clienteRepoMock.Setup(r => r.GetByIdAsync(clienteId))
                .ReturnsAsync(cliente);
            _contaGraficaRepoMock.Setup(r => r.GetByClienteIdAsync(clienteId))
                .ReturnsAsync((ContasGraficasViewModel?)null);

            // Act
            await _service.RebalancearPorDesvioAsync(clienteId);

            // Assert
            _cestaRepoMock.Verify(r => r.GetAtualAsync(), Times.Never);
        }

        [Fact]
        public async Task RebalancearPorDesvioAsync_NoActiveBasket_DoesNothing()
        {
            // Arrange
            var clienteId = 1L;
            var cliente = new ClienteViewModel { Id = 1, Nome = "Cliente Teste", Ativo = true };
            var conta = new ContasGraficasViewModel { Id = 10, ClienteId = 1 };

            _clienteRepoMock.Setup(r => r.GetByIdAsync(clienteId))
                .ReturnsAsync(cliente);
            _contaGraficaRepoMock.Setup(r => r.GetByClienteIdAsync(clienteId))
                .ReturnsAsync(conta);
            _cestaRepoMock.Setup(r => r.GetAtualAsync())
                .ReturnsAsync((CestaRecomendacaoViewModel?)null);

            // Act
            await _service.RebalancearPorDesvioAsync(clienteId);

            // Assert
            _custodiaRepoMock.Verify(r => r.GetByContaIdAsync(It.IsAny<long>()), Times.Never);
        }

        [Fact]
        public async Task RebalancearPorDesvioAsync_OverallocatedAsset_SellsExcess()
        {
            // Arrange
            var clienteId = 1L;
            var cliente = new ClienteViewModel
            {
                Id = 1,
                Nome = "Cliente Teste",
                CPF = "12345678900",
                Ativo = true
            };
            var conta = new ContasGraficasViewModel { Id = 10, ClienteId = 1 };
            var cesta = new CestaRecomendacaoViewModel { Id = 1, Nome = "Cesta Teste", Ativa = true };

            var itensCesta = new List<ItemCestaViewModel>
            {
                new ItemCestaViewModel { CestaId = 1, Ticker = "PETR4", Percentual = 50 },
                new ItemCestaViewModel { CestaId = 1, Ticker = "VALE3", Percentual = 50 }
            };

            // Cliente tem 70% em PETR4 e 30% em VALE3 (PETR4 está sobre-alocado)
            var custodias = new List<CustodiaViewModel>
            {
                new CustodiaViewModel
                {
                    Id = 1,
                    ContaGraficaId = 10,
                    Ticker = "PETR4",
                    Quantidade = 100, // 100 * 35 = 3500 (70%)
                    PrecoMedio = 30.00m
                },
                new CustodiaViewModel
                {
                    Id = 2,
                    ContaGraficaId = 10,
                    Ticker = "VALE3",
                    Quantidade = 25, // 25 * 60 = 1500 (30%)
                    PrecoMedio = 55.00m
                }
            };

            _clienteRepoMock.Setup(r => r.GetByIdAsync(clienteId))
                .ReturnsAsync(cliente);
            _contaGraficaRepoMock.Setup(r => r.GetByClienteIdAsync(clienteId))
                .ReturnsAsync(conta);
            _cestaRepoMock.Setup(r => r.GetAtualAsync())
                .ReturnsAsync(cesta);
            _cestaRepoMock.Setup(r => r.GetItensByCestaIdAsync(1))
                .ReturnsAsync(itensCesta);
            _custodiaRepoMock.Setup(r => r.GetByContaIdAsync(10))
                .ReturnsAsync(custodias);

            // Cotações
            _cotacaoServiceMock.Setup(s => s.GetPrecoFechamentoMaisRecenteAsync("PETR4"))
                .ReturnsAsync(35.00m);
            _cotacaoServiceMock.Setup(s => s.GetPrecoFechamentoMaisRecenteAsync("VALE3"))
                .ReturnsAsync(60.00m);

            // Act
            await _service.RebalancearPorDesvioAsync(clienteId, limiarDesvio: 5m);

            // Assert
            // Desvio PETR4: |70 - 50| = 20% > 5% -> Deve vender
            // Valor total: 5000, Valor alvo PETR4: 2500, Valor atual PETR4: 3500
            // Excesso: 1000, Quantidade a vender: 1000/35 = 28 ações

            var petr4Custodia = custodias.First(c => c.Ticker == "PETR4");
            petr4Custodia.Quantidade.Should().Be(72); // 100 - 28

            _custodiaRepoMock.Verify(r => r.UpdateAsync(It.Is<CustodiaViewModel>(c =>
                c.Ticker == "PETR4" && c.Quantidade == 72
            )), Times.Once);

            _rebalanceamentoRepoMock.Verify(r => r.AddRangeAsync(
                It.Is<IEnumerable<RebalanceamentoViewModel>>(list =>
                    list.Any(rb => rb.TickerVendido == "PETR4" &&
                                   rb.Tipo == EnumRebalanceamentoTipo.DESVIO)
                )
            ), Times.Once);
        }

        [Fact]
        public async Task RebalancearPorDesvioAsync_UnderallocatedAsset_BuysDeficit()
        {
            // Arrange
            var clienteId = 1L;
            var cliente = new ClienteViewModel
            {
                Id = 1,
                Nome = "Cliente Teste",
                CPF = "12345678900",
                Ativo = true
            };
            var conta = new ContasGraficasViewModel { Id = 10, ClienteId = 1 };
            var cesta = new CestaRecomendacaoViewModel { Id = 1, Nome = "Cesta Teste", Ativa = true };

            var itensCesta = new List<ItemCestaViewModel>
            {
                new ItemCestaViewModel { CestaId = 1, Ticker = "PETR4", Percentual = 50 },
                new ItemCestaViewModel { CestaId = 1, Ticker = "VALE3", Percentual = 50 }
            };

            // Cliente tem 70% em PETR4 e 30% em VALE3 (VALE3 está sub-alocado)
            var custodias = new List<CustodiaViewModel>
            {
                new CustodiaViewModel
                {
                    Id = 1,
                    ContaGraficaId = 10,
                    Ticker = "PETR4",
                    Quantidade = 100, // 100 * 35 = 3500 (70%)
                    PrecoMedio = 30.00m
                },
                new CustodiaViewModel
                {
                    Id = 2,
                    ContaGraficaId = 10,
                    Ticker = "VALE3",
                    Quantidade = 25, // 25 * 60 = 1500 (30%)
                    PrecoMedio = 55.00m
                }
            };

            _clienteRepoMock.Setup(r => r.GetByIdAsync(clienteId))
                .ReturnsAsync(cliente);
            _contaGraficaRepoMock.Setup(r => r.GetByClienteIdAsync(clienteId))
                .ReturnsAsync(conta);
            _cestaRepoMock.Setup(r => r.GetAtualAsync())
                .ReturnsAsync(cesta);
            _cestaRepoMock.Setup(r => r.GetItensByCestaIdAsync(1))
                .ReturnsAsync(itensCesta);
            _custodiaRepoMock.Setup(r => r.GetByContaIdAsync(10))
                .ReturnsAsync(custodias);

            // Cotações
            _cotacaoServiceMock.Setup(s => s.GetPrecoFechamentoMaisRecenteAsync("PETR4"))
                .ReturnsAsync(35.00m);
            _cotacaoServiceMock.Setup(s => s.GetPrecoFechamentoMaisRecenteAsync("VALE3"))
                .ReturnsAsync(60.00m);

            // Act
            await _service.RebalancearPorDesvioAsync(clienteId, limiarDesvio: 5m);

            // Assert
            // Desvio VALE3: |30 - 50| = 20% > 5% -> Deve comprar
            // Valor total: 5000, Valor alvo VALE3: 2500, Valor atual VALE3: 1500
            // Deficit: 1000, Quantidade a comprar: 1000/60 = 16 ações

            var vale3Custodia = custodias.First(c => c.Ticker == "VALE3");
            vale3Custodia.Quantidade.Should().Be(41); // 25 + 16

            // Calcular novo preço médio: ((25 * 55) + (16 * 60)) / (25 + 16)
            var novoPrecoMedio = ((25 * 55.00m) + (16 * 60.00m)) / 41;
            vale3Custodia.PrecoMedio.Should().BeApproximately(novoPrecoMedio, 0.01m);

            _custodiaRepoMock.Verify(r => r.UpdateAsync(It.Is<CustodiaViewModel>(c =>
                c.Ticker == "VALE3" && c.Quantidade == 41
            )), Times.Once);

            _rebalanceamentoRepoMock.Verify(r => r.AddRangeAsync(
                It.Is<IEnumerable<RebalanceamentoViewModel>>(list =>
                    list.Any(rb => rb.TickerComprado == "VALE3" &&
                                   rb.Tipo == EnumRebalanceamentoTipo.DESVIO)
                )
            ), Times.Once);

            // Verificar publicação de IR dedo-duro
            _kafkaProducerMock.Verify(k => k.PublishAsync(
                It.Is<string>(topic => topic == "ir-events"),
                It.IsAny<string>()
            ), Times.AtLeastOnce);
        }

        [Fact]
        public async Task RebalancearPorDesvioAsync_DeviationBelowThreshold_DoesNotRebalance()
        {
            // Arrange
            var clienteId = 1L;
            var cliente = new ClienteViewModel { Id = 1, Nome = "Cliente Teste", Ativo = true };
            var conta = new ContasGraficasViewModel { Id = 10, ClienteId = 1 };
            var cesta = new CestaRecomendacaoViewModel { Id = 1, Nome = "Cesta Teste", Ativa = true };

            var itensCesta = new List<ItemCestaViewModel>
            {
                new ItemCestaViewModel { CestaId = 1, Ticker = "PETR4", Percentual = 50 },
                new ItemCestaViewModel { CestaId = 1, Ticker = "VALE3", Percentual = 50 }
            };

            // Cliente tem 52% em PETR4 e 48% em VALE3 (desvio de 2%, abaixo de 5%)
            var custodias = new List<CustodiaViewModel>
            {
                new CustodiaViewModel
                {
                    Id = 1,
                    ContaGraficaId = 10,
                    Ticker = "PETR4",
                    Quantidade = 100, // 100 * 26 = 2600 (52%)
                    PrecoMedio = 25.00m
                },
                new CustodiaViewModel
                {
                    Id = 2,
                    ContaGraficaId = 10,
                    Ticker = "VALE3",
                    Quantidade = 40, // 40 * 60 = 2400 (48%)
                    PrecoMedio = 55.00m
                }
            };

            _clienteRepoMock.Setup(r => r.GetByIdAsync(clienteId))
                .ReturnsAsync(cliente);
            _contaGraficaRepoMock.Setup(r => r.GetByClienteIdAsync(clienteId))
                .ReturnsAsync(conta);
            _cestaRepoMock.Setup(r => r.GetAtualAsync())
                .ReturnsAsync(cesta);
            _cestaRepoMock.Setup(r => r.GetItensByCestaIdAsync(1))
                .ReturnsAsync(itensCesta);
            _custodiaRepoMock.Setup(r => r.GetByContaIdAsync(10))
                .ReturnsAsync(custodias);

            // Cotações
            _cotacaoServiceMock.Setup(s => s.GetPrecoFechamentoMaisRecenteAsync("PETR4"))
                .ReturnsAsync(26.00m);
            _cotacaoServiceMock.Setup(s => s.GetPrecoFechamentoMaisRecenteAsync("VALE3"))
                .ReturnsAsync(60.00m);

            // Act
            await _service.RebalancearPorDesvioAsync(clienteId, limiarDesvio: 5m);

            // Assert
            // Não deve fazer rebalanceamento
            _custodiaRepoMock.Verify(r => r.UpdateAsync(It.IsAny<CustodiaViewModel>()), Times.Never);
            _rebalanceamentoRepoMock.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<RebalanceamentoViewModel>>()), Times.Never);
        }

        [Fact]
        public async Task RebalancearPorDesvioAsync_EmptyPortfolio_DoesNothing()
        {
            // Arrange
            var clienteId = 1L;
            var cliente = new ClienteViewModel { Id = 1, Nome = "Cliente Teste", Ativo = true };
            var conta = new ContasGraficasViewModel { Id = 10, ClienteId = 1 };
            var cesta = new CestaRecomendacaoViewModel { Id = 1, Nome = "Cesta Teste", Ativa = true };

            _clienteRepoMock.Setup(r => r.GetByIdAsync(clienteId))
                .ReturnsAsync(cliente);
            _contaGraficaRepoMock.Setup(r => r.GetByClienteIdAsync(clienteId))
                .ReturnsAsync(conta);
            _cestaRepoMock.Setup(r => r.GetAtualAsync())
                .ReturnsAsync(cesta);
            _cestaRepoMock.Setup(r => r.GetItensByCestaIdAsync(1))
                .ReturnsAsync(new List<ItemCestaViewModel>());
            _custodiaRepoMock.Setup(r => r.GetByContaIdAsync(10))
                .ReturnsAsync(new List<CustodiaViewModel>()); // Carteira vazia

            // Act
            await _service.RebalancearPorDesvioAsync(clienteId);

            // Assert
            _custodiaRepoMock.Verify(r => r.UpdateAsync(It.IsAny<CustodiaViewModel>()), Times.Never);
            _rebalanceamentoRepoMock.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<RebalanceamentoViewModel>>()), Times.Never);
        }

        [Fact]
        public async Task RebalancearPorDesvioAsync_CreatesNewCustodyForUnderallocatedAsset()
        {
            // Arrange
            var clienteId = 1L;
            var cliente = new ClienteViewModel
            {
                Id = 1,
                Nome = "Cliente Teste",
                CPF = "12345678900",
                Ativo = true
            };
            var conta = new ContasGraficasViewModel { Id = 10, ClienteId = 1 };
            var cesta = new CestaRecomendacaoViewModel { Id = 1, Nome = "Cesta Teste", Ativa = true };

            var itensCesta = new List<ItemCestaViewModel>
            {
                new ItemCestaViewModel { CestaId = 1, Ticker = "PETR4", Percentual = 50 },
                new ItemCestaViewModel { CestaId = 1, Ticker = "VALE3", Percentual = 50 }
            };

            // Cliente só tem PETR4, não tem VALE3 (100% vs 0%)
            var custodias = new List<CustodiaViewModel>
            {
                new CustodiaViewModel
                {
                    Id = 1,
                    ContaGraficaId = 10,
                    Ticker = "PETR4",
                    Quantidade = 100,
                    PrecoMedio = 30.00m
                }
            };

            _clienteRepoMock.Setup(r => r.GetByIdAsync(clienteId))
                .ReturnsAsync(cliente);
            _contaGraficaRepoMock.Setup(r => r.GetByClienteIdAsync(clienteId))
                .ReturnsAsync(conta);
            _cestaRepoMock.Setup(r => r.GetAtualAsync())
                .ReturnsAsync(cesta);
            _cestaRepoMock.Setup(r => r.GetItensByCestaIdAsync(1))
                .ReturnsAsync(itensCesta);
            _custodiaRepoMock.Setup(r => r.GetByContaIdAsync(10))
                .ReturnsAsync(custodias);

            // Cotações
            _cotacaoServiceMock.Setup(s => s.GetPrecoFechamentoMaisRecenteAsync("PETR4"))
                .ReturnsAsync(35.00m);
            _cotacaoServiceMock.Setup(s => s.GetPrecoFechamentoMaisRecenteAsync("VALE3"))
                .ReturnsAsync(70.00m);

            // Act
            await _service.RebalancearPorDesvioAsync(clienteId, limiarDesvio: 5m);

            // Assert
            // Desvio VALE3: |0 - 50| = 50% > 5% -> Deve comprar
            // Valor total: 3500, Valor alvo VALE3: 1750
            // Quantidade a comprar: 1750/70 = 25 ações

            _custodiaRepoMock.Verify(r => r.AddAsync(It.Is<CustodiaViewModel>(c =>
                c.Ticker == "VALE3" &&
                c.Quantidade == 25 &&
                c.PrecoMedio == 70.00m
            )), Times.Once);
        }

        #endregion
    }
}
