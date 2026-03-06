using System.Dynamic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using CompraProgramada.Models;
using CompraProgramadaWebApp.Data.Repositories;
using CompraProgramadaWebApp.Services;
using Xunit;
using System.Collections.Generic;
using CompraProgramadaWebApp.Models.DTOs;

namespace CompraProgramada.Tests.Unit
{
    public class ContaMasterServiceTests
    {
        [Fact]
        public async Task GetCustodiaAsync_RetornaResumoComValorTotal()
        {
            var repo = new Mock<IContaMasterRepository>();
            var custodias = new List<CustodiaViewModel>
            {
                new CustodiaViewModel { Ticker = "PETR4", Quantidade = 2, PrecoMedio = 35.5m },
                new CustodiaViewModel { Ticker = "ITUB4", Quantidade = 1, PrecoMedio = 30m }
            };
            repo.Setup(r => r.GetCustodiaAsync()).ReturnsAsync(custodias);

            var service = new ContaMasterService(repo.Object);
            var result = await service.GetCustodiaAsync();
            result.Should().NotBeNull();

            var resposta = result as CustodiaResponseDTO;
            decimal valorTotal = resposta.ValorTotalResiduo;

            valorTotal.Should().BeApproximately(2 * 35.5m + 1 * 30m, 0.001m);
        }
    }
}
