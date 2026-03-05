namespace CompraProgramadaWebApp.Models.DTOs
{
    public class DetalhesTickerDTO
    {
        public decimal Preco { get; set; }
        public int QuantidadeSolicitada { get; set; }
        public int QuantidadeComprada { get; set; }
        public int ResiduoUsado { get; set; }

        public DetalhesTickerDTO()
        {
            
        }

        public DetalhesTickerDTO(decimal preco, int quantidadeSolicitada, int quantidadeComprada, int residuoUsado)
        {
            Preco = preco;
            QuantidadeSolicitada = quantidadeSolicitada;
            QuantidadeComprada = quantidadeComprada;
            ResiduoUsado = residuoUsado;
        }
    }
}
