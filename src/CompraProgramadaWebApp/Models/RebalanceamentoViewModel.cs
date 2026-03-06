using CompraProgramadaWebApp.Models.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace CompraProgramada.Models
{
    public class RebalanceamentoViewModel
    {
        public long Id { get; set; }

        public long ClienteId { get; set; }

        public EnumRebalanceamentoTipo Tipo { get; set; }

        [StringLength(10)]
        public string TickerVendido { get; set; } = string.Empty;

        [StringLength(10)]
        public string TickerComprado { get; set; } = string.Empty;

        public decimal? ValorVenda { get; set; } = 0;

        public DateTime DataRebalanceamento { get; set; }
    }
}
