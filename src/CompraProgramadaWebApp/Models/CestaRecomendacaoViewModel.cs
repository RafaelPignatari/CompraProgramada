using System;
using System.ComponentModel.DataAnnotations;

namespace CompraProgramada.Models
{
    public class CestaRecomendacaoViewModel
    {
        public long Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Nome { get; set; } = string.Empty;

        public bool Ativa { get; set; } = true;

        public DateTime DataCriacao { get; set; }

        public DateTime? DataDesativacao { get; set; }
    }
}
