using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CompraProgramadaWebApp.Data;
using CompraProgramadaWebApp.Services;
using Xunit;
using CompraProgramada.Models;
using FluentAssertions;

namespace CompraProgramada.Tests.Unit
{
    public class CotacaoImportServiceTests
    {
        [Fact]
        public async Task ImportarAsync_InvalidPasta_ThrowsArgumentException()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            using var db = new AppDbContext(options);
            var svc = new CotacaoImportService(db);

            await Assert.ThrowsAsync<ArgumentException>(() => svc.ImportarAsync(string.Empty));
        }

        [Fact]
        public async Task ImportarAsync_NonExistingPasta_ThrowsDirectoryNotFound()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            using var db = new AppDbContext(options);
            var svc = new CotacaoImportService(db);

            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            await Assert.ThrowsAsync<DirectoryNotFoundException>(() => svc.ImportarAsync(path));
        }

        [Fact]
        public async Task ImportarAsync_NoFiles_ReturnsZero()
        {
            var dir = Path.Combine(Path.GetTempPath(), "cotacoes_test_" + Guid.NewGuid());
            Directory.CreateDirectory(dir);

            try
            {
                var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
                using var db = new AppDbContext(options);
                var svc = new CotacaoImportService(db);

                var result = await svc.ImportarAsync(dir);
                Assert.Equal(0, result);
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        private string BuildLine(string dataPregao, string ticker, long precoAberturaCents, long precoMaxCents, long precoMinCents, long precoUltCents)
        {
            // construct a line with required length and fields at expected positions
            var arr = new char[300];
            for (int i = 0; i < arr.Length; i++) arr[i] = ' ';

            // tipo registro 0-1
            Array.Copy("01".ToCharArray(), 0, arr, 0, 2);
            // data pregão 2-9 yyyyMMdd
            var dp = dataPregao.PadLeft(8, '0');
            Array.Copy(dp.ToCharArray(), 0, arr, 2, 8);
            // codbdi 10-11 -> 02
            Array.Copy("02".ToCharArray(), 0, arr, 10, 2);
            // ticker 12-23 (12 chars)
            var t = ticker.PadRight(12).Substring(0,12);
            Array.Copy(t.ToCharArray(), 0, arr, 12, 12);
            // tpmerc 24-26
            Array.Copy("010".ToCharArray(), 0, arr, 24, 3);

            // preAbe at 56 length 13
            var pa = precoAberturaCents.ToString().PadLeft(13, '0');
            Array.Copy(pa.ToCharArray(), 0, arr, 56, 13);
            var pm = precoMaxCents.ToString().PadLeft(13, '0');
            Array.Copy(pm.ToCharArray(), 0, arr, 69, 13);
            var pmin = precoMinCents.ToString().PadLeft(13, '0');
            Array.Copy(pmin.ToCharArray(), 0, arr, 82, 13);
            var pu = precoUltCents.ToString().PadLeft(13, '0');
            Array.Copy(pu.ToCharArray(), 0, arr, 108, 13);

            return new string(arr);
        }

        [Fact]
        public async Task ImportarAsync_ParsesFile_InsertsRecords()
        {
            var dir = Path.Combine(Path.GetTempPath(), "cotacoes_test_" + Guid.NewGuid());
            Directory.CreateDirectory(dir);
            var filename = Path.Combine(dir, "COTAHIST_D20260305.TXT");

            try
            {
                // create two lines
                var line1 = BuildLine("20260305", "PETR4", 3500, 3700, 3400, 3700); // 35.00 etc
                var line2 = BuildLine("20260305", "VALE3", 6000, 6500, 5900, 6500);
                File.WriteAllLines(filename, new[] { line1, line2 }, Encoding.GetEncoding("ISO-8859-1"));

                var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
                using var db = new AppDbContext(options);
                var svc = new CotacaoImportService(db);

                var result = await svc.ImportarAsync(dir);
                Assert.Equal(2, result);

                var cotacoes = db.Cotacoes.ToList();
                cotacoes.Count.Should().Be(2);
                cotacoes.Any(c => c.Ticker.Trim() == "PETR4" && c.PrecoFechamento == 37.00m).Should().BeTrue();
                cotacoes.Any(c => c.Ticker.Trim() == "VALE3" && c.PrecoFechamento == 65.00m).Should().BeTrue();
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }
    }
}
