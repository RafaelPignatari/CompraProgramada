using System;
using System.ComponentModel.DataAnnotations;

namespace CompraProgramada.Models
{
    public class DistribuicaoViewModel
    {
        public long Id { get; set; }

        public long OrdemCompraId { get; set; }

        public long CustodiaFilhoId { get; set; }

        [Required]
        [StringLength(10)]
        public string Ticker { get; set; } = string.Empty;

        public int Quantidade { get; set; }

        public decimal? PrecoUnitario { get; set; }

        public DateTime DataDistribuicao { get; set; }
    }
}
