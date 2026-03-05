using System;
using System.Threading.Tasks;

namespace CompraProgramadaWebApp.Services
{
    public interface ICompraProgramadaService
    {
        /// <summary>
        /// Executa o motor de compra programada para a data informada (ou hoje se null).
        /// Retorna o número de ordens geradas.
        /// </summary>
        Task<int> ExecutarAsync(DateTime? dataExecucao = null);
    }
}
