using CompraProgramada.Models;
using CompraProgramadaWebApp.Data.Repositories;
using CompraProgramadaWebApp.Helpers;
using CompraProgramadaWebApp.Models.DTOs;

namespace CompraProgramadaWebApp.Services
{
    public class CestaService : ICestaService
    {
        private readonly ICestaRepository _repo;
        private readonly IClienteService _clientesService;
        private readonly ICotacaoService _cotacaoService;
        private readonly IRebalanceamentoService _rebalanceamentoService;

        public CestaService(ICestaRepository repo, IClienteService clienteService, ICotacaoService cotacaoService, IRebalanceamentoService rebalanceamentoService)
        {
            _repo = repo;
            _clientesService = clienteService;
            _cotacaoService = cotacaoService;
            _rebalanceamentoService = rebalanceamentoService;
        }

        public async Task<CestaResponseDTO> CriarOuAtualizarCestaAsync(CestaRequestDTO cestaDTO)
        {
            if (cestaDTO.Itens.Count != 5)
                throw new InvalidOperationException(Constantes.QTD_ATIVOS_INVALIDA);

            var soma = cestaDTO.Itens.Sum(i => i.Percentual);

            if (soma != 100)
                throw new InvalidOperationException(Constantes.PERCENTUAIS_INVALIDOS);

            var atual = await _repo.GetAtualAsync();
            var retorno = new CestaResponseDTO();

            var ativosRemovidos = new List<string>();
            var ativosAdicionados = new List<string>();
            long? cestaAnteriorId = null;

            if (atual != null)
            {
                retorno.RebalanceamentoDisparado = true;
                cestaAnteriorId = atual.Id;

                var itensAntigos = (await _repo.GetItensByCestaIdAsync(atual.Id)).Select(i => i.Ticker.Trim()).ToList();

                var novosTickers = cestaDTO.Itens.Select(i => i.Ticker.Trim()).ToList();

                ativosRemovidos = itensAntigos.Except(novosTickers, StringComparer.OrdinalIgnoreCase).ToList();
                ativosAdicionados = novosTickers.Except(itensAntigos, StringComparer.OrdinalIgnoreCase).ToList();

                await DesativaCesta(atual);
            }

            var cesta = new CestaRecomendacaoViewModel { Nome = cestaDTO.Nome, Ativa = true, DataCriacao = DateTime.UtcNow };
            var itens = new List<ItemCestaViewModel>();

            foreach (var itemTO in cestaDTO.Itens)
                itens.Add(new ItemCestaViewModel(itemTO));

            await _repo.AddAsync(cesta, itens);

            if (retorno.RebalanceamentoDisparado && cestaAnteriorId.HasValue)
            {
                await _rebalanceamentoService.RebalancearPorMudancaDeCestaAsync(cestaAnteriorId.Value, cesta.Id);
                return await MontaRetornoAtualizacaoCesta(cesta, cestaDTO.Itens, ativosAdicionados, ativosRemovidos);
            }

            retorno = await MontaRetornoCriacaoCesta(cesta, cestaDTO.Itens);

            return retorno;
        }

        public async Task<CestaGetResponseDTO> GetAtualAsync()
        {
            var cesta = await _repo.GetAtualAsync();

            if (cesta == null)
                throw new InvalidOperationException(Constantes.CESTA_NAO_ENCONTRADA);
            var retorno = new CestaGetResponseDTO
            {
                CestaId = cesta.Id,
                Nome = cesta.Nome,
                Ativa = cesta.Ativa,
                DataCriacao = cesta.DataCriacao
            };

            var itens = (await _repo.GetItensByCestaIdAsync(cesta.Id)).ToList();

            var itensResp = new List<ItemCestaResponsePercentualDTO>();
            foreach (var i in itens)
            {
                var ticker = i.Ticker?.Trim() ?? string.Empty;

                var cotacaoAtual = await _cotacaoService.GetPrecoFechamentoMaisRecenteAsync(ticker) ?? 0m;

                itensResp.Add(new ItemCestaResponsePercentualDTO
                {
                    Ticker = i.Ticker,
                    Percentual = i.Percentual,
                    CotacaoAtual = cotacaoAtual
                });
            }

            retorno.Itens = itensResp;

            return retorno;
        }

        public async Task<IEnumerable<HistoricoCestaResponseDTO>> GetHistoricoAsync()
        {
            var historicoCestas = await _repo.GetHistoricoAsync();
            var retorno = new List<HistoricoCestaResponseDTO>();

            foreach (var cesta in historicoCestas)
            {
                var itens = await _repo.GetItensByCestaIdAsync(cesta.Id);
                var itensResp = new List<ItemCestaResponseDTO>();

                foreach (var item in itens)
                {
                    itensResp.Add(new ItemCestaResponseDTO
                    {
                        Ticker = item.Ticker,
                        Percentual = item.Percentual
                    });
                }

                retorno.Add(new HistoricoCestaResponseDTO(cesta, itensResp));
            }

            return retorno;
        }

        private async Task DesativaCesta(CestaRecomendacaoViewModel cesta)
        {
            cesta.Ativa = false;
            cesta.DataDesativacao = System.DateTime.UtcNow;

            await _repo.SaveChangesAsync();
        }

        private async Task<CestaResponseDTO> MontaRetornoCriacaoCesta(CestaRecomendacaoViewModel cesta, List<ItemRequest> itens)
        {
            return new CestaResponseDTO
            {
                Id = cesta.Id,
                Nome = cesta.Nome,
                Ativa = cesta.Ativa,
                DataCriacao = cesta.DataCriacao,
                Itens = itens,
                Mensagem = Constantes.Mensagens.PRIMEIRA_CESTA_CADASTRADA
            };
        }

        private async Task<CestaUpdateResponseDTO> MontaRetornoAtualizacaoCesta(CestaRecomendacaoViewModel cesta, List<ItemRequest> itens, List<string> ativosAdicionados, List<string> ativosRemovidos)
        {
            var qtdClientesAfetados = await _clientesService.GetQtdClientesAtivosAsync();

            return new CestaUpdateResponseDTO
            {
                Id = cesta.Id,
                Nome = cesta.Nome,
                Ativa = cesta.Ativa,
                DataCriacao = cesta.DataCriacao,
                Itens = itens,
                RebalanceamentoDisparado = true,
                AtivosAdicionados = ativosAdicionados ?? new List<string>(),
                AtivosRemovidos = ativosRemovidos ?? new List<string>(),
                Mensagem = string.Format(Constantes.Mensagens.CESTA_ATUALIZADA, qtdClientesAfetados)
            };
        }
    }
}
