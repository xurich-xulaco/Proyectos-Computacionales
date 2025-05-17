----------------------------------------------------------
-- Script “Consultas Varias” – Versión revisada
-- Aplicable sobre la base de datos Cronometraje_Carreras
----------------------------------------------------------

--------------------------------------------------------------------------------
-- 0) Parámetros y cambio de contexto
--------------------------------------------------------------------------------
DECLARE @DBName SYSNAME = N'Cronometraje_Carreras';

IF DB_ID(@DBName) IS NULL
BEGIN
    RAISERROR('La base de datos %s no existe.', 16,1, @DBName);
    RETURN;
END

EXEC(N'USE [' + @DBName + '];');
GO

--------------------------------------------------------------------------------
-- 1) Reglas de validación en ADMINISTRADOR
--    (Sin validar retroactivamente los datos existentes)
--------------------------------------------------------------------------------
-- 1.1) Asegurar contraseña fuerte
IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints 
     WHERE name = 'chk_pass_admin'
)
BEGIN
    ALTER TABLE dbo.ADMINISTRADOR
    WITH NOCHECK
    ADD CONSTRAINT chk_pass_admin
        CHECK (
            LEN(pass_admin) >= 8
            AND pass_admin LIKE '%[A-Z]%'
            AND pass_admin LIKE '%[a-z]%'
            AND pass_admin LIKE '%[0-9]%'
            AND pass_admin LIKE '%[@$!%*?&]%'
        );
    ALTER TABLE dbo.ADMINISTRADOR
    CHECK CONSTRAINT chk_pass_admin;  -- Solo para futuras filas
END

-- 1.2) Asegurar nombre de usuario de 10 caracteres mixtos
IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints 
     WHERE name = 'chk_uss_valid'
)
BEGIN
    ALTER TABLE dbo.ADMINISTRADOR
    WITH NOCHECK
    ADD CONSTRAINT chk_uss_valid
        CHECK (
            LEN(uss_admin) = 10
            AND uss_admin LIKE '%[A-Z]%'
            AND uss_admin LIKE '%[a-z]%'
            AND uss_admin LIKE '%[0-9]%'
        );
    ALTER TABLE dbo.ADMINISTRADOR
    CHECK CONSTRAINT chk_uss_valid;  -- Solo para nuevas modificaciones
END

-- 1.3) Trigger que valida pass_admin al INSERT o UPDATE
IF OBJECT_ID(N'dbo.trg_validar_contrasena','TR') IS NOT NULL
    DROP TRIGGER dbo.trg_validar_contrasena;
GO

CREATE TRIGGER dbo.trg_validar_contrasena
ON dbo.ADMINISTRADOR
INSTEAD OF INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @bad INT;
    SELECT @bad = COUNT(*)
    FROM inserted i
    WHERE
        LEN(i.pass_admin) < 8
        OR i.pass_admin    NOT LIKE '%[A-Z]%'
        OR i.pass_admin    NOT LIKE '%[a-z]%'
        OR i.pass_admin    NOT LIKE '%[0-9]%'
        OR i.pass_admin    NOT LIKE '%[@$!%*?&]%';

    IF @bad > 0
    BEGIN
        RAISERROR(
            'La contraseña debe tener ≥8 caracteres, con mayúscula, minúscula, dígito y símbolo.', 
            16,1
        );
        ROLLBACK TRANSACTION;
        RETURN;
    END

    -- Si pasa, delegamos a la operación original:
    IF EXISTS(SELECT * FROM inserted WHERE ID_admin IS NULL)
        INSERT INTO dbo.ADMINISTRADOR (uss_admin, pass_admin)
        SELECT uss_admin, pass_admin FROM inserted WHERE ID_admin IS NULL;

    IF EXISTS(SELECT * FROM inserted WHERE ID_admin IS NOT NULL)
        UPDATE a
        SET 
          a.uss_admin  = i.uss_admin,
          a.pass_admin = i.pass_admin
        FROM dbo.ADMINISTRADOR AS a
        JOIN inserted AS i
          ON a.ID_admin = i.ID_admin;
END;
GO

--------------------------------------------------------------------------------
-- 2) Alteraciones de esquema menores
--------------------------------------------------------------------------------
-- 2.1) Agregar columna pais a CORREDOR
IF COL_LENGTH('dbo.CORREDOR','pais') IS NULL
    ALTER TABLE dbo.CORREDOR
    ADD pais VARCHAR(30) NULL;
GO

-- 2.2) Unicidad compuesta en CARRERA
IF EXISTS(
    SELECT 1 
      FROM sys.objects 
     WHERE name = 'UC_Carrera' AND type = 'UQ'
)
    ALTER TABLE dbo.CARRERA DROP CONSTRAINT UC_Carrera;
ALTER TABLE dbo.CARRERA
ADD CONSTRAINT UC_Carrera UNIQUE(nom_carrera, year_carrera, edi_carrera);
GO

-- 2.3) Ajustar tabla TELEFONO
-- (redefine ID_corredor si fue mal tipado)
IF OBJECT_ID(N'dbo.TELEFONO','U') IS NOT NULL
BEGIN
    ALTER TABLE dbo.TELEFONO
    ALTER COLUMN ID_corredor BINARY(32) NOT NULL;
END
GO

--------------------------------------------------------------------------------
-- 3) Tablas nuevas / recreaciones puntuales
--------------------------------------------------------------------------------
-- 3.1) Asegurar existencia de TELEFONO
IF OBJECT_ID(N'dbo.TELEFONO','U') IS NULL
BEGIN
    CREATE TABLE dbo.TELEFONO (
        ID_tele INT IDENTITY(1,1) PRIMARY KEY,
        numero  VARCHAR(15)       NOT NULL,
        ID_corredor BINARY(32)    NOT NULL
            CONSTRAINT FK_Tele_Cor FOREIGN KEY(ID_corredor)
            REFERENCES dbo.CORREDOR(ID_corredor)
    );
END
GO

-- 3.2) Asegurar existencia de VINCULA_PARTICIPANTE
IF OBJECT_ID(N'dbo.Vincula_participante','U') IS NULL
BEGIN
    CREATE TABLE dbo.Vincula_participante (
        ID_vinculo   UNIQUEIDENTIFIER DEFAULT NEWID() PRIMARY KEY,
        ID_corredor  BINARY(32)       NOT NULL
            CONSTRAINT FK_Vinc_Cor FOREIGN KEY(ID_corredor)
            REFERENCES dbo.CORREDOR(ID_corredor),
        ID_carr_cat  INT              NOT NULL
            CONSTRAINT FK_Vinc_Cat FOREIGN KEY(ID_carr_cat)
            REFERENCES dbo.CARR_Cat(ID_carr_cat),
        num_corredor INT              NOT NULL,
        folio_chip   VARCHAR(20)      NOT NULL,
        CONSTRAINT UQ_Vinc_Folio UNIQUE(folio_chip),
        CONSTRAINT UQ_Vinc_Num UNIQUE(ID_carr_cat, num_corredor)
    );
END
GO

--------------------------------------------------------------------------------
-- 4) DML de ejemplo y limpieza selectiva
--------------------------------------------------------------------------------
-- 4.1) Insert de admin de prueba
IF NOT EXISTS(SELECT 1 FROM dbo.ADMINISTRADOR WHERE uss_admin='Eladmin57')
    INSERT INTO dbo.ADMINISTRADOR(uss_admin, pass_admin)
    VALUES('Eladmin57','Xj45!87Aa');
GO

-- 4.2) Complementar CATEGORIA 1–50 km
WITH Numbers AS (
    SELECT 1 AS n
    UNION ALL
    SELECT n + 1 FROM Numbers WHERE n < 50
)
INSERT INTO dbo.CATEGORIA(nombre_categoria)
SELECT CONCAT(n, ' km')
FROM Numbers AS nums
WHERE NOT EXISTS (
    SELECT 1 
    FROM dbo.CATEGORIA AS c
    WHERE RTRIM(LTRIM(c.nombre_categoria)) COLLATE Latin1_General_CS_AS = CONCAT(n, ' km')
)
OPTION (MAXRECURSION 50);
GO

--------------------------------------------------------------------------------
-- 5) Consultas de prueba y ejemplos de uso
--------------------------------------------------------------------------------
-- 5.1) Buscar carreras con exactamente 3 categorías
SELECT
    ca.ID_carrera,
    CONCAT(
      ca.nom_carrera,' - ',ca.year_carrera,
      ' (Ed:',ca.edi_carrera,') ',
      '(', STRING_AGG(cat.nombre_categoria, ', '), ')'
    ) AS Descripción
FROM dbo.CARRERA AS ca
JOIN dbo.CARR_Cat AS cc ON ca.ID_carrera = cc.ID_carrera
JOIN dbo.CATEGORIA AS cat ON cc.ID_categoria = cat.ID_categoria
GROUP BY ca.ID_carrera, ca.nom_carrera, ca.year_carrera, ca.edi_carrera
HAVING COUNT(*) = 3
ORDER BY ca.year_carrera DESC, ca.edi_carrera DESC;
GO

-- 5.2) Ver todos los corredores y teléfonos
SELECT 
    c.nom_corredor + ' ' + c.apP_corredor AS Corredor,
    t.numero
FROM dbo.CORREDOR AS c
LEFT JOIN dbo.TELEFONO  AS t
  ON c.ID_corredor = t.ID_corredor;
GO

-- 5.3) Clasificación en 5 km para la edición 1 de 2024
WITH Tiempos AS (
    SELECT 
        folio_chip,
        tiempo_registrado,
        ROW_NUMBER() OVER (
          PARTITION BY folio_chip ORDER BY tiempo_registrado
        ) AS NumTiempo
    FROM dbo.TIEMPO
), Carrera5km AS (
    SELECT v.folio_chip, v.num_corredor, c.nom_corredor
    FROM dbo.Vincula_participante AS v
    JOIN dbo.CARR_Cat AS cc ON v.ID_carr_cat = cc.ID_carr_cat
    JOIN dbo.CATEGORIA   AS cat ON cc.ID_categoria = cat.ID_categoria
    JOIN dbo.CARRERA     AS ca  ON cc.ID_carrera = ca.ID_carrera
    JOIN dbo.CORREDOR    AS c   ON v.ID_corredor = c.ID_corredor
    WHERE ca.year_carrera = 2024 
      AND ca.edi_carrera  = 1 
      AND cat.nombre_categoria = '5 km'
)
SELECT
    RANK() OVER (ORDER BY 
        DATEDIFF(SECOND,0,COALESCE(t1.tiempo_registrado,'00:00:00')) +
        DATEDIFF(SECOND,0,COALESCE(t2.tiempo_registrado,'00:00:00')) +
        DATEDIFF(SECOND,0,COALESCE(t3.tiempo_registrado,'00:00:00'))
    ) AS Posición,
    c.num_corredor       AS NumCorredor,
    c.nom_corredor       AS Nombre,
    COALESCE(t1.tiempo_registrado,'00:00:00') AS T1,
    COALESCE(t2.tiempo_registrado,'00:00:00') AS T2,
    COALESCE(t3.tiempo_registrado,'00:00:00') AS T3,
    CONVERT(
      TIME,
      DATEADD(SECOND,
        DATEDIFF(SECOND,0,COALESCE(t1.tiempo_registrado,'00:00:00'))+
        DATEDIFF(SECOND,0,COALESCE(t2.tiempo_registrado,'00:00:00'))+
        DATEDIFF(SECOND,0,COALESCE(t3.tiempo_registrado,'00:00:00')),
      0)
    ) AS TiempoTotal
FROM Carrera5km AS c
LEFT JOIN Tiempos AS t1 ON c.folio_chip = t1.folio_chip AND t1.NumTiempo = 1
LEFT JOIN Tiempos AS t2 ON c.folio_chip = t2.folio_chip AND t2.NumTiempo = 2
LEFT JOIN Tiempos AS t3 ON c.folio_chip = t3.folio_chip AND t3.NumTiempo = 3
ORDER BY Posición;
GO

SELECT nombre_categoria
FROM CATEGORIA
ORDER BY TRY_CAST(LEFT(nombre_categoria, CHARINDEX(' ', nombre_categoria) - 1) AS INT);
