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

        /// <summary>
        /// Importa cotações a partir dos arquivos de cotações localizados na pasta de cotações do projeto.
        /// </summary>
        /// <remarks>
        /// O método localiza o diretório relativo ao ContentRootPath e chama o serviço de importação.
        /// Retorna o número de registros importados.
        /// </remarks>
        /// <returns>Ok(200) com a quantidade importada, NotFound(404) se pasta não existir, BadRequest(400) ou 500 em erros.</returns>
        /// <response code="200">Importação realizada com sucesso. Retorna quantidade de registros importados.</response>
        /// <response code="404">Pasta de cotações não encontrada.</response>
        /// <response code="400">Argumento inválido (ex.: caminho inválido).</response>
        /// <response code="500">Erro interno ao importar as cotações.</response>
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
