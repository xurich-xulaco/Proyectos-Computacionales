-- Habilité "SQLCMD Mode" para un uso optimo de todos los scripts
-- Query → SQLCMD Mode
-- ================================================
-- Configuración inicial: cambiar sólo DBName si lo deseas
-- ================================================
:setvar DBName Cronometraje

IF DB_ID('$(DBName)') IS NULL
    CREATE DATABASE [$(DBName)];
GO

USE [$(DBName)];
GO

-- ================================================
-- Transacción principal con manejo de errores
-- ================================================
BEGIN TRY
    BEGIN TRANSACTION;

    -- ================================================
    -- 1) Eliminar tablas en orden inverso a sus dependencias
    -- ================================================
    IF OBJECT_ID('dbo.TIEMPO','U') IS NOT NULL
        DROP TABLE dbo.TIEMPO;
    IF OBJECT_ID('dbo.Vincula_participante','U') IS NOT NULL
        DROP TABLE dbo.Vincula_participante;
    IF OBJECT_ID('dbo.TELEFONO','U') IS NOT NULL
        DROP TABLE dbo.TELEFONO;
    IF OBJECT_ID('dbo.CARR_Cat','U') IS NOT NULL
        DROP TABLE dbo.CARR_Cat;
    IF OBJECT_ID('dbo.CONTACTANOS','U') IS NOT NULL
        DROP TABLE dbo.CONTACTANOS;
    IF OBJECT_ID('dbo.CORREDOR','U') IS NOT NULL
        DROP TABLE dbo.CORREDOR;
    IF OBJECT_ID('dbo.ADMINISTRADOR','U') IS NOT NULL
        DROP TABLE dbo.ADMINISTRADOR;
    IF OBJECT_ID('dbo.CATEGORIA','U') IS NOT NULL
        DROP TABLE dbo.CATEGORIA;
    IF OBJECT_ID('dbo.CARRERA','U') IS NOT NULL
        DROP TABLE dbo.CARRERA;
    GO

    -- ================================================
    -- 2) Crear tablas maestras (sin FKs aún)
    -- ================================================
    CREATE TABLE dbo.CARRERA
    (
        ID_carrera     INT IDENTITY(1,1) NOT NULL
      , nom_carrera    VARCHAR(100)       NOT NULL
      , year_carrera   INT                 NOT NULL
      , edi_carrera    INT                 NOT NULL
      , CONSTRAINT PK_CARRERA PRIMARY KEY(ID_carrera)
    );
    GO

    CREATE TABLE dbo.CATEGORIA
    (
        ID_categoria      INT IDENTITY(1,1) NOT NULL
      , nombre_categoria VARCHAR(100)       NOT NULL
      , CONSTRAINT PK_CATEGORIA PRIMARY KEY(ID_categoria)
    );
    GO

    CREATE TABLE dbo.ADMINISTRADOR
    (
        ID_admin    INT IDENTITY(1,1) NOT NULL
      , uss_admin   VARCHAR(50)       NOT NULL
      , pass_admin  VARCHAR(256)      NOT NULL
      , CONSTRAINT PK_ADMINISTRADOR PRIMARY KEY(ID_admin)
    );
    GO

    -- ================================================
    -- 3) Crear CORREDOR con columna computada para ID
    -- ================================================
    CREATE TABLE dbo.CORREDOR
    (
        nom_corredor   VARCHAR(100) NOT NULL
      , apP_corredor   VARCHAR(100) NOT NULL
      , apM_corredor   VARCHAR(100) NULL
      , f_corredor     DATE         NOT NULL
      , sex_corredor   CHAR(1)      NOT NULL
      , c_corredor     VARCHAR(50)  NULL
      , pais           VARCHAR(50)  NULL
      -- ID_corredor se calcula como SHA2_256 de los demás campos, y persiste
      , ID_corredor    AS 
            HASHBYTES(
              'SHA2_256',
              CONCAT(
                nom_corredor, apP_corredor, ISNULL(apM_corredor,''),
                CONVERT(CHAR(8), f_corredor, 112),
                sex_corredor, ISNULL(c_corredor,''), ISNULL(pais,'')
              )
            ) PERSISTED
      , CONSTRAINT PK_CORREDOR PRIMARY KEY(ID_corredor)
    );
    GO

    CREATE TABLE dbo.CONTACTANOS
    (
        ID_cont    INT IDENTITY(1,1) NOT NULL
      , ID_uss     INT                NOT NULL
      , nombre     VARCHAR(100)       NOT NULL
      , correo     VARCHAR(100)       NOT NULL
      , mensaje    VARCHAR(MAX)       NOT NULL
      , CONSTRAINT PK_CONTACTANOS PRIMARY KEY(ID_cont)
      , CONSTRAINT FK_CONTACTANOS_ADMIN 
          FOREIGN KEY(ID_uss) REFERENCES dbo.ADMINISTRADOR(ID_admin)
    );
    GO

    CREATE TABLE dbo.CARR_Cat
    (
        ID_carr_cat   INT IDENTITY(1,1) NOT NULL
      , ID_carrera    INT                NOT NULL
      , ID_categoria  INT                NOT NULL
      , CONSTRAINT PK_CARR_Cat PRIMARY KEY(ID_carr_cat)
      , CONSTRAINT FK_CARR_Cat_CARRERA  
          FOREIGN KEY(ID_carrera) REFERENCES dbo.CARRERA(ID_carrera)
      , CONSTRAINT FK_CARR_Cat_CATEGORIA
          FOREIGN KEY(ID_categoria) REFERENCES dbo.CATEGORIA(ID_categoria)
    );
    GO

    CREATE TABLE dbo.Vincula_participante
    (
        ID_vinculo     UNIQUEIDENTIFIER   DEFAULT NEWSEQUENTIALID() NOT NULL
      , ID_corredor    VARBINARY(32)      NOT NULL
      , ID_carr_cat    INT                NOT NULL
      , num_corredor   INT                NOT NULL
      , folio_chip     VARCHAR(50)        NOT NULL
      , CONSTRAINT PK_Vincula PRIMARY KEY(ID_vinculo)
      , CONSTRAINT UQ_Vincula_Folio UNIQUE(folio_chip)
      , CONSTRAINT FK_Vincula_Corredor 
          FOREIGN KEY(ID_corredor) REFERENCES dbo.CORREDOR(ID_corredor)
      , CONSTRAINT FK_Vincula_CarrCat  
          FOREIGN KEY(ID_carr_cat) REFERENCES dbo.CARR_Cat(ID_carr_cat)
    );
    GO

    CREATE TABLE dbo.TELEFONO
    (
        ID_tele     INT IDENTITY(1,1) NOT NULL
      , numero      VARCHAR(20)        NOT NULL
      , ID_corredor VARBINARY(32)      NOT NULL
      , CONSTRAINT PK_TELEFONO PRIMARY KEY(ID_tele)
      , CONSTRAINT FK_TELEFONO_Corredor 
          FOREIGN KEY(ID_corredor) REFERENCES dbo.CORREDOR(ID_corredor)
    );
    GO

    CREATE TABLE dbo.TIEMPO
    (
        ID_tiempo         INT IDENTITY(1,1) NOT NULL
      , folio_chip        VARCHAR(50)        NOT NULL
      , tiempo_registrado TIME               NOT NULL
      , CONSTRAINT PK_TIEMPO PRIMARY KEY(ID_tiempo)
      -- Opcional: asegurar que el chip exista en Vincula_participante
      , CONSTRAINT FK_TIEMPO_Folio 
          FOREIGN KEY(folio_chip) REFERENCES dbo.Vincula_participante(folio_chip)
    );
    GO

    -- ================================================
    -- 4) (Opcional) Inserts iniciales de ejemplo
    -- ================================================
    IF NOT EXISTS(SELECT 1 FROM dbo.CATEGORIA WHERE nombre_categoria = '1 km')
    BEGIN
        INSERT INTO dbo.CATEGORIA(nombre_categoria)
        VALUES ('1 km'),('2 km'),('5 km'),('10 km'),('21 km'),('42 km'),('50 km');
    END
    GO

    IF NOT EXISTS(SELECT 1 FROM dbo.ADMINISTRADOR WHERE uss_admin = 'admin')
    BEGIN
        INSERT INTO dbo.ADMINISTRADOR(uss_admin, pass_admin)
        VALUES('admin','CambiarMe123!');
    END
    GO

    -- ================================================
    -- Confirmar transacción
    -- ================================================
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    THROW;
END CATCH
GO

-- ================================================
-- Fin del script “plug-and-play”
-- ================================================
