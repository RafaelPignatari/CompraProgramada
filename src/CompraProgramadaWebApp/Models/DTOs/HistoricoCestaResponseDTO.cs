using CompraProgramada.Models;
using System.ComponentModel.DataAnnotations;

namespace CompraProgramadaWebApp.Models.DTOs
{
    public class HistoricoCestaResponseDTO
    {
        public long Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public bool Ativa { get; set; } = true;

        public DateTime DataCriacao { get; set; }

        public DateTime? DataDesativacao { get; set; }
        public List<ItemCestaResponseDTO> Itens { get; set; }

        public HistoricoCestaResponseDTO()
        {
            
        }

        public HistoricoCestaResponseDTO(CestaRecomendacaoViewModel cesta, List<ItemCestaResponseDTO> itens)
        {
            Id = cesta.Id;
            Nome = cesta.Nome;
            Ativa = cesta.Ativa;
            DataCriacao = cesta.DataCriacao;
            DataDesativacao = cesta.DataDesativacao;
            Itens = itens;
        }
    }

    public class ItemCestaResponseDTO
    {
        public string Ticker { get; set; } = string.Empty;

        public decimal Percentual { get; set; }
    }
}
