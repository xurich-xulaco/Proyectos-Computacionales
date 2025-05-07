USE [Cronometraje_Carreras_Deportivas];
GO

-------------------------------------------------------------
-- ROLLBACK DE PATCHES: deja el esquema tal como al inicio
-------------------------------------------------------------
BEGIN TRY
    BEGIN TRANSACTION;

    -----------------------------------------------------------------
    -- 1) Drops de TRIGGERS
    -----------------------------------------------------------------
    IF OBJECT_ID('dbo.trg_VP_InsteadOfInsert','TR') IS NOT NULL
        DROP TRIGGER dbo.trg_VP_InsteadOfInsert;
    IF OBJECT_ID('dbo.trg_VP_InsteadOfDelete','TR') IS NOT NULL
        DROP TRIGGER dbo.trg_VP_InsteadOfDelete;
    IF OBJECT_ID('dbo.trg_Carrera_InsteadOfDelete','TR') IS NOT NULL
        DROP TRIGGER dbo.trg_Carrera_InsteadOfDelete;
    IF OBJECT_ID('dbo.trg_CarrCat_AfterDelete','TR') IS NOT NULL
        DROP TRIGGER dbo.trg_CarrCat_AfterDelete;
    
    -----------------------------------------------------------------
    -- 2) DROP CONSTRAINTS añadidas en Vincula_participante y CARR_Cat
    -----------------------------------------------------------------
    IF EXISTS (
        SELECT 1
          FROM sys.key_constraints kc
         WHERE kc.name = 'UQ_CARR_Cat_CarreraCategoria'
           AND kc.parent_object_id = OBJECT_ID('dbo.CARR_Cat')
    )
        ALTER TABLE dbo.CARR_Cat
        DROP CONSTRAINT UQ_CARR_Cat_CarreraCategoria;

    IF EXISTS (
        SELECT 1
          FROM sys.foreign_keys fk
         WHERE fk.name = 'FK_VP_Corredor'
           AND fk.parent_object_id = OBJECT_ID('dbo.Vincula_participante')
    )
        ALTER TABLE dbo.Vincula_participante
        DROP CONSTRAINT FK_VP_Corredor;

    IF EXISTS (
        SELECT 1
          FROM sys.key_constraints kc
         WHERE kc.name = 'UQ_VP_FolioChip'
           AND kc.parent_object_id = OBJECT_ID('dbo.Vincula_participante')
    )
        ALTER TABLE dbo.Vincula_participante
        DROP CONSTRAINT UQ_VP_FolioChip;

    IF EXISTS (
        SELECT 1
          FROM sys.key_constraints kc
         WHERE kc.name = 'UQ_VP_CorredorCarrera'
           AND kc.parent_object_id = OBJECT_ID('dbo.Vincula_participante')
    )
        ALTER TABLE dbo.Vincula_participante
        DROP CONSTRAINT UQ_VP_CorredorCarrera;

    IF EXISTS (
        SELECT 1
          FROM sys.check_constraints cc
         WHERE cc.name = 'CHK_TIEMPO_NO_ZERO'
           AND cc.parent_object_id = OBJECT_ID('dbo.TIEMPO')
    )
        ALTER TABLE dbo.TIEMPO
        DROP CONSTRAINT CHK_TIEMPO_NO_ZERO;

    -----------------------------------------------------------------
    -- 3) Restaurar TIEMPO.tiempo_registrado al estado NOT NULL
    -----------------------------------------------------------------
    DECLARE @nUpdated INT;
    UPDATE dbo.TIEMPO
        SET tiempo_registrado = '00:00:00'
    WHERE tiempo_registrado IS NULL;
    SET @nUpdated = @@ROWCOUNT;
    PRINT CONCAT(@nUpdated, ' filas actualizadas en TIEMPO.tiempo_registrado');

    DECLARE @df_tiempo NVARCHAR(128);
    SELECT @df_tiempo = dc.name
      FROM sys.default_constraints dc
      JOIN sys.columns c
        ON dc.parent_object_id = c.object_id
       AND dc.parent_column_id = c.column_id
     WHERE dc.parent_object_id = OBJECT_ID('dbo.TIEMPO')
       AND c.name = 'tiempo_registrado';

    IF @df_tiempo IS NOT NULL
        EXEC('ALTER TABLE dbo.TIEMPO DROP CONSTRAINT ' + @df_tiempo);
    
    ALTER TABLE dbo.TIEMPO
      ALTER COLUMN tiempo_registrado TIME NOT NULL;

    -----------------------------------------------------------------
    -- 4) Quitar la columna opcional is_active de CARRERA
    -----------------------------------------------------------------
    DECLARE @df_carrera NVARCHAR(128);
    SELECT @df_carrera = dc.name
      FROM sys.default_constraints dc
      JOIN sys.columns c
        ON dc.parent_object_id = c.object_id
       AND dc.parent_column_id = c.column_id
     WHERE dc.parent_object_id = OBJECT_ID('dbo.CARRERA')
       AND c.name = 'is_active';

    IF @df_carrera IS NOT NULL
        EXEC('ALTER TABLE dbo.CARRERA DROP CONSTRAINT ' + @df_carrera);

    IF EXISTS (
        SELECT 1
          FROM sys.columns
         WHERE object_id = OBJECT_ID('dbo.CARRERA')
           AND name = 'is_active'
    )
        ALTER TABLE dbo.CARRERA
        DROP COLUMN is_active;

    -----------------------------------------------------------------
    -- 5) (Opcional) Restaurar cualquier default original 
    --     en otras columnas si tuvieras la definición
    -----------------------------------------------------------------
    -- Aquí podrías volver a crear defaults originales de tu init script,
    -- por ejemplo:
    -- ALTER TABLE dbo.TIEMPO
    -- ADD CONSTRAINT DF_TIEMPO_tiempo DEFAULT('00:00:00') FOR tiempo_registrado;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
