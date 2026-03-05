-- Script para inserir uma Conta Master com Id = 1 e o Cliente relacionado

USE CompraProgramadaDB;

-- Desativa checagens de chave estrangeira temporariamente
SET @OLD_FK = @@FOREIGN_KEY_CHECKS;
SET FOREIGN_KEY_CHECKS=0;

-- Insere cliente com Id = 1 caso não exista
INSERT INTO Clientes (Id, Nome, CPF, Email, ValorMensal, Ativo, DataAdesao)
SELECT 1, 'Cliente Master', '00000000000', 'master@localhost', 0.00, TRUE, NOW()
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM Clientes WHERE Id = 1);

-- Insere conta gráfica MASTER com Id = 1 caso não exista
INSERT INTO ContasGraficas (Id, ClienteId, NumeroConta, Tipo, DataCriacao)
SELECT 1, 1, 'MASTER-0001', 'MASTER', NOW()
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ContasGraficas WHERE Id = 1);

-- Ajusta o AUTO_INCREMENT para evitar conflitos futuros (define para max(Id)+1)
SET @next := (SELECT COALESCE(MAX(Id),0) + 1 FROM Clientes);
SET @sql := CONCAT('ALTER TABLE Clientes AUTO_INCREMENT = ', @next);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @next := (SELECT COALESCE(MAX(Id),0) + 1 FROM ContasGraficas);
SET @sql := CONCAT('ALTER TABLE ContasGraficas AUTO_INCREMENT = ', @next);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- Restaura checagens de chave estrangeira
SET FOREIGN_KEY_CHECKS=@OLD_FK;

-- Fim do script
