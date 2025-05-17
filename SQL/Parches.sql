-- ================================================
-- Parches.sql – Ajustes para Vincula_participante
-- ================================================
DECLARE @DBName SYSNAME = N'Cronometraje_Carreras';
IF DB_ID(@DBName) IS NULL
BEGIN
    RAISERROR('La base de datos %s no existe.',16,1,@DBName);
    RETURN;
END
EXEC(N'USE [' + @DBName + '];');
GO

/*============================================================================
  1) ELIMINAR TRIGGERS EXISTENTES
============================================================================*/
DROP TRIGGER IF EXISTS dbo.trg_AssignDefaultChip;
DROP TRIGGER IF EXISTS dbo.trg_EnforceNumeroPorCarrera;
DROP TRIGGER IF EXISTS dbo.trg_EnforceSingleParticipation;
DROP TRIGGER IF EXISTS dbo.trg_PreventOrphan_Corredor;
DROP TRIGGER IF EXISTS dbo.trg_PreventOrphan_Carrera;
DROP TRIGGER IF EXISTS dbo.INSTEADOF_DELETE_CARRERA;
DROP TRIGGER IF EXISTS dbo.trg_PreventDeleteVinculaConTiempo;
GO

/*============================================================================
  2) ELIMINAR CONSTRAINTS Y COLUMNAS EXISTENTES
============================================================================*/
/* 2.1) FK y UQ sobre folio_chip */
IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name = 'FK__TIEMPO__folio_ch__5535A963')
  ALTER TABLE dbo.TIEMPO DROP CONSTRAINT FK__TIEMPO__folio_ch__5535A963;
IF EXISTS(SELECT 1 FROM sys.objects WHERE name = 'UQ_Vincula_Folio' AND type = 'UQ')
  ALTER TABLE dbo.Vincula_participante DROP CONSTRAINT UQ_Vincula_Folio;
GO

/* 2.2) Quitar columna folio_chip para recrearla */
IF EXISTS(
  SELECT 1 FROM sys.columns 
  WHERE object_id = OBJECT_ID(N'dbo.Vincula_participante') AND name = 'folio_chip'
)
  ALTER TABLE dbo.Vincula_participante DROP COLUMN folio_chip;
GO

/* 2.3) Quitar UQ_NumeroPorCarrera */
IF EXISTS(SELECT 1 FROM sys.objects WHERE name = 'UQ_NumeroPorCarrera' AND type = 'UQ')
  ALTER TABLE dbo.Vincula_participante DROP CONSTRAINT UQ_NumeroPorCarrera;
GO

/* 2.4) Quitar UQ_CARR_Cat_CarreraCategoria */
IF EXISTS(
  SELECT 1 FROM sys.objects 
  WHERE name = 'UQ_CARR_Cat_CarreraCategoria' AND type = 'UQ'
)
  ALTER TABLE dbo.CARR_Cat DROP CONSTRAINT UQ_CARR_Cat_CarreraCategoria;
GO

/* 2.5) Quitar CHK_ID_Categoria_NotNull */
IF EXISTS(
  SELECT 1 FROM sys.check_constraints 
  WHERE name = 'CHK_ID_Categoria_NotNull'
)
  ALTER TABLE dbo.CARR_Cat DROP CONSTRAINT CHK_ID_Categoria_NotNull;
GO

/* 2.6) Quitar columna ID_carrera si existiera */
IF EXISTS(
  SELECT 1 FROM sys.columns 
  WHERE object_id = OBJECT_ID(N'dbo.Vincula_participante') AND name = 'ID_carrera'
)
  ALTER TABLE dbo.Vincula_participante DROP COLUMN ID_carrera;
GO

/*============================================================================
  3) RECREAR COLUMNAS Y CONSTRAINTS NUEVOS
============================================================================*/
/* 3.1) Agregar columna folio_chip como varchar(20) con default */
ALTER TABLE dbo.Vincula_participante
ADD folio_chip VARCHAR(20) NOT NULL DEFAULT('');
GO
-- Asegurar mismo tipo en TIEMPO:
ALTER TABLE dbo.TIEMPO
ALTER COLUMN folio_chip VARCHAR(20) NOT NULL;
GO

/* 3.2) Unique folio_chip */
ALTER TABLE dbo.Vincula_participante
ADD CONSTRAINT UQ_Vincula_Folio UNIQUE(folio_chip);
GO

/* 3.3) FK folio_chip en TIEMPO */
ALTER TABLE dbo.TIEMPO
ADD CONSTRAINT FK__TIEMPO__folio_ch__5535A963
  FOREIGN KEY(folio_chip) REFERENCES dbo.Vincula_participante(folio_chip);
GO

/* 3.4) ID_carrera para referencia */
ALTER TABLE dbo.Vincula_participante
ADD ID_carrera INT NULL;
GO

/* 3.5) Unicidad de número de corredor por carrera */
ALTER TABLE dbo.Vincula_participante
ADD CONSTRAINT UQ_NumeroPorCarrera UNIQUE(ID_carrera, num_corredor);
GO

/* 3.6) Unicidad de categoría por carrera */
ALTER TABLE dbo.CARR_Cat
ADD CONSTRAINT UQ_CARR_Cat_CarreraCategoria UNIQUE(ID_carrera, ID_categoria);
GO

/* 3.7) Asegurar categoría no nula */
ALTER TABLE dbo.CARR_Cat
ADD CONSTRAINT CHK_ID_Categoria_NotNull CHECK(ID_categoria IS NOT NULL);
GO

/* 3.8) FK CARR_Cat → CARRERA con cascada */
ALTER TABLE dbo.CARR_Cat
ADD CONSTRAINT FK_CARR_Cat_Carrera
  FOREIGN KEY(ID_carrera) REFERENCES dbo.CARRERA(ID_carrera) ON DELETE CASCADE;
GO

/*============================================================================
  4) SEQUENCE para folio_chip
============================================================================*/
IF NOT EXISTS (SELECT * FROM sys.sequences WHERE name = 'Seq_FolioChip')
BEGIN
    CREATE SEQUENCE Seq_FolioChip
      START WITH 1
      INCREMENT BY 1;
END;
GO

/*============================================================================
  5) TRIGGER: Asignar num_corredor y folio_chip en HEX usando SEQUENCE
     (ID_vinculo se autogenera con DEFAULT NEWSEQUENTIALID())
============================================================================*/
CREATE OR ALTER TRIGGER dbo.trg_AssignDefaultChip
ON dbo.Vincula_participante
INSTEAD OF INSERT
AS
BEGIN
    SET NOCOUNT ON;

    -- 1) Tabla variable para preparar líneas
    DECLARE @t TABLE (
      ID_vinculo    UNIQUEIDENTIFIER,
      ID_corredor   VARBINARY(32),
      ID_carr_cat   INT,
      ID_carrera    INT,
      num_corredor  INT,
      incoming_chip VARCHAR(20),
      seqval        BIGINT
    );

    -- 2) Poblar @t: capturamos NEXT VALUE FOR Seq_FolioChip directamente
    INSERT INTO @t
      (ID_vinculo, ID_corredor, ID_carr_cat, ID_carrera,
       num_corredor, incoming_chip, seqval)
    SELECT
      NEWID(),                    -- deja que el DEFAULT NEWSEQUENTIALID() sea irrelevante
      i.ID_corredor,
      i.ID_carr_cat,
      cc.ID_carrera,
      COALESCE((
        SELECT MAX(vp.num_corredor) + 1
        FROM dbo.Vincula_participante AS vp
        WHERE vp.ID_carrera = cc.ID_carrera
      ), 1),
      i.folio_chip,               -- puede venir NULL
      NEXT VALUE FOR Seq_FolioChip
    FROM inserted AS i
    JOIN dbo.CARR_Cat AS cc
      ON i.ID_carr_cat = cc.ID_carr_cat;

    -- 3) Insertar en la tabla real, formateando seqval a HEX si incoming_chip es NULL
    INSERT INTO dbo.Vincula_participante
      (ID_vinculo, ID_corredor, ID_carr_cat, ID_carrera,
       num_corredor, folio_chip)
    SELECT
      ID_vinculo,
      ID_corredor,
      ID_carr_cat,
      ID_carrera,
      num_corredor,
      CASE
        WHEN incoming_chip IS NOT NULL THEN incoming_chip
        ELSE RIGHT('000000' + FORMAT(seqval, 'X'), 6)
      END
    FROM @t;
END;
GO

/*============================================================================
  6) TRIGGERS DE VALIDACIÓN
============================================================================*/
/* 6.1) Unicidad de num_corredor por carrera (refuerzo) */
CREATE TRIGGER dbo.trg_EnforceNumeroPorCarrera
ON dbo.Vincula_participante
AFTER INSERT,UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS(
      SELECT ID_carrera, num_corredor
      FROM inserted
      GROUP BY ID_carrera,num_corredor
      HAVING COUNT(*)>1
    ) OR EXISTS(
      SELECT 1
      FROM inserted i
      JOIN dbo.Vincula_participante vp
        ON vp.ID_carrera=i.ID_carrera
       AND vp.num_corredor=i.num_corredor
       AND vp.ID_vinculo<>i.ID_vinculo
    )
    BEGIN
      RAISERROR('Número de corredor duplicado en la misma carrera.',16,1);
      ROLLBACK;
    END
END;
GO

/* 6.2) Evitar doble participación */
CREATE TRIGGER dbo.trg_EnforceSingleParticipation
ON dbo.Vincula_participante
AFTER INSERT,UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS(
      SELECT ID_corredor,ID_carrera
      FROM inserted
      GROUP BY ID_corredor,ID_carrera
      HAVING COUNT(*)>1
    ) OR EXISTS(
      SELECT 1
      FROM inserted i
      JOIN dbo.Vincula_participante vp
        ON vp.ID_corredor=i.ID_corredor
       AND vp.ID_carrera=i.ID_carrera
       AND vp.ID_vinculo<>i.ID_vinculo
    )
    BEGIN
      RAISERROR('Un corredor no puede inscribirse dos veces en la misma carrera.',16,1);
      ROLLBACK;
    END
END;
GO

/* 6.3) Prevenir borrado de vinculo con tiempos */
CREATE OR ALTER TRIGGER dbo.trg_PreventDeleteVinculaConTiempo
ON dbo.Vincula_participante
INSTEAD OF DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS(
      SELECT 1
      FROM deleted d
      JOIN dbo.TIEMPO t ON t.folio_chip=d.folio_chip
    )
    BEGIN
      RAISERROR('No se puede eliminar participante con tiempos registrados.',16,1);
      ROLLBACK;
      RETURN;
    END
    DELETE vp
    FROM dbo.Vincula_participante vp
    JOIN deleted d ON vp.ID_vinculo=d.ID_vinculo;
END;
GO

/* 6.4) Prevenir carrera sin categoría (update) */
CREATE OR ALTER TRIGGER dbo.trg_PreventOrphan_Carrera
ON dbo.CARRERA
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS(
      SELECT i.ID_carrera
      FROM inserted i
      LEFT JOIN dbo.CARR_Cat cc ON cc.ID_carrera=i.ID_carrera
      GROUP BY i.ID_carrera
      HAVING COUNT(cc.ID_carr_cat)=0
    )
    BEGIN
        RAISERROR('Una carrera debe tener al menos una categoría.',16,1);
        ROLLBACK;
    END
END;
GO

/* 6.5) INSTEAD OF DELETE en CARRERA (borrado seguro) */
CREATE OR ALTER TRIGGER dbo.INSTEADOF_DELETE_CARRERA
ON dbo.CARRERA
INSTEAD OF DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS(
      SELECT 1
      FROM deleted d
      JOIN dbo.CARR_Cat cc ON cc.ID_carrera=d.ID_carrera
      JOIN dbo.Vincula_participante vp ON vp.ID_carr_cat=cc.ID_carr_cat
      JOIN dbo.TIEMPO t ON t.folio_chip=vp.folio_chip
    )
    BEGIN
      RAISERROR('No se puede eliminar carrera con tiempos registrados.',16,1);
      ROLLBACK;
      RETURN;
    END
    -- 1) Eliminar participantes
    DELETE vp
    FROM dbo.Vincula_participante vp
    JOIN dbo.CARR_Cat cc ON vp.ID_carr_cat=cc.ID_carr_cat
    JOIN deleted d ON cc.ID_carrera=d.ID_carrera;
    -- 2) Eliminar corredores huérfanos
    DELETE c
    FROM dbo.CORREDOR c
    LEFT JOIN dbo.Vincula_participante vp ON vp.ID_corredor=c.ID_corredor
    WHERE vp.ID_corredor IS NULL;
    -- 3) Eliminar carrera (categorías en cascada)
    DELETE c
    FROM dbo.CARRERA c
    JOIN deleted d ON c.ID_carrera=d.ID_carrera;
END;
GO

PRINT 'Parches.sql aplicado correctamente.';
