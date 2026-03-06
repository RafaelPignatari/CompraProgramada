using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using CompraProgramadaWebApp.Data.Repositories;
using CompraProgramadaWebApp.Services;
using CompraProgramada.Models;
using Xunit;

namespace CompraProgramada.Tests.Unit
{
    public class CotacaoServiceTests
    {
        [Fact]
        public async Task GetPrecoFechamentoMaisRecenteAsync_RetornaPrecoQuandoExiste()
        {
            var repo = new Mock<ICotacaoRepository>();
            repo.Setup(r => r.GetLatestByTickerAsync("PETR4")).ReturnsAsync(new CotacaoViewModel { PrecoFechamento = 35m });

            var service = new CotacaoService(repo.Object);

            var preco = await service.GetPrecoFechamentoMaisRecenteAsync("PETR4");

            preco.Should().Be(35m);
        }

        [Fact]
        public async Task GetPrecosFechamentoMaisRecentesAsync_RetornaDicionario()
        {
            var repo = new Mock<ICotacaoRepository>();
            var dict = new Dictionary<string, CotacaoViewModel?>
            {
                { "PETR4", new CotacaoViewModel { PrecoFechamento = 35m } },
                { "VALE3", new CotacaoViewModel { PrecoFechamento = 62m } }
            };
            repo.Setup(r => r.GetLatestByTickersAsync(It.IsAny<IEnumerable<string>>())).ReturnsAsync(dict);

            var service = new CotacaoService(repo.Object);

            var precos = await service.GetPrecosFechamentoMaisRecentesAsync(new[] { "PETR4", "VALE3" });

            precos.Should().ContainKey("PETR4");
            precos["PETR4"].Should().Be(35m);
            precos["VALE3"].Should().Be(62m);
        }
    }
}
