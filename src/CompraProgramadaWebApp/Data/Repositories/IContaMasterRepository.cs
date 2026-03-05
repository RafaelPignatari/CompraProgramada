using System.Collections.Generic;
using System.Threading.Tasks;
using CompraProgramada.Models;

namespace CompraProgramadaWebApp.Data.Repositories
{
    public interface IContaMasterRepository
    {
        Task<IEnumerable<CustodiaViewModel>> GetCustodiaAsync();
    }
}
