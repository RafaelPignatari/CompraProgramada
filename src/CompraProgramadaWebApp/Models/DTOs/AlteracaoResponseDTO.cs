using CompraProgramada.Models;
using CompraProgramadaWebApp.Helpers;

namespace CompraProgramadaWebApp.Models.DTOs
{
    public class AlteracaoResponseDTO
    {
        public long ClienteId { get; set; }
        public decimal ValorMensal { get; set; }
        public decimal ValorMensalAnterior { get; set; }
        public DateTime DataAlteracao { get; set; }
        public string Mensagem { get; set; } = Constantes.Mensagens.VALOR_MENSAL_ATUALIZADO;

        public AlteracaoResponseDTO()
        {

        }

        public AlteracaoResponseDTO(ClienteViewModel cliente, decimal valorAnterior, DateTime dataAlteracao)
        {
            ClienteId = cliente.Id;
            ValorMensal = cliente.ValorMensal;
            ValorMensalAnterior = valorAnterior;
            DataAlteracao = dataAlteracao;
        }
    }
}
