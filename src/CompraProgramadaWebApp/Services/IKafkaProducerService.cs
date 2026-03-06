using System.Threading.Tasks;

namespace CompraProgramadaWebApp.Services
{
    public interface IKafkaProducerService
    {
        Task PublishAsync(string topic, string message);
    }
}
