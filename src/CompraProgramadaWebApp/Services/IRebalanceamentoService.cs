using System.Threading.Tasks;

namespace CompraProgramadaWebApp.Services
{
    public interface IRebalanceamentoService
    {
        /// <summary>
        /// Dispara o processo de rebalanceamento para todos os clientes ativos quando a cesta é alterada.
        /// </summary>
        /// <param name="cestaAnteriorId">ID da cesta anterior (que foi desativada)</param>
        /// <param name="cestaNovaId">ID da nova cesta ativa</param>
        Task RebalancearPorMudancaDeCestaAsync(long cestaAnteriorId, long cestaNovaId);

        /// <summary>
        /// Rebalanceia a carteira de um cliente específico por desvio de proporção.
        /// </summary>
        /// <param name="clienteId">ID do cliente</param>
        /// <param name="limiarDesvio">Limiar de desvio em pontos percentuais (padrão: 5)</param>
        Task RebalancearPorDesvioAsync(long clienteId, decimal limiarDesvio = 5m);
    }
}
