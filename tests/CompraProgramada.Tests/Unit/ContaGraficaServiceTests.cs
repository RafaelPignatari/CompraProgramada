using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using CompraProgramada.Models;
using CompraProgramadaWebApp.Data.Repositories;
using CompraProgramadaWebApp.Services;
using Xunit;

namespace CompraProgramada.Tests.Unit
{
    public class ContaGraficaServiceTests
    {
        [Fact]
        public async Task CriarContaFilhoteAsync_CriaContaECustodia()
        {
            var contaRepo = new Mock<IContaGraficaRepository>();
            contaRepo.Setup(r => r.AddAsync(It.IsAny<ContasGraficasViewModel>())).Returns(Task.CompletedTask);
            contaRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var custRepo = new Mock<ICustodiaRepository>();
            custRepo.Setup(r => r.AddAsync(It.IsAny<CustodiaViewModel>())).Returns(Task.CompletedTask);
            custRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var service = new ContaGraficaService(contaRepo.Object, custRepo.Object);

            var result = await service.CriarContaFilhoteAsync(42);

            result.Should().NotBeNull();
            result.ClienteId.Should().Be(42);
            contaRepo.Verify(r => r.AddAsync(It.IsAny<ContasGraficasViewModel>()), Times.Once);
            custRepo.Verify(r => r.AddAsync(It.IsAny<CustodiaViewModel>()), Times.Once);
        }
    }
}
