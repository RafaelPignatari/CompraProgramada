using CompraProgramadaWebApp.Models.DTOs;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CompraProgramada.Models
{
    [Table("ItensCesta")]
    public class ItemCestaViewModel
    {
        public long Id { get; set; }

        public long CestaId { get; set; }

        [Required]
        [StringLength(10)]
        public string Ticker { get; set; } = string.Empty;

        public decimal Percentual { get; set; }

        public ItemCestaViewModel()
        {
            
        }

        public ItemCestaViewModel(ItemRequest item)
        {
            Ticker = item.Ticker;
            Percentual = item.Percentual;
        }
    }
}
