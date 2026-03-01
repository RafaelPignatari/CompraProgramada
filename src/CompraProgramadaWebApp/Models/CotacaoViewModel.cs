using System;
using System.ComponentModel.DataAnnotations;

namespace CompraProgramada.Models
{
    public class CotacaoViewModel
    {
        public long Id { get; set; }

        public DateTime DataPregao { get; set; }

        [Required]
        [StringLength(10)]
        public string Ticker { get; set; } = string.Empty;

        public decimal? PrecoAbertura { get; set; }
        public decimal? PrecoFechamento { get; set; }
        public decimal? PrecoMaximo { get; set; }
        public decimal? PrecoMinimo { get; set; }
    }
}
