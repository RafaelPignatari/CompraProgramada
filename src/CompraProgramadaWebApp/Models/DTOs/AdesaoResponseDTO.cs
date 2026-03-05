using CompraProgramada.Models;

namespace CompraProgramadaWebApp.Models.DTOs
{
    public class AdesaoResponseDTO
    {
        public long ClienteId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string CPF { get; set; } = string.Empty;
        public string? Email { get; set; }
        public decimal ValorMensal { get; set; }
        public bool Ativo { get; set; }
        public System.DateTime DataAdesao { get; set; }
        public ContaGraficaDTO? ContaGrafica { get; set; }

        public AdesaoResponseDTO()
        {
            
        }

        public AdesaoResponseDTO(ClienteViewModel cliente, ContasGraficasViewModel contaGrafica)
        {
            ClienteId = cliente.Id;
            Nome = cliente.Nome;
            CPF = cliente.CPF;
            Email = cliente.Email;
            ValorMensal = cliente.ValorMensal;
            Ativo = cliente.Ativo;
            DataAdesao = cliente.DataAdesao;
            ContaGrafica = new ContaGraficaDTO(contaGrafica);
        }
    }

    public class ContaGraficaDTO
    {
        public long Id { get; set; }
        public string NumeroConta { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public System.DateTime DataCriacao { get; set; }

        public ContaGraficaDTO()
        {
            
        }

        public ContaGraficaDTO(ContasGraficasViewModel model)
        {
            Id = model.Id;
            NumeroConta = model.NumeroConta;
            Tipo = model.Tipo.ToString();
            DataCriacao = model.DataCriacao;
        }
    }
}
