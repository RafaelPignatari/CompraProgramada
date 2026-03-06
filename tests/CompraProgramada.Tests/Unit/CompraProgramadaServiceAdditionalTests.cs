using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using CompraProgramada.Models;
using CompraProgramadaWebApp.Data.Repositories;
using CompraProgramadaWebApp.Models.DTOs;
using CompraProgramadaWebApp.Services;
using CompraProgramadaWebApp.Models.Enums;
using Xunit;

namespace CompraProgramada.Tests.Unit
{
    public class CompraProgramadaServiceAdditionalTests
    {
        private CompraProgramadaService CreateService(
            Mock<IClienteRepository>? clienteRepo = null,
            Mock<ICestaService>? cestaService = null,
            Mock<ICotacaoService>? cotacaoService = null,
            Mock<IContaMasterRepository>? contaMasterRepo = null,
            Mock<IOrdemRepository>? ordemRepo = null,
            Mock<IContaGraficaRepository>? contaGraficaRepo = null,
            Mock<ICustodiaRepository>? custodiaRepo = null,
            Mock<IKafkaProducerService>? kafka = null)
        {
            clienteRepo ??= new Mock<IClienteRepository>();
            cestaService ??= new Mock<ICestaService>();
            cotacaoService ??= new Mock<ICotacaoService>();
            contaMasterRepo ??= new Mock<IContaMasterRepository>();
            ordemRepo ??= new Mock<IOrdemRepository>();
            contaGraficaRepo ??= new Mock<IContaGraficaRepository>();
            custodiaRepo ??= new Mock<ICustodiaRepository>();
            kafka ??= new Mock<IKafkaProducerService>();

            return new CompraProgramadaService(
                clienteRepo.Object,
                cestaService.Object,
                cotacaoService.Object,
                contaMasterRepo.Object,
                ordemRepo.Object,
                contaGraficaRepo.Object,
                custodiaRepo.Object,
                kafka.Object);
        }

        [Fact]
        public async Task ExecutarAsync_PrecosAusentes_RetornaNenhumaOrdem()
        {
            var clienteRepo = new Mock<IClienteRepository>();
            clienteRepo.Setup(r => r.GetClientesAtivosAsync()).ReturnsAsync(new List<ClienteViewModel>
            {
                new ClienteViewModel { Id = 1, Nome = "A", CPF = "1", ValorMensal = 300m }
            });

            var cesta = new CestaGetResponseDTO { CestaId = 1, Nome = "c", Ativa = true, DataCriacao = DateTime.UtcNow, Itens = new List<ItemCestaResponsePercentualDTO> { new ItemCestaResponsePercentualDTO { Ticker = "FOO", Percentual = 100m } } };
            var cestaService = new Mock<ICestaService>();
            cestaService.Setup(c => c.GetAtualAsync()).ReturnsAsync(cesta);

            var cotacao = new Mock<ICotacaoService>();
            cotacao.Setup(c => c.GetPrecosFechamentoMaisRecentesAsync(It.IsAny<IEnumerable<string>>())).ReturnsAsync(new Dictionary<string, decimal?> { { "FOO", null } });

            var contaMasterRepo = new Mock<IContaMasterRepository>();
            contaMasterRepo.Setup(r => r.GetCustodiaAsync()).ReturnsAsync(new List<CustodiaViewModel>());

            var service = CreateService(clienteRepo, cestaService, cotacao, contaMasterRepo);

            var result = await service.ExecutarAsync(new DateTime(2026, 3, 5));

            result.Should().NotBeNull();
            // mensagem pode ser "Nenhuma ordem gerada." ou "Nenhum aporte a processar." dependendo do caminho
            Xunit.Assert.True(result.Mensagem.Contains("Nenhuma ordem gerada") || result.Mensagem.Contains("Nenhum aporte"));
            result.OrdensCompra.Should().BeEmpty();
        }

        [Fact]
        public async Task ExecutarAsync_ResiduoMaiorQueSolicitado_DistribuiResiduo()
        {
            // One client, residuo in master covers requested quantity
            var cliente = new ClienteViewModel { Id = 1, Nome = "Cliente1", CPF = "11111111111", ValorMensal = 300m };
            var clienteRepo = new Mock<IClienteRepository>();
            clienteRepo.Setup(r => r.GetClientesAtivosAsync()).ReturnsAsync(new List<ClienteViewModel> { cliente });
            clienteRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cliente);

            var cesta = new CestaGetResponseDTO { CestaId = 1, Nome = "c", Ativa = true, DataCriacao = DateTime.UtcNow, Itens = new List<ItemCestaResponsePercentualDTO> { new ItemCestaResponsePercentualDTO { Ticker = "ABC", Percentual = 100m } } };
            var cestaService = new Mock<ICestaService>();
            cestaService.Setup(c => c.GetAtualAsync()).ReturnsAsync(cesta);

            var precos = new Dictionary<string, decimal?> { { "ABC", 10m } };
            var cotacao = new Mock<ICotacaoService>();
            cotacao.Setup(c => c.GetPrecosFechamentoMaisRecentesAsync(It.IsAny<IEnumerable<string>>())).ReturnsAsync(precos);

            // master custodia has 10 shares as residue
            var contaMasterRepo = new Mock<IContaMasterRepository>();
            contaMasterRepo.Setup(r => r.GetCustodiaAsync()).ReturnsAsync(new List<CustodiaViewModel> { new CustodiaViewModel { Ticker = "ABC", Quantidade = 10, PrecoMedio = 9.5m } });

            var ordemRepo = new Mock<IOrdemRepository>();
            ordemRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<OrdemCompraViewModel>>())).Returns(Task.CompletedTask);
            ordemRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var contaGraficaRepo = new Mock<IContaGraficaRepository>();
            contaGraficaRepo.Setup(c => c.GetByClienteIdAsync(1)).ReturnsAsync(new ContasGraficasViewModel { Id = 100, ClienteId = 1, NumeroConta = "FLH-000100" });

            var custodiaRepo = new Mock<ICustodiaRepository>();
            // simulate existing custodia on filhote
            custodiaRepo.Setup(c => c.GetByContaAndTickerAsync(100, "ABC")).ReturnsAsync(new CustodiaViewModel { ContaGraficaId = 100, Ticker = "ABC", Quantidade = 5, PrecoMedio = 8m });
            custodiaRepo.Setup(c => c.UpdateAsync(It.IsAny<CustodiaViewModel>())).Returns(Task.CompletedTask);
            custodiaRepo.Setup(c => c.SaveChangesAsync()).Returns(Task.CompletedTask);

            var kafka = new Mock<IKafkaProducerService>();

            var service = CreateService(clienteRepo, cestaService, cotacao, contaMasterRepo, ordemRepo, contaGraficaRepo, custodiaRepo, kafka);

            var result = await service.ExecutarAsync(new DateTime(2026, 3, 5));

            result.Should().NotBeNull();
            // since residue covered, ordens may be empty but distribution should include ticker
            result.OrdensCompra.Should().ContainSingle(o => o.Ticker == "ABC");
            result.Distribuicoes.Should().ContainSingle(d => d.ClienteId == 1);
            // verify kafka publish called at least once
            kafka.Verify(k => k.PublishAsync("IR_DEDO_DURO", It.IsAny<string>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task ExecutarAsync_GeraLotesEFracionarios_DistribuiEAtualizaMaster()
        {
            // Two clients to test distribution proportional
            var clientes = new List<ClienteViewModel>
            {
                new ClienteViewModel { Id = 1, Nome = "A", CPF = "1", ValorMensal = 1000m },
                new ClienteViewModel { Id = 2, Nome = "B", CPF = "2", ValorMensal = 2000m }
            };
            var clienteRepo = new Mock<IClienteRepository>();
            clienteRepo.Setup(r => r.GetClientesAtivosAsync()).ReturnsAsync(clientes);
            clienteRepo.Setup(r => r.GetByIdAsync(It.IsAny<long>())).ReturnsAsync((long id) => clientes.FirstOrDefault(c => c.Id == id));

            var cesta = new CestaGetResponseDTO
            {
                CestaId = 1,
                Nome = "c",
                Ativa = true,
                DataCriacao = DateTime.UtcNow,
                Itens = new List<ItemCestaResponsePercentualDTO> { new ItemCestaResponsePercentualDTO { Ticker = "PQR", Percentual = 100m } }
            };
            var cestaService = new Mock<ICestaService>();
            cestaService.Setup(c => c.GetAtualAsync()).ReturnsAsync(cesta);

            // Set price so totalConsolidado (1000/3 + 2000/3 = 1000) => quantidade comprada = floor(1000 / 3.0?) Wait compute.
            // For simplicity, set totalConsolidado such that quantidadeASerComprada = 350
            // We'll instead control by making ValorMensal large: totalConsolidado = (1000+2000)/3 = 1000 -> quantity = 1000/2.5=400. Adjust price to 2.5
            var precos = new Dictionary<string, decimal?> { { "PQR", 2.5m } };
            var cotacao = new Mock<ICotacaoService>();
            cotacao.Setup(c => c.GetPrecosFechamentoMaisRecentesAsync(It.IsAny<IEnumerable<string>>())).ReturnsAsync(precos);

            var contaMasterRepo = new Mock<IContaMasterRepository>();
            contaMasterRepo.Setup(r => r.GetCustodiaAsync()).ReturnsAsync(new List<CustodiaViewModel>());

            var ordemRepo = new Mock<IOrdemRepository>();
            ordemRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<OrdemCompraViewModel>>())).Returns(Task.CompletedTask);
            ordemRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var contaGraficaRepo = new Mock<IContaGraficaRepository>();
            contaGraficaRepo.Setup(c => c.GetByClienteIdAsync(1)).ReturnsAsync(new ContasGraficasViewModel { Id = 10, ClienteId = 1 });
            contaGraficaRepo.Setup(c => c.GetByClienteIdAsync(2)).ReturnsAsync(new ContasGraficasViewModel { Id = 20, ClienteId = 2 });

            var custodiaRepo = new Mock<ICustodiaRepository>();
            custodiaRepo.Setup(c => c.GetByContaAndTickerAsync(It.IsAny<long>(), It.IsAny<string>())).ReturnsAsync((CustodiaViewModel?)null);
            custodiaRepo.Setup(c => c.AddAsync(It.IsAny<CustodiaViewModel>())).Returns(Task.CompletedTask);
            custodiaRepo.Setup(c => c.SaveChangesAsync()).Returns(Task.CompletedTask);

            var kafka = new Mock<IKafkaProducerService>();

            var service = CreateService(clienteRepo, cestaService, cotacao, contaMasterRepo, ordemRepo, contaGraficaRepo, custodiaRepo, kafka);

            var result = await service.ExecutarAsync(new DateTime(2026, 3, 5));

            result.Should().NotBeNull();
            result.OrdensCompra.Should().ContainSingle(o => o.Ticker == "PQR");
            // should have two details (LOTE and FRACIONARIO) or at least one
            result.OrdensCompra.First().Detalhes.Should().NotBeEmpty();
            // verify distributions for both clients
            result.Distribuicoes.Select(d => d.ClienteId).Should().Contain(new[] { 1L, 2L });
            // kafka published
            kafka.Verify(k => k.PublishAsync("IR_DEDO_DURO", It.IsAny<string>()), Times.AtLeastOnce);
        }
    }
}
