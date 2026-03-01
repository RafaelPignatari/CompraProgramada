using CompraProgramadaWebApp.Models.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace CompraProgramada.Models
{
    public class EventoIRViewModel
    {
        public long Id { get; set; }

        public long ClienteId { get; set; }

        public EnumEventoIRTipo Tipo { get; set; }

        public decimal? ValorBase { get; set; }

        public decimal? ValorIR { get; set; }

        public bool PublicadoKafka { get; set; } = false;

        public DateTime DataEvento { get; set; }
    }
}
