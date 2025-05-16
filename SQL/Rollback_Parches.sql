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
  1) ELIMINAR TRIGGERS
============================================================================*/
DROP TRIGGER IF EXISTS dbo.trg_AssignDefaultChip;
DROP TRIGGER IF EXISTS dbo.trg_EnforceNumeroPorCarrera;
DROP TRIGGER IF EXISTS dbo.trg_EnforceSingleParticipation;
DROP TRIGGER IF EXISTS dbo.trg_PreventOrphan_Corredor;
DROP TRIGGER IF EXISTS dbo.trg_PreventOrphan_Carrera;
DROP TRIGGER IF EXISTS dbo.INSTEADOF_DELETE_CARRERA;
GO

/*============================================================================
  2) ELIMINAR CONSTRAINTS
============================================================================*/
IF EXISTS (
  SELECT 1 FROM sys.objects 
  WHERE object_id = OBJECT_ID(N'[dbo].[UQ_CARR_Cat_CarreraCategoria]') 
    AND type = 'UQ'
)
  ALTER TABLE dbo.CARR_Cat DROP CONSTRAINT UQ_CARR_Cat_CarreraCategoria;
GO

IF EXISTS (
  SELECT 1 FROM sys.objects 
  WHERE object_id = OBJECT_ID(N'[dbo].[UQ_Vincula_Folio]') 
    AND type = 'UQ'
)
  ALTER TABLE dbo.Vincula_participante DROP CONSTRAINT UQ_Vincula_Folio;
GO

IF EXISTS (
  SELECT 1 FROM sys.objects 
  WHERE object_id = OBJECT_ID(N'[dbo].[UQ_NumeroPorCarrera]') 
    AND type = 'UQ'
)
  ALTER TABLE dbo.Vincula_participante DROP CONSTRAINT UQ_NumeroPorCarrera;
GO

IF EXISTS (
  SELECT 1 FROM sys.check_constraints 
  WHERE object_id = OBJECT_ID(N'[dbo].[CHK_ID_Categoria_NotNull]')
)
  ALTER TABLE dbo.CARR_Cat DROP CONSTRAINT CHK_ID_Categoria_NotNull;
GO

/*============================================================================
  3) ELIMINAR COLUMNA DENORMALIZADA
============================================================================*/
IF EXISTS (
  SELECT 1 
  FROM sys.columns 
  WHERE object_id = OBJECT_ID(N'[dbo].[Vincula_participante]') 
    AND name = 'ID_carrera'
)
  ALTER TABLE dbo.Vincula_participante DROP COLUMN ID_carrera;
GO

PRINT 'Rollback_Parches.sql completado: BD restaurada al estado previo.';
