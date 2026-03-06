using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CompraProgramadaWebApp.Data;
using CompraProgramada.Models;
using System.Globalization;

namespace CompraProgramadaWebApp.Services
{
    public class CotacaoImportService : ICotacaoImportService
    {
        private readonly AppDbContext _db;

        public CotacaoImportService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<int> ImportarAsync(string pastaCotacoes)
        {
            ValidatePasta(pastaCotacoes);

            // Garantir suporte a ISO-8859-1
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var encoding = Encoding.GetEncoding("ISO-8859-1");

            var ultimoArquivo = GetUltimoArquivo(pastaCotacoes);
            if (string.IsNullOrEmpty(ultimoArquivo))
                return 0;

            var totalImportados = await ProcessFileAsync(ultimoArquivo, encoding);
            return totalImportados;
        }

        private void ValidatePasta(string pastaCotacoes)
        {
            if (string.IsNullOrWhiteSpace(pastaCotacoes))
                throw new ArgumentException("Pasta Cotacoes é obrigatória", nameof(pastaCotacoes));

            if (!Directory.Exists(pastaCotacoes))
                throw new DirectoryNotFoundException($"Pasta não encontrada: {pastaCotacoes}");
        }

        private string? GetUltimoArquivo(string pastaCotacoes)
        {
            return Directory.GetFiles(pastaCotacoes, "COTAHIST_D*.TXT")
                .Select(f => new
                {
                    Caminho = f,
                    Data = ExtrairData(Path.GetFileNameWithoutExtension(f))
                })
                .Where(x => x.Data != null)
                .OrderByDescending(x => x.Data)
                .Select(x => x.Caminho)
                .FirstOrDefault();
        }

        private DateTime? ExtrairData(string nomeArquivo)
        {
            var dataStr = nomeArquivo.Replace("COTAHIST_D", "");

            if (DateTime.TryParseExact(
                dataStr,
                "yyyyMMdd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var data))
            {
                return data;
            }

            return null;
        }

        private async Task<int> ProcessFileAsync(string arquivo, Encoding encoding)
        {
            var totalImportados = 0;
            using var reader = new StreamReader(arquivo, encoding);
            string? linha;
            var registrosProcessados = 0;

            while ((linha = await reader.ReadLineAsync()) != null)
            {
                var entidade = ProcessLine(linha);
                if (entidade == null)
                    continue;

                // Evitar duplicados: se já existe cotação para ticker+data, pular
                var exists = await _db.Cotacoes
                                    .AsNoTracking()
                                    .AnyAsync(c => c.Ticker == entidade.Ticker && c.DataPregao == entidade.DataPregao);

                if (exists)
                    continue;

                await _db.Cotacoes.AddAsync(entidade);
                registrosProcessados++;
                totalImportados++;

                if (registrosProcessados >= 500)
                {
                    await _db.SaveChangesAsync();
                    registrosProcessados = 0;
                }
            }

            if (registrosProcessados > 0)
                await _db.SaveChangesAsync();

            return totalImportados;
        }

        private CotacaoViewModel? ProcessLine(string? linha)
        {
            if (string.IsNullOrEmpty(linha) || linha.Length < 245)
                return null;

            var tipoRegistro = linha.Substring(0, 2);
            if (tipoRegistro != "01")
                return null;

            var codbdi = linha.Substring(10, 2);
            if (codbdi != "02" && codbdi != "96")
                return null;

            var tpmerc = linha.Substring(24, 3);
            if (tpmerc != "010" && tpmerc != "020")
                return null;

            // Extrair campos
            var dataPregaoStr = linha.Substring(2, 8);
            if (!DateTime.TryParseExact(dataPregaoStr, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dataPregao))
                return null;

            var ticker = linha.Substring(12, 12).Trim();

            var preAbe = ParsePreco(linha.Substring(56, 13));
            var preMax = ParsePreco(linha.Substring(69, 13));
            var preMin = ParsePreco(linha.Substring(82, 13));
            var preUlt = ParsePreco(linha.Substring(108, 13));

            return new CotacaoViewModel
            {
                DataPregao = dataPregao,
                Ticker = ticker,
                PrecoAbertura = preAbe,
                PrecoMaximo = preMax,
                PrecoMinimo = preMin,
                PrecoFechamento = preUlt
            };
        }

        private decimal? ParsePreco(string s)
        {
            if (long.TryParse(s.Trim(), out var v))
            {
                return v / 100m;
            }

            return null;
        }
    }
}
