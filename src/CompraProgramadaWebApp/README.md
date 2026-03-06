# 📈 Sistema de Compra Programada de Ações - Itaú Corretora

Sistema de compra programada de ações com rebalanceamento automático de carteira baseado em uma cesta de recomendação Top Five.

## 📋 Índice

- [Visão Geral](#visão-geral)
- [Arquitetura](#arquitetura)
- [Tecnologias Utilizadas](#tecnologias-utilizadas)
- [Estrutura do Projeto](#estrutura-do-projeto)
- [Configuração e Execução](#configuração-e-execução)
- [Funcionalidades Principais](#funcionalidades-principais)
- [API Endpoints](#api-endpoints)
- [Regras de Negócio](#regras-de-negócio)
- [Testes](#testes)
- [Considerações de Produção](#considerações-de-produção)
- [Melhorias Futuras](#melhorias-futuras)

## 🎯 Visão Geral

O sistema permite que clientes façam aportes mensais para compra programada de ações baseada em uma cesta de recomendação definida pela corretora. O sistema realiza:

- Compras programadas em 3 datas por mês (dias 5, 15 e 25)
- Agrupamento de pedidos de múltiplos clientes
- Distribuição proporcional das ações compradas
- Rebalanceamento automático quando a cesta é alterada
- Cálculo de preço médio e rentabilidade
- Publicação de eventos de IR no Kafka

## 🏗️ Arquitetura

### Decisão Arquitetural

O projeto foi desenvolvido como uma **WebApp ASP.NET Core Razor Pages** que unifica frontend e APIs REST no mesmo projeto. Esta decisão foi tomada para:

- ✅ **Poupar tempo de desenvolvimento** durante a fase inicial
- ✅ **Simplificar o deployment** em ambiente de desenvolvimento
- ✅ **Reduzir complexidade** de configuração e orquestração

> **⚠️ Nota para Produção:** A longo prazo, é recomendado **separar o frontend das APIs** para possibilitar:
> - Escalabilidade independente dos componentes
> - Deployment e versionamento independentes
> - Melhor distribuição de carga
> - Arquitetura de microsserviços

### Padrão Repository

O projeto utiliza o **padrão Repository** para garantir separação clara de responsabilidades:

```
┌─────────────────┐
│   Controllers   │  ← Endpoints de API, validações, retornos HTTP
└────────┬────────┘
         │
┌────────▼────────┐
│    Services     │  ← Regras de negócio, orquestração
└────────┬────────┘
         │
┌────────▼────────┐
│  Repositories   │  ← Acesso a dados, queries ao banco
└────────┬────────┘
         │
┌────────▼────────┐
│   DbContext     │  ← Entity Framework Core
└─────────────────┘
```

**Responsabilidades:**
- **Controllers**: Recepção de requisições HTTP, validação de entrada, formatação de respostas
- **Services**: Implementação de regras de negócio, orquestração entre repositórios
- **Repositories**: Consultas e persistência de dados, abstraindo acesso ao banco

## 🛠️ Tecnologias Utilizadas

- **.NET 10** - Framework principal
- **ASP.NET Core Razor Pages** - Frontend e APIs
- **Entity Framework Core** - ORM
- **MySQL 8.0** - Banco de dados relacional
- **Apache Kafka** - Message broker para eventos de IR
- **Docker & Docker Compose** - Containerização
- **Swagger/OpenAPI** - Documentação de API
- **xUnit** - Framework de testes

## 📁 Estrutura do Projeto

```
CompraProgramada/
├── src/
│   └── CompraProgramadaWebApp/
│       ├── Controllers/          # Controllers de API e páginas
│       │   ├── Api/
│       │   │   ├── Admin/       # Endpoints administrativos
│       │   │   ├── ClientesController.cs
│       │   │   ├── ComprasProgramadasController.cs
│       │   │   └── CotacoesController.cs
│       │   ├── HomeController.cs
│       │   └── RentabilidadeController.cs
│       ├── Services/            # Regras de negócio
│       │   ├── CestaService.cs
│       │   ├── ClienteService.cs
│       │   ├── CompraProgramadaService.cs
│       │   ├── CotacaoService.cs
│       │   ├── RebalanceamentoService.cs
│       │   └── KafkaProducerService.cs
│       ├── Data/
│       │   ├── Repositories/    # Acesso a dados
│       │   └── AppDbContext.cs
│       ├── Models/              # ViewModels e DTOs
│       ├── Helpers/             # Classes auxiliares
│       └── Utils/               # Utilitários
├── tests/
│   └── CompraProgramada.Tests/  # Testes unitários e de integração
├── database/                     # Scripts SQL
│   ├── create-database.sql
│   └── insert-conta-master.sql
├── docker-compose.yml           # Configuração Docker
├── regras-negocio-detalhadas.md # Especificação completa
├── REBALANCEAMENTO.md           # Documentação de rebalanceamento
└── README.md                    # Este arquivo
```

## 🚀 Configuração e Execução

### Pré-requisitos

- [Docker](https://www.docker.com/get-started) instalado
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) instalado
- Git

### Passo 1: Subir Infraestrutura (Docker)

```bash
# Navegue até a raiz do repositório
cd CompraProgramada

# Suba os containers (MySQL, Kafka e Kafka UI)
docker-compose up -d

# Verifique se os containers estão rodando
docker ps
```

**Serviços disponíveis:**
- MySQL: `localhost:3306`
- Kafka: `localhost:9092` (interno) / `localhost:29092` (externo)
- Kafka UI: http://localhost:8080

### Passo 2: Criar Banco de Dados

```bash
# Execute o script de criação do banco
docker exec -i mysql mysql -u rafael -p souumasenhadeteste < database/create-database.sql

# Execute o script de inserção da conta master
docker exec -i mysql mysql -u rafael -p souumasenhadeteste CompraProgramadaDB < database/insert-conta-master.sql
```

### Passo 3: Configurar String de Conexão

A aplicação usa a variável de ambiente `CONEXAO_BANCO`. Se não estiver definida, usa a connection string do `appsettings.json`.

**Opção 1: Via variável de ambiente**

```bash
# Windows (PowerShell)
$env:CONEXAO_BANCO="Server=localhost;Port=3306;Database=CompraProgramadaDB;User=rafael;Password=souumasenhadeteste;"

# Linux/macOS
export CONEXAO_BANCO="Server=localhost;Port=3306;Database=CompraProgramadaDB;User=rafael;Password=souumasenhadeteste;"
```

**Opção 2: Via appsettings.json** (já configurado)

### Passo 4: Executar a Aplicação

```bash
# Navegue até o projeto
cd src/CompraProgramadaWebApp

# Restaure as dependências
dotnet restore

# Execute a aplicação
dotnet run
```

A aplicação estará disponível em:
- HTTPS: https://localhost:5001
- HTTP: http://localhost:5000
- Swagger: https://localhost:5001/swagger

### Passo 5: Importar Cotações (Opcional)

Para testar as compras programadas, você precisa importar cotações históricas:

```bash
# Via API (POST)
curl -X POST "https://localhost:5001/api/cotacoes/importar" \
  -H "Content-Type: application/json" \
  -d '{"caminhoArquivo": "/caminho/para/COTAHIST.TXT"}'
```

## 🎨 Funcionalidades Principais

### 1. Gestão de Clientes

- **Adesão**: Cadastro de cliente com valor mensal de aporte
- **Saída**: Desativação do cliente (mantém custódia)
- **Alteração de Valor**: Mudança do valor mensal de aporte

### 2. Cesta de Recomendação (Top Five)

- **Criação/Atualização**: Definir 5 ativos com percentuais (total = 100%)
- **Histórico**: Consultar cestas anteriores
- **Rebalanceamento Automático**: Disparo ao alterar a cesta

### 3. Compra Programada

- **Execução**: Dias 5, 15 e 25 de cada mês
- **Agrupamento**: Consolidação de aportes de todos os clientes ativos
- **Distribuição**: Proporcional ao aporte de cada cliente
- **Lote Padrão vs Fracionário**: Automático (≥100 ações = lote)

### 4. Rebalanceamento

- **Por Mudança de Cesta**: Automático ao alterar a composição
- **Por Desvio**: Manual, quando carteira diverge do alvo (>5%)
- **Cálculo de IR**: Dedo-duro (compras) e sobre lucro (vendas)

### 5. Rentabilidade

- **P/L por Ativo**: Lucro/prejuízo individual
- **P/L Total**: Consolidado da carteira
- **Rentabilidade %**: Percentual de ganho/perda
- **Composição**: Percentual de cada ativo na carteira

## 📡 API Endpoints

### Clientes

```http
POST   /api/clientes                    # Adesão de cliente
PUT    /api/clientes/{id}/saida         # Saída do produto
PUT    /api/clientes/{id}/alterar-valor # Alterar valor mensal
```

### Cesta de Recomendação

```http
POST   /api/admin/cesta                 # Criar/atualizar cesta
GET    /api/admin/cesta/atual           # Obter cesta ativa
GET    /api/admin/cesta/historico       # Histórico de cestas
POST   /api/admin/cesta/rebalancear/{clienteId} # Rebalancear por desvio
```

### Compras Programadas

```http
POST   /api/compras-programadas/executar # Executar compra programada
```

### Cotações

```http
POST   /api/cotacoes/importar           # Importar arquivo COTAHIST
GET    /api/cotacoes/{ticker}           # Consultar cotação atual
```

### Conta Master

```http
GET    /api/admin/conta-master/custodia # Consultar custódia master
```

**📖 Documentação completa:** Acesse `/swagger` após iniciar a aplicação.

## 📜 Regras de Negócio

O sistema implementa 70+ regras de negócio detalhadas, incluindo:

- **RN-001 a RN-013**: Gestão de clientes (adesão, saída, alteração)
- **RN-014 a RN-019**: Cesta de recomendação
- **RN-020 a RN-044**: Motor de compra programada
- **RN-045 a RN-052**: Rebalanceamento de carteira
- **RN-053 a RN-062**: Cálculo e publicação de IR
- **RN-063 a RN-070**: Tela de rentabilidade

📄 **Documentação completa**: Veja [regras-negocio-detalhadas.md](regras-negocio-detalhadas.md)

📄 **Documentação de rebalanceamento**: Veja [REBALANCEAMENTO.md](REBALANCEAMENTO.md)

## 🧪 Testes

O projeto inclui **testes unitários e de integração** para garantir a qualidade e confiabilidade do sistema.

### Executar Testes

```bash
# Navegue até o diretório de testes
cd tests/CompraProgramada.Tests

# Execute todos os testes
dotnet test

# Execute com cobertura
dotnet test /p:CollectCoverage=true
```

### Cobertura de Testes

Os testes cobrem:

- ✅ **Services**: Regras de negócio (CestaService, RebalanceamentoService, CompraProgramadaService)
- ✅ **Repositories**: Acesso a dados
- ✅ **Controllers**: Endpoints de API
- ✅ **Cálculos**: Preço médio, distribuição proporcional, IR
- ✅ **Integração**: Fluxos completos end-to-end

## 🔐 Considerações de Segurança

### Ambiente de Desenvolvimento

> **⚠️ ATENÇÃO**: As credenciais do banco de dados estão **propositalmente expostas** no `docker-compose.yml` para facilitar o desenvolvimento e avaliação do projeto.

```yaml
MYSQL_USER: rafael
MYSQL_PASSWORD: souumasenhadeteste
```

### Ambiente de Produção

Em um ambiente de CI/CD e produção, as credenciais devem ser:

- ✅ Armazenadas em **Azure Key Vault** (Azure) ou **AWS Secrets Manager** (AWS)
- ✅ Injetadas via **variáveis de ambiente** no pipeline
- ✅ Configuradas como **Secrets** no GitHub Actions/Azure DevOps
- ✅ Rotacionadas periodicamente
- ✅ Nunca commitadas no código-fonte

**Exemplo de configuração ideal:**

```yaml
# GitHub Actions
env:
  CONEXAO_BANCO: ${{ secrets.DATABASE_CONNECTION_STRING }}
  KAFKA_BOOTSTRAP_SERVERS: ${{ secrets.KAFKA_SERVERS }}
```

## ⚙️ Considerações de Produção

### Motor de Compras

**Implementação Atual:**
- ✅ Trigger **manual via API**: `POST /api/compras-programadas/executar`
- ✅ Adequado para desenvolvimento e testes

**Recomendação para Produção:**

Migrar para **Azure Functions** (ou AWS Lambda) com trigger agendado:

```csharp
[FunctionName("CompraProgramadaScheduled")]
public async Task Run(
    [TimerTrigger("0 0 10 5,15,25 * *")] TimerInfo timer,
    ILogger log)
{
    log.LogInformation($"Executando compra programada: {DateTime.UtcNow}");
    await _compraProgramadaService.ExecutarCompraAsync();
}
```

**Vantagens:**
- ⚡ Execução automática baseada em cron
- 💰 Custo reduzido (pay-per-execution)
- 📈 Escalabilidade automática
- 🔄 Retry automático em caso de falha

### CI/CD

**Status Atual:**
- ❌ Não implementado devido a restrições de tempo

**Recomendação para Produção:**

Implementar pipeline completo com:

```yaml
# Exemplo: Azure DevOps / GitHub Actions
stages:
  - build:
      - dotnet restore
      - dotnet build
      - dotnet test
  - quality:
      - SonarQube analysis
      - Code coverage check
  - deploy:
      - Staging environment
      - Production environment (com aprovação manual)
```

**Ferramentas sugeridas:**
- GitHub Actions

## 🚀 Melhorias Futuras

### Arquitetura

- [ ] Separar frontend das APIs
- [ ] Implementar API Gateway (Azure API Management / AWS API Gateway)

### Funcionalidades

- [ ] Dashboard administrativo em tempo real
- [ ] Notificações aos clientes (email/SMS) sobre compras e rebalanceamentos
- [ ] Relatórios de IR exportáveis (PDF/Excel)
- [ ] Compensação de prejuízos fiscais entre meses
- [ ] Simulador de rentabilidade
- [ ] Integração com APIs de cotação em tempo real

### Infraestrutura

- [ ] Configurar Application Insights / CloudWatch para monitoramento
- [ ] Implementar cache (Redis) para cotações
- [ ] Implementar autenticação e autorização (Azure AD / JWT)

### Testes

- [ ] Aumentar cobertura para > 70%
- [ ] Implementar testes de carga (JMeter/k6)
- [ ] Testes de regressão visual
- [ ] Testes de segurança (OWASP)

### DevOps

- [ ] CI/CD completo (build, test, deploy)

## 📝 Licença

Este projeto foi desenvolvido como parte de um desafio técnico.

## 📞 Contato

**Desenvolvedor:** Rafael Pignatari

**GitHub:** [github.com/RafaelPignatari](https://github.com/RafaelPignatari)

---

⭐ Se este projeto foi útil para você, considere dar uma estrela no repositório!
