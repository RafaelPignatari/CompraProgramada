using CompraProgramada.Models;
using CompraProgramadaWebApp.Data.Repositories;
using CompraProgramadaWebApp.Models.Enums;

namespace CompraProgramadaWebApp.Services
{
    public class ContaGraficaService : IContaGraficaService
    {
        private readonly IContaGraficaRepository _contaRepo;
        private readonly ICustodiaRepository _custodiaRepo;

        public ContaGraficaService(IContaGraficaRepository contaRepo, ICustodiaRepository custodiaRepo)
        {
            _contaRepo = contaRepo;
            _custodiaRepo = custodiaRepo;
        }

        public async Task<ContasGraficasViewModel> CriarContaFilhoteAsync(long clienteId)
        {
            var conta = new ContasGraficasViewModel
            {
                ClienteId = clienteId,
                NumeroConta = $"FLH-{clienteId:D6}",
                Tipo = EnumContaTipo.FILHOTE,
                DataCriacao = DateTime.UtcNow
            };

            await _contaRepo.AddAsync(conta);
            await _contaRepo.SaveChangesAsync();

            var custodia = new CustodiaViewModel
            {
                ContaGraficaId = conta.Id,
                Ticker = string.Empty,
                DataUltimaAtualizacao = DateTime.UtcNow
            };

            await _custodiaRepo.AddAsync(custodia);
            await _custodiaRepo.SaveChangesAsync();

            return conta;
        }
    }
}
