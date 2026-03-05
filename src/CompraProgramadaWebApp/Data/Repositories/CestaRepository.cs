using CompraProgramada.Models;
using CompraProgramadaWebApp.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CompraProgramadaWebApp.Data.Repositories
{
    public class CestaRepository : ICestaRepository
    {
        private readonly AppDbContext _context;
        public CestaRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<CestaRecomendacaoViewModel> AddAsync(CestaRecomendacaoViewModel cesta, IEnumerable<ItemCestaViewModel> itens)
        {
            _context.CestasRecomendacao.Add(cesta);
            await _context.SaveChangesAsync();

            foreach (var item in itens)
            {
                item.CestaId = cesta.Id;
                await _context.ItemCestas.AddAsync(item);
            }

            await _context.SaveChangesAsync();
            return cesta;
        }

        public async Task<CestaRecomendacaoViewModel?> GetAtualAsync()
        {
            return await _context.CestasRecomendacao.OrderByDescending(c => c.DataCriacao).FirstOrDefaultAsync(c => c.Ativa);
        }

        public async Task<IEnumerable<CestaRecomendacaoViewModel>> GetHistoricoAsync()
        {
            return await _context.CestasRecomendacao.OrderByDescending(c => c.DataCriacao).ToListAsync();
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }
    }
}
