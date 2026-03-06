using CompraProgramadaWebApp.Helpers;
using CompraProgramadaWebApp.Models.DTOs;
using CompraProgramadaWebApp.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace CompraProgramadaWebApp.Controllers.Api
{
    [ApiController]
    [Route("api/motor")]
    public class ComprasProgramadasController : ControllerBase
    {
        private readonly ICompraProgramadaService _service;

        public ComprasProgramadasController(ICompraProgramadaService service)
        {
            _service = service;
        }

        /// <summary>
        /// Executa o motor de compra programada para a data informada.
        /// </summary>
        /// <param name="request">Objeto contendo a data de referência para execução (campo dataReferencia). Se nulo, usa a data atual.</param>
        /// <returns>Ok(200) com o resultado da execução ou erros de validação/cesta quando aplicável.</returns>
        /// <response code="200">Execução realizada com sucesso. Retorna resumo das ordens e distribuições.</response>
        /// <response code="404">Cesta de recomendação não encontrada.</response>
        /// <response code="400">Erro de validação (data inválida ou outro erro de negócio).</response>
        /// <response code="500">Erro interno ao executar o motor de compra programada.</response>
        [HttpPost("executar-compra")]
        public async Task<IActionResult> Executar([FromBody] ExecucaoRequestDTO request)
        {
            try
            {
                var data = request?.DataReferencia;
                var result = await _service.ExecutarAsync(data);
                return Ok(result);
            }
            catch (InvalidOperationException ex) when (ex.Message == Constantes.CESTA_NAO_ENCONTRADA)
            {
                return NotFound(new { erro = Constantes.CESTA_NAO_ENCONTRADA, codigo = Constantes.CESTA_NAO_ENCONTRADA });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { erro = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { erro = "Erro ao executar motor de compra programada" });
            }
        }
    }
}
