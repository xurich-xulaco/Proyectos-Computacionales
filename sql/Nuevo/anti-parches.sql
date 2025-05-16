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
    -- 2) Drops de CONSTRAINTS añadidas
    -----------------------------------------------------------------
    -- 2.1 CARR_Cat: clave única
    IF EXISTS (SELECT 1 
               FROM sys.objects o 
               JOIN sys.schemas s ON o.schema_id = s.schema_id 
               WHERE o.name = 'UQ_CARR_Cat_CarreraCategoria'
                 AND o.type = 'UQ')
    BEGIN
        ALTER TABLE dbo.CARR_Cat
        DROP CONSTRAINT UQ_CARR_Cat_CarreraCategoria;
    END

    -- 2.2 Vincula_participante: unicidad de folio_chip y de corredor–carrera
    IF EXISTS (SELECT 1 
               FROM sys.objects o 
               WHERE o.name = 'UQ_VP_FolioChip' AND o.type = 'UQ')
    BEGIN
        ALTER TABLE dbo.Vincula_participante
        DROP CONSTRAINT UQ_VP_FolioChip;
    END
    IF EXISTS (SELECT 1 
               FROM sys.objects o 
               WHERE o.name = 'UQ_VP_CorredorCarrera' AND o.type = 'UQ')
    BEGIN
        ALTER TABLE dbo.Vincula_participante
        DROP CONSTRAINT UQ_VP_CorredorCarrera;
    END

    -- 2.3 Vincula_participante: FK al corredor
    IF EXISTS (SELECT 1 
               FROM sys.foreign_keys fk 
               WHERE fk.name = 'FK_VP_Corredor')
    BEGIN
        ALTER TABLE dbo.Vincula_participante
        DROP CONSTRAINT FK_VP_Corredor;
    END

    -- 2.4 TIEMPO: check para evitar '00:00:00'
    IF EXISTS (SELECT 1 
               FROM sys.check_constraints cc 
               WHERE cc.name = 'CHK_TIEMPO_NO_ZERO')
    BEGIN
        ALTER TABLE dbo.TIEMPO
        DROP CONSTRAINT CHK_TIEMPO_NO_ZERO;
    END

    -----------------------------------------------------------------
    -- 3) Restaurar columna TIEMPO.tiempo_registrado
    -----------------------------------------------------------------
    -- 3.1 Dejarla NOT NULL
    ALTER TABLE dbo.TIEMPO
    ALTER COLUMN tiempo_registrado TIME NOT NULL;

    -- 3.2 (Opcional) Eliminar default introducido en otros pasos
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

    -----------------------------------------------------------------
    -- 4) Quitar columna is_active de CARRERA
    -----------------------------------------------------------------
    -- 4.1 Eliminar default si existe
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

    -- 4.2 Eliminar la columna
    IF EXISTS (
        SELECT 1
          FROM sys.columns
         WHERE object_id = OBJECT_ID('dbo.CARRERA')
           AND name = 'is_active'
    )
    BEGIN
        ALTER TABLE dbo.CARRERA
        DROP COLUMN is_active;
    END

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
    ROLLBACK TRANSACTION;
    THROW;
END CATCH;
