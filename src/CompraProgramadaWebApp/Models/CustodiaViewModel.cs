using System;
using System.ComponentModel.DataAnnotations;

namespace CompraProgramada.Models
{
    public class CustodiaViewModel
    {
        public long Id { get; set; }

        public long ContaGraficaId { get; set; }

        [Required]
        [StringLength(10)]
        public string Ticker { get; set; } = string.Empty;

        public int Quantidade { get; set; }

        public decimal PrecoMedio { get; set; }

        public DateTime? DataUltimaAtualizacao { get; set; }
    }
}
