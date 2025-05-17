----------------------------------------------------------
-- Rollback_Parches.sql
--   Revierte cambios de Parches.sql (trigger AFTER)
----------------------------------------------------------

-- 0) Parámetros y validación
DECLARE @DBName SYSNAME = N'Cronometraje_Carreras';
IF DB_ID(@DBName) IS NULL
BEGIN
    RAISERROR('La base de datos %s no existe; nada que revertir.',16,1,@DBName);
    RETURN;
END

-- 1) Contexto
EXEC(N'USE [' + @DBName + '];');
GO

-- 2) Eliminar triggers y constraints
BEGIN TRY
    BEGIN TRANSACTION;

    -- 2.1) Triggers
    DROP TRIGGER IF EXISTS dbo.trg_AssignDefaultChip;
    DROP TRIGGER IF EXISTS dbo.trg_EnforceNumeroPorCarrera;
    DROP TRIGGER IF EXISTS dbo.trg_EnforceSingleParticipation;
    DROP TRIGGER IF EXISTS dbo.trg_PreventOrphan_Corredor;
    DROP TRIGGER IF EXISTS dbo.trg_PreventOrphan_Carrera;
    DROP TRIGGER IF EXISTS dbo.INSTEADOF_DELETE_CARRERA;

    -- 2.2) Constraints
    IF OBJECT_ID(N'dbo.UQ_CARR_Cat_CarreraCategoria','UQ') IS NOT NULL
        ALTER TABLE dbo.CARR_Cat DROP CONSTRAINT UQ_CARR_Cat_CarreraCategoria;
    IF OBJECT_ID(N'dbo.UQ_Vincula_Folio','UQ') IS NOT NULL
        ALTER TABLE dbo.Vincula_participante DROP CONSTRAINT UQ_Vincula_Folio;
    IF OBJECT_ID(N'dbo.UQ_Vincula_NumeroPorCategoria','UQ') IS NOT NULL
        ALTER TABLE dbo.Vincula_participante DROP CONSTRAINT UQ_Vincula_NumeroPorCategoria;
    IF OBJECT_ID(N'dbo.CHK_ID_Categoria_NotNull','C') IS NOT NULL
        ALTER TABLE dbo.CARR_Cat DROP CONSTRAINT CHK_ID_Categoria_NotNull;

    COMMIT TRANSACTION;
    PRINT 'Rollback_Parches.sql completado: BD restaurada al estado previo.';
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
