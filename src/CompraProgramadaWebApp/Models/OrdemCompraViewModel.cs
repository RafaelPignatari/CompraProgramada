using CompraProgramadaWebApp.Models.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace CompraProgramada.Models
{
    public class OrdemCompraViewModel
    {
        public long Id { get; set; }

        public long ContaMasterId { get; set; }

        [Required]
        [StringLength(10)]
        public string Ticker { get; set; } = string.Empty;

        public int Quantidade { get; set; }

        public decimal PrecoUnitario { get; set; }

        public EnumTipoMercado TipoMercado { get; set; } = EnumTipoMercado.LOTE;

        public DateTime DataExecucao { get; set; }
    }
}
