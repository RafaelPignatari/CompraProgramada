CREATE DATABASE IF NOT EXISTS CompraProgramadaDB;
USE CompraProgramadaDB;

-- Remover tabelas se existirem (ordem reversa de dependências)
DROP TABLE IF EXISTS Distribuicoes;
DROP TABLE IF EXISTS ItensCesta;
DROP TABLE IF EXISTS Custodias;
DROP TABLE IF EXISTS OrdensCompra;
DROP TABLE IF EXISTS ContasGraficas;
DROP TABLE IF EXISTS Rebalanceamentos;
DROP TABLE IF EXISTS EventosIR;
DROP TABLE IF EXISTS Cotacoes;
DROP TABLE IF EXISTS CestasRecomendacao;
DROP TABLE IF EXISTS Clientes;

-- Tabela Clientes
CREATE TABLE Clientes (
  Id BIGINT AUTO_INCREMENT PRIMARY KEY,
  Nome VARCHAR(200) NOT NULL,
  CPF VARCHAR(11) NOT NULL UNIQUE,
  Email VARCHAR(200),
  ValorMensal DECIMAL(18,2) NOT NULL,
  Ativo BOOLEAN NOT NULL DEFAULT TRUE,
  DataAdesao DATETIME NOT NULL
);

-- Tabela CestasRecomendacao
CREATE TABLE CestasRecomendacao (
  Id BIGINT AUTO_INCREMENT PRIMARY KEY,
  Nome VARCHAR(100) NOT NULL,
  Ativa BOOLEAN NOT NULL DEFAULT TRUE,
  DataCriacao DATETIME NOT NULL,
  DataDesativacao DATETIME NULL
);

-- Tabela Cotacoes
CREATE TABLE Cotacoes (
  Id BIGINT AUTO_INCREMENT PRIMARY KEY,
  DataPregao DATE NOT NULL,
  Ticker VARCHAR(10) NOT NULL,
  PrecoAbertura DECIMAL(18,4) NULL,
  PrecoFechamento DECIMAL(18,4) NULL,
  PrecoMaximo DECIMAL(18,4) NULL,
  PrecoMinimo DECIMAL(18,4) NULL
);

-- Tabela ContasGraficas
CREATE TABLE ContasGraficas (
  Id BIGINT AUTO_INCREMENT PRIMARY KEY,
  ClienteId BIGINT NOT NULL,
  NumeroConta VARCHAR(20) NOT NULL UNIQUE,
  Tipo ENUM('MASTER','FILHOTE') NOT NULL,
  DataCriacao DATETIME NOT NULL
);

-- Tabela Rebalanceamentos
CREATE TABLE Rebalanceamentos (
  Id BIGINT AUTO_INCREMENT PRIMARY KEY,
  ClienteId BIGINT NOT NULL,
  Tipo ENUM('MUDANCA_CESTA','DESVIO') NOT NULL,
  TickerVendido VARCHAR(10) NOT NULL,
  TickerComprado VARCHAR(10) NOT NULL,
  ValorVenda DECIMAL(18,2) NOT NULL,
  DataRebalanceamento DATETIME NOT NULL
);

-- Tabela Custodias
CREATE TABLE Custodias (
  Id BIGINT AUTO_INCREMENT PRIMARY KEY,
  ContaGraficaId BIGINT NOT NULL,
  Ticker VARCHAR(10) NOT NULL,
  Quantidade INT NOT NULL,
  PrecoMedio DECIMAL(18,4) NOT NULL,
  DataUltimaAtualizacao DATETIME NOT NULL
);

-- Tabela OrdensCompra
CREATE TABLE OrdensCompra (
  Id BIGINT AUTO_INCREMENT PRIMARY KEY,
  ContaMasterId BIGINT NOT NULL,
  Ticker VARCHAR(10) NOT NULL,
  Quantidade INT NOT NULL,
  PrecoUnitario DECIMAL(18,4) NOT NULL,
  TipoMercado ENUM('LOTE','FRACIONARIO') NOT NULL,
  DataExecucao DATETIME NOT NULL
);

-- Tabela Distribuicoes
CREATE TABLE Distribuicoes (
  Id BIGINT AUTO_INCREMENT PRIMARY KEY,
  OrdemCompraId BIGINT NOT NULL,
  CustodiaFilhoId BIGINT NOT NULL,
  Ticker VARCHAR(10) NOT NULL,
  Quantidade INT NOT NULL,
  PrecoUnitario DECIMAL(18,4) NOT NULL,
  DataDistribuicao DATETIME NOT NULL
);

-- Tabela EventosIR
CREATE TABLE EventosIR (
  Id BIGINT AUTO_INCREMENT PRIMARY KEY,
  ClienteId BIGINT NOT NULL,
  Tipo ENUM('DEDO_DURO','IR_VENDA') NOT NULL,
  ValorBase DECIMAL(18,2) NULL,
  ValorIR DECIMAL(18,2) NULL,
  PublicadoKafka BOOLEAN NOT NULL,
  DataEvento DATETIME NOT NULL
);

-- Tabela ItensCesta
CREATE TABLE ItensCesta (
  Id BIGINT AUTO_INCREMENT PRIMARY KEY,
  CestaId BIGINT NOT NULL,
  Ticker VARCHAR(10) NOT NULL,
  Percentual DECIMAL(5,2) NOT NULL
);

-- Foreign Keys
ALTER TABLE ContasGraficas
  ADD CONSTRAINT FK_contasgraficas_cliente FOREIGN KEY (ClienteId) REFERENCES Clientes(Id) ON DELETE CASCADE;

ALTER TABLE Custodias
  ADD CONSTRAINT FK_custodias_contagrafica FOREIGN KEY (ContaGraficaId) REFERENCES ContasGraficas(Id) ON DELETE CASCADE;

ALTER TABLE OrdensCompra
  ADD CONSTRAINT FK_ordenscompra_contamaster FOREIGN KEY (ContaMasterId) REFERENCES ContasGraficas(Id) ON DELETE RESTRICT;

ALTER TABLE Distribuicoes
  ADD CONSTRAINT FK_distribuicoes_ordemcompra FOREIGN KEY (OrdemCompraId) REFERENCES OrdensCompra(Id) ON DELETE CASCADE,
  ADD CONSTRAINT FK_distribuicoes_custodia FOREIGN KEY (CustodiaFilhoId) REFERENCES Custodias(Id) ON DELETE RESTRICT;

ALTER TABLE Rebalanceamentos
  ADD CONSTRAINT FK_rebalanceamentos_cliente FOREIGN KEY (ClienteId) REFERENCES Clientes(Id) ON DELETE CASCADE;

ALTER TABLE EventosIR
  ADD CONSTRAINT FK_eventosir_cliente FOREIGN KEY (ClienteId) REFERENCES Clientes(Id) ON DELETE CASCADE;

ALTER TABLE ItensCesta
  ADD CONSTRAINT FK_itenscesta_cesta FOREIGN KEY (CestaId) REFERENCES CestasRecomendacao(Id) ON DELETE CASCADE;

-- Fim do script