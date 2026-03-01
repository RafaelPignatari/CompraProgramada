using System;
using System.ComponentModel.DataAnnotations;

namespace CompraProgramada.Models
{
    public class ItemCestaViewModel
    {
        public long Id { get; set; }

        public long CestaId { get; set; }

        [Required]
        [StringLength(10)]
        public string Ticker { get; set; } = string.Empty;

        public decimal Percentual { get; set; }
    }
}
