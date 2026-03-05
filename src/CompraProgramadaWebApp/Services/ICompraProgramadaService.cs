using CompraProgramadaWebApp.Models.DTOs;
using System;
using System.Threading.Tasks;

namespace CompraProgramadaWebApp.Services
{
    public interface ICompraProgramadaService
    {
        /// <summary>
        /// Executa o motor de compra programada para a data informada (ou hoje se null).
        /// Retorna um DTO com detalhes da execução, ordens geradas, distribuições e residuos.
        /// </summary>
        Task<CompraProgramadaResultDTO> ExecutarAsync(DateTime? dataExecucao = null);
    }
}
