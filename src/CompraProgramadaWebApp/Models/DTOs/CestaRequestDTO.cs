using CompraProgramadaWebApp.Controllers.Api.Admin;

namespace CompraProgramadaWebApp.Models.DTOs
{
    public class CestaRequestDTO
    {
        public string Nome { get; set; } = string.Empty;
        public List<ItemRequest> Itens { get; set; } = new List<ItemRequest>();
    }

    public class ItemRequest
    {
        public string Ticker { get; set; } = string.Empty;
        public decimal Percentual { get; set; }
    }
}
