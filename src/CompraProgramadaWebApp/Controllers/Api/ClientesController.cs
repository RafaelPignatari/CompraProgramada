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

        /// <summary>
        /// Realiza adesão de um novo cliente ao serviço de compra programada.
        /// </summary>
        /// <param name="model">Dados do cliente (nome, CPF, email e valor mensal).</param>
        /// <returns>Created (201) com o recurso criado ou BadRequest em caso de erro de validação.</returns>
        /// <response code="201">Cliente criado com sucesso. Retorna o recurso criado.</response>
        /// <response code="400">Dados inválidos (CPF duplicado, valor mensal inválido ou erro genérico).</response>
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

        /// <summary>
        /// Retorna os dados de um cliente pelo identificador.
        /// </summary>
        /// <param name="clienteId">Identificador do cliente.</param>
        /// <returns>Ok(200) com cliente quando encontrado ou NotFound(404) caso contrário.</returns>
        /// <response code="200">Cliente encontrado e retornado.</response>
        /// <response code="404">Cliente não encontrado.</response>
        [HttpGet("{clienteId}")]
        public async Task<IActionResult> GetById(long clienteId)
        {
            var cliente = await _service.GetByIdAsync(clienteId);

            if (cliente == null) 
                return NotFound(new { erro = Constantes.Mensagens.CLIENTE_NAO_ENCONTRADO, codigo = Constantes.CLIENTE_NAO_ENCONTRADO });

            return Ok(cliente);
        }

        /// <summary>
        /// Marca a saída (encerramento) do cliente do serviço.
        /// </summary>
        /// <param name="clienteId">Identificador do cliente a encerrar.</param>
        /// <returns>Ok(200) com resumo da operação, NotFound ou BadRequest em caso de erro.</returns>
        /// <response code="200">Saída realizada com sucesso.</response>
        /// <response code="404">Cliente não encontrado.</response>
        /// <response code="400">Cliente já inativo ou erro de negócio.</response>
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

        /// <summary>
        /// Altera o valor mensal do cliente.
        /// </summary>
        /// <param name="clienteId">Identificador do cliente.</param>
        /// <param name="body">Objeto contendo o novo valor mensal.</param>
        /// <returns>Ok(200) com detalhes da alteração ou NotFound/BadRequest em caso de erro.</returns>
        /// <response code="200">Valor mensal alterado com sucesso.</response>
        /// <response code="404">Cliente não encontrado.</response>
        /// <response code="400">Valor mensal inválido ou erro genérico.</response>
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
