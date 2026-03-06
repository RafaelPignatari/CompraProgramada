using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using CompraProgramada.Models;
using CompraProgramadaWebApp.Data.Repositories;
using CompraProgramadaWebApp.Services;
using CompraProgramadaWebApp.Models.DTOs;
using Xunit;

namespace CompraProgramada.Tests.Unit
{
    public class CompraProgramadaServiceTests
    {
        [Fact]
        public async Task ExecutarAsync_NoClientes_ReturnsNenhumAporteMessage()
        {
            // Arrange
            var clienteRepo = new Mock<IClienteRepository>();
            clienteRepo.Setup(r => r.GetClientesAtivosAsync()).ReturnsAsync(new List<ClienteViewModel>());

            var cestaService = new Mock<ICestaService>();
            var cotacaoService = new Mock<ICotacaoService>();
            var contaMasterRepo = new Mock<IContaMasterRepository>();
            contaMasterRepo.Setup(r => r.GetCustodiaAsync()).ReturnsAsync(new List<CustodiaViewModel>());
            var ordemRepo = new Mock<IOrdemRepository>();
            var contaGraficaRepo = new Mock<IContaGraficaRepository>();
            var custodiaRepo = new Mock<ICustodiaRepository>();
            var kafka = new Mock<IKafkaProducerService>();

            var service = new CompraProgramadaService(
                clienteRepo.Object,
                cestaService.Object,
                cotacaoService.Object,
                contaMasterRepo.Object,
                ordemRepo.Object,
                contaGraficaRepo.Object,
                custodiaRepo.Object,
                kafka.Object);

            // Act
            var result = await service.ExecutarAsync(new DateTime(2026, 3, 5));

            // Assert
            result.Should().NotBeNull();
            result.Mensagem.Should().Contain("Nenhum aporte");
            result.TotalConsolidado.Should().Be(0);
            result.TotalClientes.Should().Be(0);
        }
    }
}
