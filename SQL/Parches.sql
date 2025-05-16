-- ================================================
-- CONFIGURACIÓN INICIAL
-- ================================================
:setvar DBName Cronometraje

IF DB_ID('$(DBName)') IS NULL
BEGIN
    RAISERROR('La base de datos $(DBName) no existe. Ejecútalo tras crear el esquema principal.', 16, 1);
    RETURN;
END
GO

USE [$(DBName)];
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
GO

/*============================================================================
  2) ELIMINAR CONSTRAINTS Y COLUMNA EXISTENTES
============================================================================*/
/* UQ en CARR_Cat */
IF EXISTS (
  SELECT 1 FROM sys.objects 
  WHERE object_id = OBJECT_ID(N'[dbo].[UQ_CARR_Cat_CarreraCategoria]') 
    AND type = 'UQ'
)
  ALTER TABLE dbo.CARR_Cat DROP CONSTRAINT UQ_CARR_Cat_CarreraCategoria;
GO

/* UQ de chips */
IF EXISTS (
  SELECT 1 FROM sys.objects 
  WHERE object_id = OBJECT_ID(N'[dbo].[UQ_Vincula_Folio]') 
    AND type = 'UQ'
)
  ALTER TABLE dbo.Vincula_participante DROP CONSTRAINT UQ_Vincula_Folio;
GO

/* UQ anterior de num_corredor */
IF EXISTS (
  SELECT 1 FROM sys.objects 
  WHERE object_id = OBJECT_ID(N'[dbo].[UQ_NumeroPorCarrera]') 
    AND type = 'UQ'
)
  ALTER TABLE dbo.Vincula_participante DROP CONSTRAINT UQ_NumeroPorCarrera;
GO

/* CHECK de categoría no null */
IF EXISTS (
  SELECT 1 FROM sys.check_constraints 
  WHERE object_id = OBJECT_ID(N'[dbo].[CHK_ID_Categoria_NotNull]')
)
  ALTER TABLE dbo.CARR_Cat DROP CONSTRAINT CHK_ID_Categoria_NotNull;
GO

/* Columna denormalizada de carrera en Vincula_participante */
IF EXISTS (
  SELECT 1 
  FROM sys.columns 
  WHERE object_id = OBJECT_ID(N'[dbo].[Vincula_participante]') 
    AND name = 'ID_carrera'
)
  ALTER TABLE dbo.Vincula_participante DROP COLUMN ID_carrera;
GO

/*============================================================================
  3) CREAR CONSTRAINTS Y COLUMNA NUEVOS
============================================================================*/
/* 3.1) Unicidad de categoría por carrera */
ALTER TABLE dbo.CARR_Cat
ADD CONSTRAINT UQ_CARR_Cat_CarreraCategoria
  UNIQUE(ID_carrera, ID_categoria);
GO

/* 3.2) Chips únicos */
ALTER TABLE dbo.Vincula_participante
ADD CONSTRAINT UQ_Vincula_Folio
  UNIQUE(folio_chip);
GO

/* 3.3) Añadir columna ID_carrera para referencia */
ALTER TABLE dbo.Vincula_participante
ADD ID_carrera INT NULL;
GO

/* 3.4) Unicidad de número de corredor POR carrera */
ALTER TABLE dbo.Vincula_participante
ADD CONSTRAINT UQ_NumeroPorCarrera
  UNIQUE(ID_carrera, num_corredor);
GO

/* 3.5) Asegurar que categoría no sea NULL */
ALTER TABLE dbo.CARR_Cat
ADD CONSTRAINT CHK_ID_Categoria_NotNull
  CHECK (ID_categoria IS NOT NULL);
GO

/*============================================================================
  4) TRIGGER: Asignar folio_chip, num_corredor e ID_carrera
============================================================================*/
CREATE TRIGGER dbo.trg_AssignDefaultChip
ON dbo.Vincula_participante
INSTEAD OF INSERT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Vincula_participante
      (ID_vinculo, ID_corredor, ID_carr_cat, ID_carrera, num_corredor, folio_chip)
    SELECT
      NEWID(),
      i.ID_corredor,
      i.ID_carr_cat,
      cc.ID_carrera,
      -- siguiente número dentro DE LA CARRERA completa
      COALESCE((
        SELECT MAX(vp.num_corredor) + 1
        FROM dbo.Vincula_participante vp
        WHERE vp.ID_carrera = cc.ID_carrera
      ), 1),
      CASE
        WHEN ISNULL(i.folio_chip, '') = ''
        THEN 'CHIP-' + CONVERT(VARCHAR(36), NEWID())
        ELSE i.folio_chip
      END
    FROM inserted i
    JOIN dbo.CARR_Cat cc
      ON i.ID_carr_cat = cc.ID_carr_cat;
END;
GO

/*============================================================================
  5) TRIGGER: reforzar unicidad de num_corredor por carrera
============================================================================*/
CREATE TRIGGER dbo.trg_EnforceNumeroPorCarrera
ON dbo.Vincula_participante
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- 5.a) duplicados dentro del mismo lote
    IF EXISTS (
      SELECT ID_carrera, num_corredor, COUNT(*) cnt
      FROM inserted
      GROUP BY ID_carrera, num_corredor
      HAVING COUNT(*) > 1
    )
    BEGIN
      RAISERROR(
        'El número de corredor está repetido en el mismo lote.', 16,1
      );
      ROLLBACK TRANSACTION;
      RETURN;
    END

    -- 5.b) duplicados contra existentes
    IF EXISTS (
      SELECT 1
      FROM inserted i
      JOIN dbo.Vincula_participante vp
        ON vp.ID_carrera   = i.ID_carrera
       AND vp.num_corredor = i.num_corredor
       AND vp.ID_vinculo  <> i.ID_vinculo
    )
    BEGIN
      RAISERROR(
        'El número de corredor ya existe en esta carrera.', 16,1
      );
      ROLLBACK TRANSACTION;
    END
END;
GO

/*============================================================================
  6) TRIGGER: impedir doble participación de un mismo corredor en la misma carrera
============================================================================*/
CREATE TRIGGER dbo.trg_EnforceSingleParticipation
ON dbo.Vincula_participante
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- 6.a) en el lote
    IF EXISTS (
      SELECT ID_corredor, ID_carrera, COUNT(*) cnt
      FROM inserted
      GROUP BY ID_corredor, ID_carrera
      HAVING COUNT(*) > 1
    )
    BEGIN
      RAISERROR(
        'Un corredor no puede inscribirse dos veces en la misma carrera.', 
        16,1
      );
      ROLLBACK TRANSACTION;
      RETURN;
    END

    -- 6.b) contra existentes
    IF EXISTS (
      SELECT 1
      FROM inserted i
      JOIN dbo.Vincula_participante vp
        ON vp.ID_corredor = i.ID_corredor
       AND vp.ID_carrera = i.ID_carrera
       AND vp.ID_vinculo <> i.ID_vinculo
    )
    BEGIN
      RAISERROR(
        'Un corredor ya está inscrito en esta carrera.', 16,1
      );
      ROLLBACK TRANSACTION;
    END
END;
GO

/*============================================================================
  7) TRIGGER: impedir corredor huérfano
============================================================================*/
CREATE TRIGGER dbo.trg_PreventOrphan_Corredor
ON dbo.Vincula_participante
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
      SELECT d.ID_corredor
      FROM deleted d
      LEFT JOIN dbo.Vincula_participante vp
        ON vp.ID_corredor = d.ID_corredor
      WHERE vp.ID_corredor IS NULL
    )
    BEGIN
      RAISERROR(
        'No se permite eliminar el último vínculo de un corredor.', 
        16,1
      );
      ROLLBACK TRANSACTION;
    END
END;
GO

/*============================================================================
  8) TRIGGER: impedir carrera sin categoría
============================================================================*/
CREATE TRIGGER dbo.trg_PreventOrphan_Carrera
ON dbo.CARR_Cat
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
      SELECT d.ID_carrera
      FROM deleted d
      LEFT JOIN dbo.CARR_Cat cc
        ON cc.ID_carrera = d.ID_carrera
      WHERE cc.ID_carrera IS NULL
    )
    BEGIN
      RAISERROR(
        'No se permite eliminar la última categoría de una carrera.', 
        16,1
      );
      ROLLBACK TRANSACTION;
    END
END;
GO

/*============================================================================
  9) TRIGGER: borrado físico de CARRERA solo si NO hay tiempos
============================================================================*/
CREATE TRIGGER dbo.INSTEADOF_DELETE_CARRERA
ON dbo.CARRERA
INSTEAD OF DELETE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
      SELECT 1
      FROM deleted d
      JOIN dbo.CARR_Cat cc 
        ON cc.ID_carrera = d.ID_carrera
      JOIN dbo.Vincula_participante vp 
        ON vp.ID_carr_cat = cc.ID_carr_cat
      JOIN dbo.TIEMPO t 
        ON t.folio_chip = vp.folio_chip
    )
    BEGIN
      RAISERROR(
        'No se puede eliminar la(s) carrera(s): existen tiempos registrados.', 
        16,1
      );
      ROLLBACK TRANSACTION;
      RETURN;
    END

    DELETE vp
    FROM dbo.Vincula_participante vp
    JOIN dbo.CARR_Cat cc 
      ON vp.ID_carr_cat = cc.ID_carr_cat
    JOIN deleted d 
      ON cc.ID_carrera = d.ID_carrera;

    DELETE cc2
    FROM dbo.CARR_Cat cc2
    JOIN deleted d 
      ON cc2.ID_carrera = d.ID_carrera;

    DELETE c
    FROM dbo.CARRERA c
    JOIN deleted d 
      ON c.ID_carrera = d.ID_carrera;
END;
GO

PRINT 'Parches.sql aplicado correctamente.';
