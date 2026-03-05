namespace CompraProgramadaWebApp.Helpers
{
    public static class Constantes
    {
        public const string CLIENTE_CPF_DUPLICADO = "CPF_DUPLICADO";
        public const string QTD_ATIVOS_INVALIDA = "QUANTIDADE_ATIVOS_INVALIDA";
        public const string PERCENTUAIS_INVALIDOS = "PERCENTUAIS_INVALIDOS";
        public const string VALOR_MENSAL_INVALIDO = "VALOR_MENSAL_INVALIDO";
        public const string CLIENTE_NAO_ENCONTRADO = "CLIENTE_NAO_ENCONTRADO";
        public const string CESTA_NAO_ENCONTRADA = "CESTA_NAO_ENCONTRADA";
        public const string CLIENTE_JA_INATIVO = "CLIENTE_JA_INATIVO";

        public struct Mensagens
        {
            public const string POSICAO_ENCERRADA = "Adesao encerrada. Sua posicao em custodia foi mantida.";
            public const string VALOR_MENSAL_ATUALIZADO = "Valor mensal atualizado. O novo valor sera considerado a partir da proxima data de compra.";

            public const string CPF_DUPLICADO = "CPF ja cadastrado no sistema.";
            public const string QUANTIDADE_ATIVOS_INVALIDA = "A cesta deve conter exatamente 5 ativos. Quantidade informada: {0}";
            public const string PERCENTUAIS_INVALIDOS = "A soma dos percentuais deve ser exatamente 100%. Soma atual: {0}%";
            public const string VALOR_MENSAL_INVALIDO = "O valor mensal minimo e de R$ 100,00.";
            public const string CLIENTE_NAO_ENCONTRADO = "Cliente nao encontrado.";
            public const string CESTA_NAO_ENCONTRADA = "Cesta não encontrada.";
            public const string CLIENTE_JA_INATIVO = "Cliente já inativo.";

            public const string ERRO_GENERICO = "A API não pôde processar a solicitação. Por favor, cheque os parâmetros na documentação.";
        }
    }
}
