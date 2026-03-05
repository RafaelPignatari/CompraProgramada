using System.Collections.Generic;
using System.Threading.Tasks;
using CompraProgramada.Models;

namespace CompraProgramadaWebApp.Services
{
    public interface IContaMasterService
    {
        Task<object> GetCustodiaAsync();
    }
}
