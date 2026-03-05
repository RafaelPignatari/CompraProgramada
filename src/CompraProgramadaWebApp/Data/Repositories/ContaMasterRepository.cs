using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CompraProgramada.Models;
using CompraProgramadaWebApp.Data;
using Microsoft.EntityFrameworkCore;

namespace CompraProgramadaWebApp.Data.Repositories
{
    public class ContaMasterRepository : IContaMasterRepository
    {
        private readonly AppDbContext _context;
        public ContaMasterRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CustodiaViewModel>> GetCustodiaAsync()
        {
            return await _context.Custodias.ToListAsync();
        }
    }
}
