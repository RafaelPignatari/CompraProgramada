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
            if (string.IsNullOrWhiteSpace(pastaCotacoes))
                throw new ArgumentException("Pasta Cotacoes é obrigatória", nameof(pastaCotacoes));

            if (!Directory.Exists(pastaCotacoes))
                throw new DirectoryNotFoundException($"Pasta não encontrada: {pastaCotacoes}");

            // Garantir suporte a ISO-8859-1
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var encoding = Encoding.GetEncoding("ISO-8859-1");

            var ultimoArquivo = Directory.GetFiles(pastaCotacoes, "COTAHIST_D*.TXT")
                                         .OrderByDescending(f => f)
                                         .FirstOrDefault();

            if (string.IsNullOrEmpty(ultimoArquivo))
                return 0;

            var totalImportados = 0;

            using var reader = new StreamReader(ultimoArquivo, encoding);
            string? linha;
            var registrosProcessados = 0;

            while ((linha = await reader.ReadLineAsync()) != null)
            {
                if (linha.Length < 245)
                    continue;

                var tipoRegistro = linha.Substring(0, 2);
                if (tipoRegistro != "01")
                    continue;

                var codbdi = linha.Substring(10, 2);
                if (codbdi != "02" && codbdi != "96")
                    continue;

                var tpmerc = linha.Substring(24, 3);
                if (tpmerc != "010" && tpmerc != "020")
                    continue;

                // Extrair campos
                var dataPregaoStr = linha.Substring(2, 8);
                if (!DateTime.TryParseExact(dataPregaoStr, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dataPregao))
                    continue;

                var ticker = linha.Substring(12, 12).Trim();

                var preAbe = ParsePreco(linha.Substring(56, 13));
                var preMax = ParsePreco(linha.Substring(69, 13));
                var preMin = ParsePreco(linha.Substring(82, 13));
                var preUlt = ParsePreco(linha.Substring(108, 13));

                // Evitar duplicados: se já existe cotação para ticker+data, pular
                var exists = await _db.Cotacoes
                                    .AsNoTracking()
                                    .AnyAsync(c => c.Ticker == ticker && c.DataPregao == dataPregao);

                if (exists)
                    continue;

                var entidade = new CotacaoViewModel
                {
                    DataPregao = dataPregao,
                    Ticker = ticker,
                    PrecoAbertura = preAbe,
                    PrecoMaximo = preMax,
                    PrecoMinimo = preMin,
                    PrecoFechamento = preUlt
                };

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
