using System;
using CompraProgramada.Models;

namespace CompraProgramadaWebApp.Models.DTOs
{
    public class CestaGetResponseDTO
    {
        public long CestaId { get; set; }
        public string Nome { get; set; }
        public bool Ativa { get; set; }
        public DateTime DataCriacao { get; set; }
        public IEnumerable<ItemCestaResponsePercentualDTO> Itens { get; set; }
    }

    public class ItemCestaResponsePercentualDTO : ItemCestaResponseDTO
    {
        public string Ticker { get; set; } = string.Empty;

        public decimal Percentual { get; set; }
        private decimal _cotacaoAtual;

        public decimal CotacaoAtual
        {
            get => _cotacaoAtual;
            set => _cotacaoAtual = Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }
    }
}
