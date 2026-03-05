using CompraProgramada.Models;
using CompraProgramadaWebApp.Services;
using Microsoft.AspNetCore.Mvc;
using CompraProgramadaWebApp.Helpers;

namespace CompraProgramadaWebApp.Controllers.Api.Admin
{
    [ApiController]
    [Route("api/admin/cesta")]
    public class CestaController : ControllerBase
    {
        private readonly ICestaService _service;
        public CestaController(ICestaService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CestaRequest request)
        {
            try
            {
                var cesta = await _service.CriarOuAtualizarCestaAsync(request.Nome, request.Itens.Select(i => new ItemCestaViewModel { Ticker = i.Ticker, Percentual = i.Percentual }));

                return Created(string.Empty, new { cestaId = cesta.Id, nome = cesta.Nome, ativa = cesta.Ativa, dataCriacao = cesta.DataCriacao, itens = request.Itens });
            }
            catch (System.InvalidOperationException ex) when (ex.Message == Constantes.QTD_ATIVOS_INVALIDA)
            {
                return BadRequest(new { erro = string.Format(Constantes.Mensagens.QUANTIDADE_ATIVOS_INVALIDA, request.Itens.Count), codigo = Constantes.QTD_ATIVOS_INVALIDA });
            }
            catch (System.InvalidOperationException ex) when (ex.Message == Constantes.PERCENTUAIS_INVALIDOS)
            {
                return BadRequest(new { erro = string.Format(Constantes.Mensagens.PERCENTUAIS_INVALIDOS, request.Itens.Sum(i => i.Percentual)), codigo = Constantes.PERCENTUAIS_INVALIDOS });
            }
        }

        [HttpGet("atual")]
        public async Task<IActionResult> GetAtual()
        {
            var atual = await _service.GetAtualAsync();

            if (atual == null) 
                return NotFound(new { erro = Constantes.CESTA_NAO_ENCONTRADA, codigo = Constantes.CESTA_NAO_ENCONTRADA });

            var itens = await Task.Run(() => new object[0]);

            return Ok(new { cestaId = atual.Id, nome = atual.Nome, ativa = atual.Ativa, dataCriacao = atual.DataCriacao, itens = itens });
        }

        [HttpGet("historico")]
        public async Task<IActionResult> Historico()
        {
            var lista = await _service.GetHistoricoAsync();

            return Ok(new { cestas = lista });
        }
    }

    public class CestaRequest
    {
        public string Nome { get; set; } = string.Empty;
        public List<ItemRequest> Itens { get; set; } = new List<ItemRequest>();
    }

    public class ItemRequest
    {
        public string Ticker { get; set; } = string.Empty;
        public decimal Percentual { get; set; }
    }
}
