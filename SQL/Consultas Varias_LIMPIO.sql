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

-- ================================================
-- AJUSTES Y CONSTRAINTS EN ADMINISTRADOR
-- ================================================
BEGIN TRY
    BEGIN TRANSACTION;

    -- 1) CHECK de contraseña
    IF NOT EXISTS(
        SELECT 1 FROM sys.check_constraints 
        WHERE name = 'chk_pass_admin'
    )
    ALTER TABLE dbo.ADMINISTRADOR
    ADD CONSTRAINT chk_pass_admin
    CHECK (
        LEN(pass_admin) >= 8 AND
        PATINDEX('%[A-Z]%', pass_admin) > 0 AND
        PATINDEX('%[a-z]%', pass_admin) > 0 AND
        PATINDEX('%[0-9]%', pass_admin) > 0 AND
        PATINDEX('%[^a-zA-Z0-9]%', pass_admin) > 0
    );
    GO

    -- 2) CHECK de usuario
    IF NOT EXISTS(
        SELECT 1 FROM sys.check_constraints 
        WHERE name = 'chk_uss_admin'
    )
    ALTER TABLE dbo.ADMINISTRADOR
    ADD CONSTRAINT chk_uss_admin
    CHECK (
        LEN(uss_admin) = 10 AND
        PATINDEX('%[A-Z]%', uss_admin) > 0 AND
        PATINDEX('%[a-z]%', uss_admin) > 0 AND
        PATINDEX('%[0-9]%', uss_admin) > 0
    );
    GO

    -- 3) Trigger de validación de contraseña
    IF OBJECT_ID('dbo.trg_validar_contrasena','TR') IS NOT NULL
        DROP TRIGGER dbo.trg_validar_contrasena;
    GO

    CREATE TRIGGER dbo.trg_validar_contrasena
    ON dbo.ADMINISTRADOR
    AFTER INSERT, UPDATE
    AS
    BEGIN
        SET NOCOUNT ON;
        IF EXISTS (
            SELECT 1
            FROM inserted
            WHERE
                LEN(pass_admin) < 8 OR
                pass_admin    NOT LIKE '%[A-Z]%' OR
                pass_admin    NOT LIKE '%[a-z]%' OR
                pass_admin    NOT LIKE '%[0-9]%' OR
                pass_admin    NOT LIKE '%[^a-zA-Z0-9]%'
        )
        BEGIN
            RAISERROR(
                'La contraseña debe tener ≥8 caracteres, una mayúscula, una minúscula, un número y un símbolo.',
                16, 1
            );
            ROLLBACK TRANSACTION;
        END
    END;
    GO

    -- 4) Insert de ejemplo en ADMINISTRADOR
    IF NOT EXISTS(
        SELECT 1 FROM dbo.ADMINISTRADOR WHERE uss_admin = 'Eladmin57'
    )
        INSERT INTO dbo.ADMINISTRADOR(uss_admin, pass_admin)
        VALUES('Eladmin57','Xj45!87Aa');
    GO


    -- ================================================
    -- RECREAR TELEFONO
    -- ================================================
    IF OBJECT_ID('dbo.TELEFONO','U') IS NOT NULL
        DROP TABLE dbo.TELEFONO;
    GO

    CREATE TABLE dbo.TELEFONO
    (
        ID_tele     INT IDENTITY(1,1) NOT NULL,
        numero      VARCHAR(20)        NOT NULL,
        ID_corredor VARBINARY(32)      NOT NULL,
        CONSTRAINT PK_TELEFONO PRIMARY KEY(ID_tele),
        CONSTRAINT FK_TELEFONO_Corredor 
            FOREIGN KEY(ID_corredor)
            REFERENCES dbo.CORREDOR(ID_corredor)
    );
    GO


    -- ================================================
    -- RECREAR VINCULA_PARTICIPANTE
    -- ================================================
    IF OBJECT_ID('dbo.Vincula_participante','U') IS NOT NULL
        DROP TABLE dbo.Vincula_participante;
    GO

    CREATE TABLE dbo.Vincula_participante
    (
        ID_vinculo   UNIQUEIDENTIFIER DEFAULT NEWSEQUENTIALID() NOT NULL,
        ID_corredor  VARBINARY(32)      NOT NULL,
        ID_carr_cat  INT                NOT NULL,
        num_corredor INT                NOT NULL,
        folio_chip   VARCHAR(50)        NOT NULL,
        CONSTRAINT PK_Vincula PRIMARY KEY(ID_vinculo),
        CONSTRAINT UQ_Vincula_Folio UNIQUE(folio_chip),
        CONSTRAINT FK_Vincula_Corredor 
            FOREIGN KEY(ID_corredor)
            REFERENCES dbo.CORREDOR(ID_corredor),
        CONSTRAINT FK_Vincula_CarrCat  
            FOREIGN KEY(ID_carr_cat)
            REFERENCES dbo.CARR_Cat(ID_carr_cat),
        CONSTRAINT UQ_NumCorredorPorCarrera 
            UNIQUE(ID_carr_cat, num_corredor)
    );
    GO


    -- ================================================
    -- INSERTS DE CATEGORÍAS FINALES
    -- ================================================
    IF NOT EXISTS(SELECT 1 FROM dbo.CATEGORIA)
    BEGIN
        INSERT INTO dbo.CATEGORIA(nombre_categoria)
        VALUES 
            ('1 km'),
            ('2 km'),
            ('5 km'),
            ('10 km'),
            ('21 km'),
            ('42 km'),
            ('50 km');
    END
    GO


    -- ================================================
    -- INSERTS SIMULADOS (opcional)
    -- ================================================
    DECLARE @DummyCorredor VARBINARY(32) 
        = (SELECT TOP 1 ID_corredor FROM dbo.CORREDOR);

    IF @DummyCorredor IS NOT NULL
    BEGIN
        INSERT INTO dbo.Vincula_participante
            (ID_corredor, ID_carr_cat, num_corredor, folio_chip)
        VALUES
            (@DummyCorredor, 1, 101, 'RFID0001'),
            (@DummyCorredor, 2, 202, 'RFID0002');
    END
    GO

    IF @DummyCorredor IS NOT NULL
    BEGIN
        INSERT INTO dbo.TELEFONO(numero, ID_corredor)
        VALUES('4441234567', @DummyCorredor);
    END
    GO


    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO


-- ================================================
-- CONSULTAS DE PRUEBA
-- ================================================
-- 1) Ver todos los administradores
SELECT * FROM dbo.ADMINISTRADOR;
GO

-- 2) Listado de corredores vinculados con sus chips
SELECT 
    c.nom_corredor + ' ' + c.apP_corredor AS Nombre,
    v.num_corredor,
    v.folio_chip
FROM dbo.CORREDOR c
JOIN dbo.Vincula_participante v
  ON c.ID_corredor = v.ID_corredor;
GO

-- 3) Tiempos ordenados por chip
WITH T AS (
    SELECT folio_chip, tiempo_registrado,
           ROW_NUMBER() OVER (
             PARTITION BY folio_chip 
             ORDER BY tiempo_registrado
           ) AS N
    FROM dbo.TIEMPO
)
SELECT
    folio_chip,
    MAX(CASE WHEN N=1 THEN tiempo_registrado END) AS T1,
    MAX(CASE WHEN N=2 THEN tiempo_registrado END) AS T2,
    MAX(CASE WHEN N=3 THEN tiempo_registrado END) AS T3
FROM T
GROUP BY folio_chip;
GO

-- 4) Carreras con sus categorías
SELECT
    ca.nom_carrera,
    ca.year_carrera,
    STRING_AGG(cat.nombre_categoria, ', ') AS Categorias
FROM dbo.CARRERA ca
JOIN dbo.CARR_Cat cc ON ca.ID_carrera = cc.ID_carrera
JOIN dbo.CATEGORIA cat ON cc.ID_categoria = cat.ID_categoria
GROUP BY ca.nom_carrera, ca.year_carrera;
GO
