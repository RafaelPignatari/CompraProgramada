using CompraProgramada.Models;
using CompraProgramadaWebApp.Data.Repositories;
using CompraProgramadaWebApp.Helpers;
using CompraProgramadaWebApp.Models.DTOs;

namespace CompraProgramadaWebApp.Services
{
    public class ClienteService : IClienteService
    {
        private readonly IClienteRepository _repo;
        private readonly IContaGraficaService _contaService;

        public ClienteService(IClienteRepository repo, IContaGraficaService contaService)
        {
            _repo = repo;
            _contaService = contaService;
        }

        public async Task<AdesaoResponseDTO> AdesaoAsync(ClienteDTO clienteDTO)
        {
            var existing = await _repo.GetByCpfAsync(clienteDTO.CPF);

            if (existing != null)
                throw new InvalidOperationException(Constantes.CLIENTE_CPF_DUPLICADO);

            if (clienteDTO.ValorMensal < 100)
                throw new InvalidOperationException(Constantes.VALOR_MENSAL_INVALIDO);

            var model = new ClienteViewModel(clienteDTO);

            await _repo.AddAsync(model);
            await _repo.SaveChangesAsync();

            var contaGrafica = await _contaService.CriarContaFilhoteAsync(model.Id);

            var retorno = new AdesaoResponseDTO(model, contaGrafica);

            return retorno;
        }

        public async Task<ClienteViewModel?> SaidaAsync(long clienteId)
        {
            var cliente = await _repo.GetByIdAsync(clienteId);

            if (cliente == null)
                throw new InvalidOperationException(Constantes.CLIENTE_NAO_ENCONTRADO);

            if (!cliente.Ativo)
                throw new InvalidOperationException(Constantes.CLIENTE_JA_INATIVO);

            cliente.Ativo = false;
            cliente.DataSaida = DateTime.UtcNow;

            await _repo.UpdateAsync(cliente);
            await _repo.SaveChangesAsync();

            return cliente;
        }

        public async Task<AlteracaoResponseDTO?> AlterarValorMensalAsync(long clienteId, decimal novoValor)
        {
            var cliente = await _repo.GetByIdAsync(clienteId);

            if (cliente == null)
                throw new InvalidOperationException(Constantes.CLIENTE_NAO_ENCONTRADO);

            if (novoValor < 100)
                throw new InvalidOperationException(Constantes.VALOR_MENSAL_INVALIDO);

            var valorAnterior = cliente.ValorMensal;
            cliente.ValorMensal = novoValor;

            await _repo.UpdateAsync(cliente);
            await _repo.SaveChangesAsync();

            var retorno = new AlteracaoResponseDTO(cliente, valorAnterior, DateTime.UtcNow);

            return retorno;
        }

        public Task<ClienteViewModel?> GetByIdAsync(long clienteId)
        {
            return _repo.GetByIdAsync(clienteId);
        }
    }
}
