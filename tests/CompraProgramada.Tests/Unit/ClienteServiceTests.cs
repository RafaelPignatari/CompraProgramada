using System;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using CompraProgramada.Models;
using CompraProgramadaWebApp.Data.Repositories;
using CompraProgramadaWebApp.Services;
using CompraProgramadaWebApp.Models.DTOs;
using Xunit;
using CompraProgramadaWebApp.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace CompraProgramada.Tests.Unit
{
    public class ClienteServiceTests
    {
        [Fact]
        public async Task AdesaoAsync_Sucesso_RetornaAdesaoResponse()
        {
            var repo = new Mock<IClienteRepository>();
            repo.Setup(r => r.GetByCpfAsync(It.IsAny<string>())).ReturnsAsync((ClienteViewModel?)null);
            repo.Setup(r => r.AddAsync(It.IsAny<ClienteViewModel>())).Returns(Task.CompletedTask);
            repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var contaService = new Mock<IContaGraficaService>();
            contaService.Setup(c => c.CriarContaFilhoteAsync(It.IsAny<long>())).ReturnsAsync(new ContasGraficasViewModel { Id = 1, ClienteId = 1, NumeroConta = "FLH-000001" });

            var service = new ClienteService(repo.Object, contaService.Object);

            var dto = new ClienteDTO { Nome = "Teste", CPF = "12345678901", Email = "x@x.com", ValorMensal = 300m };

            var result = await service.AdesaoAsync(dto);

            result.Should().NotBeNull();
            result.ContaGrafica.Should().NotBeNull();
            repo.Verify(r => r.AddAsync(It.IsAny<ClienteViewModel>()), Times.Once);
        }

        [Fact]
        public async Task AdesaoAsync_CpfDuplicado_ThrowsInvalidOperation()
        {
            var existing = new ClienteViewModel { Id = 1, CPF = "123" };
            var repo = new Mock<IClienteRepository>();
            repo.Setup(r => r.GetByCpfAsync(It.IsAny<string>())).ReturnsAsync(existing);
            var contaService = new Mock<IContaGraficaService>();
            var service = new ClienteService(repo.Object, contaService.Object);

            var dto = new ClienteDTO { Nome = "Teste", CPF = "123", Email = "x@x.com", ValorMensal = 300m };

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.AdesaoAsync(dto));
        }

        [Fact]
        public async Task AlterarValorMensalAsync_ClienteNaoEncontrado_Throws()
        {
            var repo = new Mock<IClienteRepository>();
            repo.Setup(r => r.GetByIdAsync(It.IsAny<long>())).ReturnsAsync((ClienteViewModel?)null);
            var contaService = new Mock<IContaGraficaService>();
            var service = new ClienteService(repo.Object, contaService.Object);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.AlterarValorMensalAsync(1, 500m));
        }

        [Fact]
        public async Task SaidaAsync_ClienteInativo_Throws()
        {
            var cliente = new ClienteViewModel { Id = 1, Ativo = false };
            var repo = new Mock<IClienteRepository>();
            repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cliente);
            var contaService = new Mock<IContaGraficaService>();
            var service = new ClienteService(repo.Object, contaService.Object);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaidaAsync(1));
        }

        [Fact]
        public async Task ClientesController_Adesao_ControllerBehaviour()
        {
            var mockService = new Mock<IClienteService>();
            var dto = new ClienteDTO { Nome = "X", CPF = "12345678901", Email = "a@a.com", ValorMensal = 300m };
            var adesao = new AdesaoResponseDTO { ClienteId = 1, Nome = "X", CPF = "12345678901", ValorMensal = 300m };

            mockService.Setup(s => s.AdesaoAsync(It.IsAny<ClienteDTO>())).ReturnsAsync(adesao);
            var controller = new CompraProgramadaWebApp.Controllers.Api.ClientesController(mockService.Object);

            var created = await controller.Adesao(dto) as CreatedAtActionResult;
            created.Should().NotBeNull();

            // simulate duplicate CPF
            mockService.Setup(s => s.AdesaoAsync(It.IsAny<ClienteDTO>())).ThrowsAsync(new InvalidOperationException(Constantes.CLIENTE_CPF_DUPLICADO));
            var bad = await controller.Adesao(dto) as BadRequestObjectResult;
            bad.Should().NotBeNull();
        }

        [Fact]
        public async Task ClientesController_SaidaAndGet_ControllerBehaviour()
        {
            var mockService = new Mock<IClienteService>();
            mockService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync((ClienteViewModel?)null);
            var controller = new CompraProgramadaWebApp.Controllers.Api.ClientesController(mockService.Object);

            var notFound = await controller.GetById(1) as NotFoundObjectResult;
            notFound.Should().NotBeNull();

            var cliente = new ClienteViewModel { Id = 2, Nome = "A", CPF = "1" };
            mockService.Setup(s => s.GetByIdAsync(2)).ReturnsAsync(cliente);
            var ok = await controller.GetById(2) as OkObjectResult;
            ok.Should().NotBeNull();

            // Saida - not found
            mockService.Setup(s => s.SaidaAsync(3)).ThrowsAsync(new InvalidOperationException(Constantes.CLIENTE_NAO_ENCONTRADO));
            var notFoundSaida = await controller.Saida(3) as NotFoundObjectResult;
            notFoundSaida.Should().NotBeNull();
        }
    }
}
