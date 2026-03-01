using Microsoft.EntityFrameworkCore;
using CompraProgramadaWebApp.Models;
using CompraProgramada.Models;

namespace CompraProgramadaWebApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }

        public DbSet<ClienteViewModel> Clientes { get; set; } = null!;
        public DbSet<ContaGraficaViewModel> ContaGraficas { get; set; } = null!;
        public DbSet<CestaRecomendacaoViewModel> CestasRecomendacao { get; set; } = null!;
        public DbSet<CotacaoViewModel> Cotacoes { get; set; } = null!;
        public DbSet<CustodiaViewModel> Custodias { get; set; } = null!;
        public DbSet<DistribuicaoViewModel> Distribuicoes { get; set; } = null!;
        public DbSet<EventoIRViewModel> EventosIR { get; set; } = null!;
        public DbSet<ItemCestaViewModel> ItemCestas { get; set; } = null!;
        public DbSet<OrdemCompraViewModel> OrdensCompra { get; set; } = null!;
        public DbSet<RebalanceamentoViewModel> Rebalanceamentos { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
