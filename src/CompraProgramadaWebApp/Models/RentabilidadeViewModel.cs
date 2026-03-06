using System.Collections.Generic;

namespace CompraProgramada.Models
{
    public class RentabilidadeAtivoViewModel
    {
        public string Ticker { get; set; } = string.Empty;
        public int Quantidade { get; set; }
        public decimal PrecoMedio { get; set; }
        public decimal CotacaoAtual { get; set; }
        public decimal ValorAtual { get; set; }
        public decimal PL { get; set; }
        public decimal ComposicaoPercentual { get; set; }
    }

    public class RentabilidadeViewModel
    {
        public long ClienteId { get; set; }
        public string ClienteNome { get; set; } = string.Empty;
        public decimal ValorInvestidoTotal { get; set; }
        public decimal ValorAtualTotal { get; set; }
        public decimal PLTotal { get; set; }
        public decimal RentabilidadePercentual { get; set; }

        public List<RentabilidadeAtivoViewModel> Ativos { get; set; } = new List<RentabilidadeAtivoViewModel>();
    }
}
