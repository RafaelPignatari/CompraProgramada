using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CompraProgramadaWebApp.Services;

namespace CompraProgramadaWebApp.Controllers.Api.Admin
{
    [ApiController]
    [Route("api/admin/conta-master")]
    public class ContaMasterController : ControllerBase
    {
        private readonly IContaMasterService _service;
        public ContaMasterController(IContaMasterService service)
        {
            _service = service;
        }

        [HttpGet("custodia")]
        public async Task<IActionResult> GetCustodia()
        {
            var result = await _service.GetCustodiaAsync();

            return Ok(result);
        }
    }
}
