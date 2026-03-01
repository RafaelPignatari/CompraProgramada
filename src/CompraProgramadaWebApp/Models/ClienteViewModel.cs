using System;
using System.ComponentModel.DataAnnotations;

namespace CompraProgramada.Models
{
    public class ClienteViewModel
    {
        public long Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Nome { get; set; } = string.Empty;

        [Required]
        [StringLength(11)]
        public string CPF { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Email { get; set; }

        [DataType(DataType.Currency)]
        public decimal ValorMensal { get; set; }

        public bool Ativo { get; set; } = true;

        public DateTime DataAdesao { get; set; }
    }
}
