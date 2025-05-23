USE [Cronometraje_Carreras_Deportivas];
GO

-----------------------------------------------------------------------
-- 0. (Opcional) Agregar columna para borrado lógico de carreras
-----------------------------------------------------------------------
/*
IF NOT EXISTS (
    SELECT 1
      FROM sys.columns
     WHERE object_id = OBJECT_ID(N'dbo.CARRERA')
       AND name = N'is_active'
)
BEGIN
    ALTER TABLE dbo.CARRERA
    ADD is_active BIT NOT NULL
        CONSTRAINT DF_CARRERA_is_active DEFAULT(1);
END
GO
*/

-----------------------------------------------------------------------
-- 1. Unicidad Carrera–Categoría (evita duplicar una misma categoría)
-----------------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.key_constraints k
     WHERE k.name = 'UQ_CARR_Cat_CarreraCategoria'
       AND k.parent_object_id = OBJECT_ID('dbo.CARR_Cat')
)
BEGIN
    ALTER TABLE dbo.CARR_Cat
      ADD CONSTRAINT UQ_CARR_Cat_CarreraCategoria
          UNIQUE (ID_carrera, ID_categoria);
END
GO

-----------------------------------------------------------------------
-- 2. Integridad referencial: impedir borrar corredor con vínculos
-----------------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys fk
     WHERE fk.name = 'FK_VP_Corredor'
       AND fk.parent_object_id = OBJECT_ID('dbo.Vincula_participante')
)
BEGIN
    ALTER TABLE dbo.Vincula_participante
      ADD CONSTRAINT FK_VP_Corredor
        FOREIGN KEY (ID_corredor)
        REFERENCES dbo.CORREDOR (ID_corredor)
        ON DELETE NO ACTION;
END
GO

-----------------------------------------------------------------------
-- 3. Garantizar unicidad de chip y de inscripción por carrera
-----------------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes i
     WHERE i.name = 'UQ_VP_FolioChip'
       AND i.object_id = OBJECT_ID('dbo.Vincula_participante')
)
BEGIN
    ALTER TABLE dbo.Vincula_participante
      ADD CONSTRAINT UQ_VP_FolioChip UNIQUE (folio_chip);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes i
     WHERE i.name = 'UQ_VP_CorredorCarrera'
       AND i.object_id = OBJECT_ID('dbo.Vincula_participante')
)
BEGIN
    ALTER TABLE dbo.Vincula_participante
      ADD CONSTRAINT UQ_VP_CorredorCarrera
          UNIQUE (ID_corredor, ID_carr_cat);
END
GO

-----------------------------------------------------------------------
-- 4. Trigger: inserción en Vincula_participante
--    Genera automáticamente num_corredor y folio_chip
-----------------------------------------------------------------------
CREATE OR ALTER TRIGGER dbo.trg_VP_InsteadOfInsert
ON dbo.Vincula_participante
INSTEAD OF INSERT
AS
BEGIN
    SET NOCOUNT ON;

    -- 5.1: Rechaza duplicados exactos
    IF EXISTS (
        SELECT 1
          FROM inserted i
          JOIN dbo.Vincula_participante vp
            ON vp.ID_corredor = i.ID_corredor
           AND vp.ID_carr_cat = i.ID_carr_cat
    )
    BEGIN
        RAISERROR('El corredor ya está asociado a esta categoría de la carrera.',16,1);
        RETURN;
    END

    -- 5.2: Rechaza si ya tiene otra categoría en la misma carrera
    IF EXISTS (
        SELECT 1
          FROM inserted i
          JOIN dbo.CARR_Cat newcc ON newcc.ID_carr_cat = i.ID_carr_cat
          JOIN dbo.Vincula_participante vpold
            ON vpold.ID_corredor = i.ID_corredor
          JOIN dbo.CARR_Cat oldcc
            ON oldcc.ID_carr_cat = vpold.ID_carr_cat
         WHERE newcc.ID_carrera = oldcc.ID_carrera
    )
    BEGIN
        RAISERROR('El corredor ya está inscrito en otra categoría de esta carrera.',16,1);
        RETURN;
    END

    -- 5.3: Calcular secuencia para cada fila y hacer el INSERT
    ;WITH ToInsert AS (
        SELECT
          i.ID_corredor,
          i.ID_carr_cat,
          cc.ID_carrera,
          ROW_NUMBER() OVER (
            PARTITION BY cc.ID_carrera 
            ORDER BY i.ID_corredor
          ) AS seq
        FROM inserted i
        JOIN dbo.CARR_Cat cc
          ON cc.ID_carr_cat = i.ID_carr_cat
    ), BaseNum AS (
        SELECT
          cc.ID_carrera,
          ISNULL(MAX(vp.num_corredor),0) AS max_num
        FROM dbo.Vincula_participante vp
        JOIN dbo.CARR_Cat cc
          ON vp.ID_carr_cat = cc.ID_carr_cat
        WHERE cc.ID_carrera IN (
            SELECT DISTINCT ID_carrera FROM ToInsert
        )
        GROUP BY cc.ID_carrera
    )
    INSERT INTO dbo.Vincula_participante
      (ID_vinculo, ID_corredor, ID_carr_cat, num_corredor, folio_chip)
    SELECT
      NEWID(),
      ti.ID_corredor,
      ti.ID_carr_cat,
      bn.max_num + ti.seq,
      'RFID' + CAST(1000000000 + bn.max_num + ti.seq AS VARCHAR(20))
    FROM ToInsert ti
    JOIN BaseNum bn
      ON bn.ID_carrera = ti.ID_carrera;
END;
GO

-----------------------------------------------------------------------
-- 5. Trigger: eliminación en Vincula_participante
--    • Bloquea si hay tiempos para ese folio_chip
--    • Si quedan otras asociaciones, sólo borra el vínculo
--    • Si era la única asociación, borra vínculo + corredor
-----------------------------------------------------------------------
CREATE OR ALTER TRIGGER dbo.trg_VP_InsteadOfDelete
ON dbo.Vincula_participante
INSTEAD OF DELETE
AS
BEGIN
    SET NOCOUNT ON;

    -- 5.a) Prohibir eliminación si existen tiempos registrados
    IF EXISTS (
        SELECT 1
          FROM deleted d
          JOIN dbo.TIEMPO t
            ON d.folio_chip = t.folio_chip
    )
    BEGIN
        RAISERROR(
          'No se puede eliminar: existen tiempos registrados para este corredor en la carrera.',
          16, 1
        );
        ROLLBACK TRANSACTION;
        RETURN;
    END

    -- 5.b) Calcular cuántos vínculos quedarían por corredor
    ;WITH DelVinc AS (
        SELECT ID_vinculo, ID_corredor
        FROM deleted
    ), RemainCounts AS (
        SELECT d.ID_corredor,
               COUNT(vp.ID_vinculo) AS RemainingCount
        FROM DelVinc d
        LEFT JOIN dbo.Vincula_participante vp
          ON vp.ID_corredor = d.ID_corredor
         AND vp.ID_vinculo NOT IN (SELECT ID_vinculo FROM DelVinc)
        GROUP BY d.ID_corredor
    )
    -- 5.c) Eliminar los vínculos solicitados
    DELETE vp
      FROM dbo.Vincula_participante vp
      JOIN DelVinc d
        ON vp.ID_vinculo = d.ID_vinculo;

    -- 5.d) Para corredores sin más asociaciones, eliminar registro en CORREDOR
    DELETE c
      FROM dbo.CORREDOR c
      JOIN RemainCounts rc
        ON c.ID_corredor = rc.ID_corredor
     WHERE rc.RemainingCount = 0;
END;
GO

-----------------------------------------------------------------------
-- 6. Trigger: eliminación en CARRERA
--    • Bloquea si hay tiempos registrados
--    • Caso contrario, elimina físicamente carrera, categorías y vínculos
-----------------------------------------------------------------------
CREATE OR ALTER TRIGGER dbo.trg_Carrera_InsteadOfDelete
ON dbo.CARRERA
INSTEAD OF DELETE
AS
BEGIN
    SET NOCOUNT ON;

    -- 6.a) Abort if any time exists
    IF EXISTS (
        SELECT 1
          FROM deleted d
          JOIN dbo.CARR_Cat cc
            ON d.ID_carrera = cc.ID_carrera
          JOIN dbo.Vincula_participante vp
            ON cc.ID_carr_cat = vp.ID_carr_cat
          JOIN dbo.TIEMPO t
            ON vp.folio_chip = t.folio_chip
    )
    BEGIN
        RAISERROR(
          'No se puede eliminar la(s) carrera(s): existen registros de tiempo.',
          16, 1
        );
        ROLLBACK TRANSACTION;
        RETURN;
    END

    -- 6.b) Delete vínculos, categorías y finalmente carrera
    DELETE vp
      FROM dbo.Vincula_participante vp
      JOIN dbo.CARR_Cat cc
        ON vp.ID_carr_cat = cc.ID_carr_cat
      JOIN deleted d
        ON cc.ID_carrera = d.ID_carrera;

    DELETE cc
      FROM dbo.CARR_Cat cc
      JOIN deleted d
        ON cc.ID_carrera = d.ID_carrera;

    DELETE c
      FROM dbo.CARRERA c
      JOIN deleted d
        ON c.ID_carrera = d.ID_carrera;
END;
GO

-----------------------------------------------------------------------
-- 7. Trigger: asegurar al menos una categoría por carrera
-----------------------------------------------------------------------
CREATE OR ALTER TRIGGER dbo.trg_CarrCat_AfterDelete
ON dbo.CARR_Cat
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
          FROM deleted d
          LEFT JOIN dbo.CARR_Cat cc
            ON d.ID_carrera = cc.ID_carrera
         WHERE cc.ID_carrera IS NULL
    )
    BEGIN
        RAISERROR(
          'Cada carrera debe tener al menos una categoría.',
          16, 1
        );
        ROLLBACK TRANSACTION;
    END
END;
GO

-----------------------------------------------------------------------
-- 8. Validación de TIEMPO: permitir NULL y prohibir '00:00:00'
-----------------------------------------------------------------------
ALTER TABLE dbo.TIEMPO
ALTER COLUMN tiempo_registrado TIME NULL;
GO

-- Eliminar default existente, si hay
DECLARE @df NVARCHAR(128);
SELECT @df = dc.name
  FROM sys.default_constraints dc
  JOIN sys.columns c
    ON dc.parent_object_id = c.object_id
   AND dc.parent_column_id = c.column_id
 WHERE dc.parent_object_id = OBJECT_ID('dbo.TIEMPO')
   AND c.name = 'tiempo_registrado';

IF @df IS NOT NULL
    EXEC('ALTER TABLE dbo.TIEMPO DROP CONSTRAINT ' + @df);
GO

ALTER TABLE dbo.TIEMPO
ADD CONSTRAINT CHK_TIEMPO_NO_ZERO
    CHECK (
        tiempo_registrado IS NULL
        OR DATEDIFF(SECOND, '00:00:00', tiempo_registrado) > 0
    );
GO
