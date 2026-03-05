using CompraProgramada.Models;
using System.Threading.Tasks;

namespace CompraProgramadaWebApp.Services
{
    public interface IContaGraficaService
    {
        Task<ContasGraficasViewModel> CriarContaFilhoteAsync(long clienteId);
    }
}
