namespace CompraProgramadaWebApp.Models.DTOs
{
    public class CustodiaResponseDTO
    {
        public object ContaMaster { get; set; }
        public IEnumerable<object> Custodia { get; set; }
        public decimal ValorTotalResiduo { get; set; }
    }
}
