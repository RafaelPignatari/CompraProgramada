namespace CompraProgramadaWebApp.Models.DTOs
{
    public class CestaResponseDTO
    {
        public long Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public bool Ativa { get; set; }
        public DateTime DataCriacao { get; set; }
        public List<ItemRequest> Itens { get; set; } = new List<ItemRequest>();
        public bool RebalanceamentoDisparado { get; set; } = false;
        public virtual string Mensagem { get; set; } = string.Empty;
    }
}
