using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using CompraProgramadaWebApp.Services;

namespace CompraProgramadaWebApp.Controllers.Api
{
    [ApiController]
    [Route("api/cotacoes")]
    public class CotacoesController : ControllerBase
    {
        private readonly ICotacaoImportService _importService;
        private readonly IWebHostEnvironment _env;

        public CotacoesController(ICotacaoImportService importService, IWebHostEnvironment env)
        {
            _importService = importService;
            _env = env;
        }

        [HttpPost("importar")]
        public async Task<IActionResult> Importar()
        {
            var pasta = Path.Combine( _env.ContentRootPath, "..\\..\\", "cotacoes");

            try
            {
                var total = await _importService.ImportarAsync(pasta);
                return Ok(new { imported = total });
            }
            catch (DirectoryNotFoundException)
            {
                return NotFound(new { error = "Pasta de cotações não encontrada.", path = pasta });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Erro ao importar cotações." });
            }
        }
    }
}
