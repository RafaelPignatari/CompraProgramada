namespace CompraProgramadaWebApp.Models.DTOs
{
    public class CestaUpdateResponseDTO : CestaResponseDTO
    {
        public List<string> AtivosRemovidos { get; set; }
        public List<string> AtivosAdicionados { get; set; }
        public override string Mensagem { get; set; } = string.Empty;
    }
}
