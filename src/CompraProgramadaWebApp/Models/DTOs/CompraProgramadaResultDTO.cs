using System;
using System.Collections.Generic;

namespace CompraProgramadaWebApp.Models.DTOs
{
    public class CompraProgramadaResultDTO
    {
        public DateTime DataExecucao { get; set; }
        public int TotalClientes { get; set; }
        public decimal TotalConsolidado { get; set; }
        public List<OrdemCompraDTO> OrdensCompra { get; set; } = new List<OrdemCompraDTO>();
        public List<DistribuicaoClienteDTO> Distribuicoes { get; set; } = new List<DistribuicaoClienteDTO>();
        public List<ResiduoDTO> ResiduosCustMaster { get; set; } = new List<ResiduoDTO>();
        public int EventosIRPublicados { get; set; }
        public string Mensagem { get; set; } = string.Empty;
    }

    public class OrdemCompraDTO
    {
        public string Ticker { get; set; } = string.Empty;
        public int QuantidadeTotal { get; set; }
        public List<OrdemDetalheDTO> Detalhes { get; set; } = new List<OrdemDetalheDTO>();
        public decimal PrecoUnitario { get; set; }
        public decimal ValorTotal { get; set; }
    }

    public class OrdemDetalheDTO
    {
        public string Tipo { get; set; } = string.Empty; // LOTE or FRACIONARIO
        public string Ticker { get; set; } = string.Empty; // ex PETR4 or PETR4F
        public int Quantidade { get; set; }
    }

    public class DistribuicaoClienteDTO
    {
        public long ClienteId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public decimal ValorAporte { get; set; }
        public List<AtivoDistribuicaoDTO> Ativos { get; set; } = new List<AtivoDistribuicaoDTO>();
    }

    public class AtivoDistribuicaoDTO
    {
        public string Ticker { get; set; } = string.Empty;
        public int Quantidade { get; set; }
    }

    public class ResiduoDTO
    {
        public string Ticker { get; set; } = string.Empty;
        public int Quantidade { get; set; }
    }
}
