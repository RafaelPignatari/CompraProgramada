using System.Threading.Tasks;

namespace CompraProgramadaWebApp.Services
{
    public interface ICotacaoImportService
    {
        /// <summary>
        /// Importa os arquivos COTAHIST da pasta informada e salva as cotações no banco.
        /// Retorna o número de registros importados.
        /// </summary>
        Task<int> ImportarAsync(string pastaCotacoes);
    }
}
