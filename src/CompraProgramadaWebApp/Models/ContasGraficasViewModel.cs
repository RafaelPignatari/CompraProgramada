using CompraProgramadaWebApp.Models.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CompraProgramada.Models
{
    [Table("ContasGraficas")]
    public class ContasGraficasViewModel
    {
        public long Id { get; set; }

        public long ClienteId { get; set; }

        [Required]
        [StringLength(20)]
        public string NumeroConta { get; set; } = string.Empty;

        public EnumContaTipo Tipo { get; set; } = EnumContaTipo.FILHOTE;

        public DateTime DataCriacao { get; set; }
    }
}
