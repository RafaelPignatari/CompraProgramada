using CompraProgramada.Models;
using CompraProgramadaWebApp.Services;
using System.Text.Json;

namespace CompraProgramadaWebApp.Helpers
{
    /// <summary>
    /// Helper para cálculo e publicação de IR (Imposto de Renda) no Kafka.
    /// </summary>
    public static class IRHelper
    {
        /// <summary>
        /// Publica evento de IR Dedo-Duro (0,005% sobre o valor da operação) no Kafka.
        /// RN-053 a RN-056: IR Dedo-Duro para compras.
        /// </summary>
        /// <param name="kafkaProducer">Serviço de publicação Kafka</param>
        /// <param name="cliente">Cliente relacionado à operação</param>
        /// <param name="ticker">Ticker do ativo</param>
        /// <param name="quantidade">Quantidade de ações</param>
        /// <param name="precoUnitario">Preço unitário da ação</param>
        /// <param name="tipoOperacao">Tipo da operação (COMPRA, VENDA, etc.)</param>
        /// <param name="dataOperacao">Data da operação</param>
        /// <returns>Task representando a operação assíncrona</returns>
        public static async Task PublicarIRDedoDuroAsync(
            IKafkaProducerService? kafkaProducer,
            ClienteViewModel cliente,
            string ticker,
            int quantidade,
            decimal precoUnitario,
            string tipoOperacao,
            DateTime dataOperacao)
        {
            try
            {
                if (kafkaProducer == null || cliente == null)
                    return;

                var valorOperacao = Math.Round(quantidade * precoUnitario, 2);
                const decimal aliquota = 0.00005m; // 0.005%
                var valorIr = Math.Round(valorOperacao * aliquota, 2);

                var mensagem = new
                {
                    tipo = "IR_DEDO_DURO",
                    clienteId = cliente.Id,
                    cpf = cliente.CPF,
                    ticker = ticker,
                    tipoOperacao = tipoOperacao,
                    quantidade = quantidade,
                    precoUnitario = Math.Round(precoUnitario, 2),
                    valorOperacao = valorOperacao,
                    aliquota = aliquota,
                    valorIR = valorIr,
                    dataOperacao = dataOperacao.ToString("o") // ISO 8601 format
                };

                var json = JsonSerializer.Serialize(mensagem);
                await kafkaProducer.PublishAsync("ir-events", json);
            }
            catch
            {
                // Não propagar exceção para não interromper o fluxo
                // Em produção, adicionar logs adequados
            }
        }

        /// <summary>
        /// Calcula o valor do IR Dedo-Duro (sem publicar no Kafka).
        /// </summary>
        /// <param name="valorOperacao">Valor total da operação</param>
        /// <returns>Valor do IR calculado (0,005% do valor da operação)</returns>
        public static decimal CalcularIRDedoDuro(decimal valorOperacao)
        {
            const decimal aliquota = 0.00005m; // 0.005%
            return Math.Round(valorOperacao * aliquota, 2);
        }

        /// <summary>
        /// Publica evento de IR sobre vendas (20% sobre lucro) no Kafka.
        /// RN-057 a RN-062: IR sobre vendas mensais acima de R$ 20.000.
        /// </summary>
        /// <param name="kafkaProducer">Serviço de publicação Kafka</param>
        /// <param name="cliente">Cliente relacionado à venda</param>
        /// <param name="mesReferencia">Mês de referência (formato: yyyy-MM)</param>
        /// <param name="totalVendasMes">Total de vendas no mês</param>
        /// <param name="lucroLiquido">Lucro líquido apurado</param>
        /// <param name="valorIR">Valor do IR calculado</param>
        /// <param name="detalhes">Detalhes das vendas (opcional)</param>
        /// <param name="dataCalculo">Data do cálculo</param>
        /// <returns>Task representando a operação assíncrona</returns>
        public static async Task PublicarIRVendaAsync(
            IKafkaProducerService? kafkaProducer,
            ClienteViewModel cliente,
            string mesReferencia,
            decimal totalVendasMes,
            decimal lucroLiquido,
            decimal valorIR,
            object? detalhes,
            DateTime dataCalculo)
        {
            try
            {
                if (kafkaProducer == null || cliente == null)
                    return;

                var mensagem = new
                {
                    tipo = "IR_VENDA",
                    clienteId = cliente.Id,
                    cpf = cliente.CPF,
                    mesReferencia = mesReferencia,
                    totalVendasMes = Math.Round(totalVendasMes, 2),
                    lucroLiquido = Math.Round(lucroLiquido, 2),
                    aliquota = 0.20m,
                    valorIR = Math.Round(valorIR, 2),
                    detalhes = detalhes,
                    dataCalculo = dataCalculo.ToString("o")
                };

                var json = JsonSerializer.Serialize(mensagem);
                await kafkaProducer.PublishAsync("ir-events", json);
            }
            catch
            {
                // Não propagar exceção para não interromper o fluxo
                // Em produção, adicionar logs adequados
            }
        }

        /// <summary>
        /// Calcula o IR sobre vendas considerando a regra de isenção (RN-058 a RN-061).
        /// </summary>
        /// <param name="totalVendasMes">Total de vendas no mês</param>
        /// <param name="lucroLiquido">Lucro líquido apurado</param>
        /// <returns>Valor do IR (0 se isento ou sem lucro, 20% do lucro caso contrário)</returns>
        public static decimal CalcularIRVenda(decimal totalVendasMes, decimal lucroLiquido)
        {
            // RN-058: Se total de vendas <= R$ 20.000, isento
            if (totalVendasMes <= 20000m)
                return 0m;

            // RN-061: Se lucro <= 0, não há IR
            if (lucroLiquido <= 0m)
                return 0m;

            // RN-059: 20% sobre o lucro líquido
            return Math.Round(lucroLiquido * 0.20m, 2);
        }
    }
}
