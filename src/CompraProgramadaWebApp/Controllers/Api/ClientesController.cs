using CompraProgramada.Models;
using CompraProgramadaWebApp.Services;
using CompraProgramadaWebApp.Helpers;
using Microsoft.AspNetCore.Mvc;
using CompraProgramadaWebApp.Models.DTOs;

namespace CompraProgramadaWebApp.Controllers.Api
{
    [ApiController]
    [Route("api/clientes")]
    public class ClientesController : ControllerBase
    {
        private readonly IClienteService _service;

        public ClientesController(IClienteService service)
        {
            _service = service;
        }

        [HttpPost("adesao")]
        public async Task<IActionResult> Adesao([FromBody] ClienteDTO? model)
        {
            try
            {
                var created = await _service.AdesaoAsync(model);

                return CreatedAtAction(nameof(GetById), new { clienteId = created.ClienteId }, created);
            }
            catch (InvalidOperationException ex) when (ex.Message == Constantes.CLIENTE_CPF_DUPLICADO)
            {
                return BadRequest(new { erro = Constantes.Mensagens.CPF_DUPLICADO, codigo = Constantes.CLIENTE_CPF_DUPLICADO });
            }
            catch (InvalidOperationException ex) when (ex.Message == Constantes.VALOR_MENSAL_INVALIDO)
            {
                return BadRequest(new { erro = Constantes.Mensagens.VALOR_MENSAL_INVALIDO, codigo = Constantes.VALOR_MENSAL_INVALIDO });
            }
            catch (Exception ex)
            {
                return BadRequest(new { erro = Constantes.Mensagens.ERRO_GENERICO });
            }
        }

        [HttpGet("{clienteId}")]
        public async Task<IActionResult> GetById(long clienteId)
        {
            var cliente = await _service.GetByIdAsync(clienteId);

            if (cliente == null) 
                return NotFound(new { erro = Constantes.Mensagens.CLIENTE_NAO_ENCONTRADO, codigo = Constantes.CLIENTE_NAO_ENCONTRADO });

            return Ok(cliente);
        }

        [HttpPost("{clienteId}/saida")]
        public async Task<IActionResult> Saida(long clienteId)
        {
            try
            {
                var cliente = await _service.SaidaAsync(clienteId);

                return Ok(new { clienteId = cliente.Id, nome = cliente.Nome, ativo = cliente.Ativo, dataSaida = cliente.DataSaida, mensagem = Constantes.Mensagens.POSICAO_ENCERRADA });
            }
            catch (InvalidOperationException ex) when (ex.Message == Constantes.CLIENTE_NAO_ENCONTRADO)
            {
                return NotFound(new { erro = Constantes.Mensagens.CLIENTE_NAO_ENCONTRADO, codigo = Constantes.CLIENTE_NAO_ENCONTRADO });
            }
            catch (InvalidOperationException ex) when (ex.Message == Constantes.CLIENTE_JA_INATIVO)
            {
                return BadRequest(new { erro = Constantes.Mensagens.CLIENTE_JA_INATIVO, codigo = Constantes.CLIENTE_JA_INATIVO });
            }
        }

        [HttpPut("{clienteId}/valor-mensal")]
        public async Task<IActionResult> AlterarValorMensal(long clienteId, [FromBody] AlteraValorDTO? body)
        {
            try
            {
                var retorno = await _service.AlterarValorMensalAsync(clienteId, body.NovoValorMensal);

                return Ok(retorno);

            }
            catch (InvalidOperationException ex) when (ex.Message == Constantes.CLIENTE_NAO_ENCONTRADO)
            {
                return NotFound(new { erro = Constantes.Mensagens.CLIENTE_NAO_ENCONTRADO, codigo = Constantes.CLIENTE_NAO_ENCONTRADO });
            }
            catch (InvalidOperationException ex) when (ex.Message == Constantes.VALOR_MENSAL_INVALIDO)
            {
                return BadRequest(new { erro = Constantes.Mensagens.VALOR_MENSAL_INVALIDO, codigo = Constantes.VALOR_MENSAL_INVALIDO });
            }
            catch (Exception ex)
            {
                return BadRequest(new { erro = Constantes.Mensagens.ERRO_GENERICO });
            }

        }
    }
}
