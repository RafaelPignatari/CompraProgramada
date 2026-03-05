using CompraProgramadaWebApp.Helpers;
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

        [HttpPost("executar-compra")]
        public async Task<IActionResult> Executar([FromQuery] DateTime? data)
        {
            try
            {
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
