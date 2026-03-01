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
        public string? TickerVendido { get; set; }

        [StringLength(10)]
        public string? TickerComprado { get; set; }

        public decimal? ValorVenda { get; set; }

        public DateTime DataRebalanceamento { get; set; }
    }
}
