using System;
using System.Threading.Tasks;
using System.Text.Json;
using Confluent.Kafka;

namespace CompraProgramadaWebApp.Services
{
    public class KafkaProducerService : IKafkaProducerService, IDisposable
    {
        private readonly IProducer<Null, string>? _producer;
        private readonly bool _enabled;

        public KafkaProducerService()
        {
            var conn = Environment.GetEnvironmentVariable("CONEXAO_KAFKA");
            if (string.IsNullOrWhiteSpace(conn))
            {
                _enabled = false;
                return;
            }

            var config = new ProducerConfig { BootstrapServers = conn };
            _producer = new ProducerBuilder<Null, string>(config).Build();
            _enabled = true;
        }

        public async Task PublishAsync(string topic, string message)
        {
            if (!_enabled || _producer == null)
                return;

            try
            {
                var msg = new Message<Null, string> { Value = message };

                var delivery = await _producer.ProduceAsync(topic, msg).ConfigureAwait(false);
            }
            catch
            {
                //TODO: Adicionar logs
            }
        }

        public void Dispose()
        {
            try
            {
                _producer?.Flush(TimeSpan.FromSeconds(2));
                _producer?.Dispose();
            }
            catch 
            {
                //TODO: Adicionar logs
            }
        }
    }
}
