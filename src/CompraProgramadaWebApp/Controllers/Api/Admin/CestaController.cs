using CompraProgramada.Models;
using CompraProgramadaWebApp.Services;
using Microsoft.AspNetCore.Mvc;
using CompraProgramadaWebApp.Helpers;
using CompraProgramadaWebApp.Models.DTOs;

namespace CompraProgramadaWebApp.Controllers.Api.Admin
{
    /// <summary>
    /// Endpoints para gerenciamento das cestas de recomendação (criação, consulta e histórico).
    /// </summary>
    [ApiController]
    [Route("api/admin/cesta")]
    public class CestaController : ControllerBase
    {
        private readonly ICestaService _service;
        private readonly IRebalanceamentoService _rebalanceamentoService;

        public CestaController(ICestaService service, IRebalanceamentoService rebalanceamentoService)
        {
            _service = service;
            _rebalanceamentoService = rebalanceamentoService;
        }

        /// <summary>
        /// Cria ou atualiza a cesta de recomendação com os itens informados.
        /// </summary>
        /// <param name="request">Objeto contendo nome e lista de itens com percentuais.</param>
        /// <returns>Created(201) com a cesta criada/atualizada ou BadRequest em caso de validação.</returns>
        /// <response code="201">Cesta criada ou atualizada com sucesso.</response>
        /// <response code="400">Quantidade de ativos inválida ou percentuais inválidos.</response>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CestaRequestDTO? request)
        {
            try
            {
                var cesta = await _service.CriarOuAtualizarCestaAsync(request);

                return Created(string.Empty, cesta);
            }
            catch (InvalidOperationException ex) when (ex.Message == Constantes.QTD_ATIVOS_INVALIDA)
            {
                return BadRequest(new { erro = string.Format(Constantes.Mensagens.QUANTIDADE_ATIVOS_INVALIDA, request.Itens.Count), codigo = Constantes.QTD_ATIVOS_INVALIDA });
            }
            catch (InvalidOperationException ex) when (ex.Message == Constantes.PERCENTUAIS_INVALIDOS)
            {
                return BadRequest(new { erro = string.Format(Constantes.Mensagens.PERCENTUAIS_INVALIDOS, request.Itens.Sum(i => i.Percentual)), codigo = Constantes.PERCENTUAIS_INVALIDOS });
            }
            catch (Exception ex)
            {
                return BadRequest(new { erro = string.Format(Constantes.Mensagens.ERRO_GENERICO)});
            }
        }

        /// <summary>
        /// Retorna a cesta atualmente ativa.
        /// </summary>
        /// <returns>Ok(200) com a cesta atual ou NotFound(404) se não existir.</returns>
        /// <response code="200">Retorna a cesta atual.</response>
        /// <response code="404">Nenhuma cesta ativa encontrada.</response>
        /// <response code="400">Erro ao processar a requisição.</response>
        [HttpGet("atual")]
        public async Task<IActionResult> GetAtual()
        {
            try
            {
                var cesta = await _service.GetAtualAsync();

                return Ok(cesta);
            }
            catch (InvalidOperationException ex) when (ex.Message == Constantes.CESTA_NAO_ENCONTRADA)
            {
                return NotFound(new { erro = Constantes.CESTA_NAO_ENCONTRADA, codigo = Constantes.CESTA_NAO_ENCONTRADA });
            }
            catch (Exception ex)
            {
                return BadRequest(new { erro = string.Format(Constantes.Mensagens.ERRO_GENERICO) });
            }
        }

        /// <summary>
        /// Retorna o histórico de cestas (listas de cestas já utilizadas/desativadas).
        /// </summary>
        /// <returns>Ok(200) com a lista de cestas históricas.</returns>
        /// <response code="200">Retorna a lista de cestas históricas.</response>
        /// <response code="400">Erro ao processar a requisição.</response>
        [HttpGet("historico")]
        public async Task<IActionResult> Historico()
        {
            try
            {
                var lista = await _service.GetHistoricoAsync();

                return Ok(new { cestas = lista });
            }
            catch (Exception ex)
            {
                return BadRequest(new { erro = string.Format(Constantes.Mensagens.ERRO_GENERICO) });
            }            
        }

        /// <summary>
        /// Dispara o rebalanceamento por desvio de proporção para um cliente específico.
        /// </summary>
        /// <param name="clienteId">ID do cliente a ser rebalanceado</param>
        /// <param name="limiarDesvio">Limiar de desvio em pontos percentuais (padrão: 5)</param>
        /// <returns>Ok(200) se o rebalanceamento foi disparado com sucesso.</returns>
        /// <response code="200">Rebalanceamento disparado com sucesso.</response>
        /// <response code="400">Erro ao processar a requisição.</response>
        [HttpPost("rebalancear/{clienteId}")]
        public async Task<IActionResult> RebalancearPorDesvio(long clienteId, [FromQuery] decimal limiarDesvio = 5m)
        {
            try
            {
                await _rebalanceamentoService.RebalancearPorDesvioAsync(clienteId, limiarDesvio);

                return Ok(new { mensagem = $"Rebalanceamento por desvio disparado para o cliente {clienteId}" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { erro = string.Format(Constantes.Mensagens.ERRO_GENERICO) });
            }
        }
    }
}
