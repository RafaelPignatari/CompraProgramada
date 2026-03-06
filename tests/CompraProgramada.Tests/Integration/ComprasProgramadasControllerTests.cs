using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CompraProgramada.Tests.Integration
{
    public class ComprasProgramadasControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public ComprasProgramadasControllerTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact(Skip = "É necessário que o kafka e o BD estejam rodando. Disponibilize nas variáveis de mabiente CONEXAO_BANCO e CONEXAO_KAFKA para rodar um teste de integração.")]
        public async Task ExecutarCompra_ReturnsOk_WhenDependenciesAvailable()
        {
            var client = _factory.CreateClient();
            var json = "{ \"dataReferencia\": \"2026-02-05\" }";
            var contentBody = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/api/motor/executar-compra", contentBody);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();
        }
    }
}
