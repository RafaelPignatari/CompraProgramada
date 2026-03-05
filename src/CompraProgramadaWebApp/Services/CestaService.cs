using CompraProgramada.Models;
using CompraProgramadaWebApp.Data.Repositories;
using CompraProgramadaWebApp.Helpers;

namespace CompraProgramadaWebApp.Services
{
    public class CestaService : ICestaService
    {
        private readonly ICestaRepository _repo;

        public CestaService(ICestaRepository repo)
        {
            _repo = repo;
        }

        public async Task<CestaRecomendacaoViewModel> CriarOuAtualizarCestaAsync(string nome, IEnumerable<ItemCestaViewModel> itens)
        {
            var lista = itens.ToList();

            if (lista.Count != 5)
                throw new InvalidOperationException(Constantes.QTD_ATIVOS_INVALIDA);

            var soma = lista.Sum(i => i.Percentual);

            if (soma != 100)
                throw new InvalidOperationException(Constantes.PERCENTUAIS_INVALIDOS);

            var atual = await _repo.GetAtualAsync();

            if (atual != null)
                await DesativaCesta(atual);

            var cesta = new CestaRecomendacaoViewModel { Nome = nome, Ativa = true, DataCriacao = DateTime.UtcNow };            

            await _repo.AddAsync(cesta, lista);
            return cesta;
        }

        public Task<CestaRecomendacaoViewModel?> GetAtualAsync()
        {
            return _repo.GetAtualAsync();
        }

        public Task<IEnumerable<CestaRecomendacaoViewModel>> GetHistoricoAsync()
        {
            return _repo.GetHistoricoAsync();
        }

        private async Task DesativaCesta(CestaRecomendacaoViewModel cesta)
        {
            cesta.Ativa = false;
            cesta.DataDesativacao = System.DateTime.UtcNow;

            await _repo.SaveChangesAsync();
        }
    }
}
