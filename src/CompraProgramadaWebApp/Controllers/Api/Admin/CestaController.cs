using CompraProgramada.Models;
using CompraProgramadaWebApp.Services;
using Microsoft.AspNetCore.Mvc;
using CompraProgramadaWebApp.Helpers;
using CompraProgramadaWebApp.Models.DTOs;

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
    }
}
