----------------------------------------------------------
-- Parámetro: nombre de la base
----------------------------------------------------------
DECLARE @DBName SYSNAME = N'Cronometraje_Carreras';
DECLARE @sql    NVARCHAR(MAX);

----------------------------------------------------------
-- 1) Crear base si no existe
----------------------------------------------------------
SET @sql = 
    N'IF DB_ID(' + QUOTENAME(@DBName,'''') + N') IS NULL
        CREATE DATABASE ' + QUOTENAME(@DBName) + N';';
EXEC sp_executesql @sql;

----------------------------------------------------------
-- 2) Configuración de la base de datos
----------------------------------------------------------
SET @sql =
    N'ALTER DATABASE ' + QUOTENAME(@DBName) + N' SET COMPATIBILITY_LEVEL = 150;
      -- (…otras ALTER DATABASE…)
      EXEC sys.sp_db_vardecimal_storage_format ' 
        + QUOTENAME(@DBName,'''') + N', N''ON'';
      ALTER DATABASE ' + QUOTENAME(@DBName) + N' SET QUERY_STORE = OFF;';
EXEC sp_executesql @sql;

----------------------------------------------------------
-- 3) Todo el DDL/DML en un solo batch dinámico
----------------------------------------------------------
SET @sql =
    N'USE ' + QUOTENAME(@DBName) + N';
     BEGIN TRY
       BEGIN TRANSACTION;

       -- Eliminar en orden inverso
       DROP TABLE IF EXISTS dbo.TIEMPO;
       DROP TABLE IF EXISTS dbo.TELEFONO;
       DROP TABLE IF EXISTS dbo.Vincula_participante;
       DROP TABLE IF EXISTS dbo.CARR_Cat;
       DROP TABLE IF EXISTS dbo.CONTACTANOS;
       DROP TABLE IF EXISTS dbo.CORREDOR;
       DROP TABLE IF EXISTS dbo.ADMINISTRADOR;
       DROP TABLE IF EXISTS dbo.CATEGORIA;
       DROP TABLE IF EXISTS dbo.CARRERA;

       -- Tablas maestras
       CREATE TABLE dbo.CARRERA (
         ID_carrera INT IDENTITY(1,1) PRIMARY KEY,
         nom_carrera VARCHAR(40) NOT NULL,
         year_carrera INT NOT NULL,
         edi_carrera INT NOT NULL DEFAULT(1),
         CONSTRAINT UC_CARRERA UNIQUE(nom_carrera,year_carrera,edi_carrera)
       );
       CREATE TABLE dbo.CATEGORIA (
         ID_categoria INT IDENTITY(1,1) PRIMARY KEY,
         nombre_categoria VARCHAR(6) NOT NULL
       );
       CREATE TABLE dbo.ADMINISTRADOR (
         ID_admin INT IDENTITY(1,1) PRIMARY KEY,
         uss_admin VARCHAR(10) NOT NULL,
         pass_admin VARCHAR(256) NOT NULL,
         CONSTRAINT chk_usser_valid CHECK (
           LEN(uss_admin)=10 AND
           PATINDEX(''%[A-Z]%'',uss_admin)>0 AND
           PATINDEX(''%[a-z]%'',uss_admin)>0 AND
           PATINDEX(''%[0-9]%'',uss_admin)>0
         ),
         CONSTRAINT chk_password_valid CHECK (
           LEN(pass_admin)>=8 AND
           PATINDEX(''%[A-Z]%'',pass_admin)>0 AND
           PATINDEX(''%[a-z]%'',pass_admin)>0 AND
           PATINDEX(''%[0-9]%'',pass_admin)>0 AND
           PATINDEX(''%[^a-zA-Z0-9]%'',pass_admin)>0
         )
       );
       CREATE TABLE dbo.CORREDOR (
         nom_corredor VARCHAR(25) NOT NULL,
         apP_corredor VARCHAR(20) NOT NULL,
         apM_corredor VARCHAR(20) NULL,
         f_corredor DATE NOT NULL,
         sex_corredor CHAR(1) NOT NULL,
         c_corredor VARCHAR(50) NULL,
         pais VARCHAR(30) NOT NULL,
         ID_corredor AS CAST(
           HASHBYTES(
             ''SHA2_256'',
             CONCAT(
               nom_corredor,apP_corredor,
               ISNULL(apM_corredor,''''),--doble comilla escapada
               CONVERT(CHAR(8),f_corredor,112),
               sex_corredor,
               ISNULL(c_corredor,''''),  
               pais
             )
           ) AS BINARY(32)
         ) PERSISTED PRIMARY KEY,
         CONSTRAINT chk_correo_corredor CHECK(c_corredor LIKE ''%@%.%''),
         CONSTRAINT chk_pais_longitud CHECK(LEN(pais)>=4),
         CONSTRAINT chk_sexo CHECK(sex_corredor IN(''F'',''M''))
       );
       CREATE TABLE dbo.CONTACTANOS (
         ID_cont INT IDENTITY(1,1) PRIMARY KEY,
         ID_uss INT NOT NULL REFERENCES dbo.ADMINISTRADOR(ID_admin),
         nombre VARCHAR(100) NOT NULL,
         correo VARCHAR(100) NOT NULL,
         mensaje VARCHAR(MAX) NOT NULL
       );
       CREATE TABLE dbo.CARR_Cat (
         ID_carr_cat INT IDENTITY(1,1) PRIMARY KEY,
         ID_carrera INT NOT NULL REFERENCES dbo.CARRERA(ID_carrera),
         ID_categoria INT NOT NULL REFERENCES dbo.CATEGORIA(ID_categoria)
       );
       CREATE TABLE dbo.Vincula_participante (
         ID_vinculo UNIQUEIDENTIFIER NOT NULL
           DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
         ID_corredor BINARY(32) NOT NULL REFERENCES dbo.CORREDOR(ID_corredor),
         ID_carr_cat INT NOT NULL REFERENCES dbo.CARR_Cat(ID_carr_cat),
         num_corredor INT NOT NULL,
         folio_chip VARCHAR(20) NOT NULL,
         CONSTRAINT UQ_Vincula_Folio UNIQUE(folio_chip)
       );
       CREATE TABLE dbo.TELEFONO (
         ID_tele INT IDENTITY(1,1) PRIMARY KEY,
         numero VARCHAR(15) NOT NULL,
         ID_corredor BINARY(32) NOT NULL REFERENCES dbo.CORREDOR(ID_corredor)
       );
       CREATE TABLE dbo.TIEMPO (
         ID_tiempo INT IDENTITY(1,1) PRIMARY KEY,
         folio_chip VARCHAR(20) NOT NULL
           REFERENCES dbo.Vincula_participante(folio_chip),
         tiempo_registrado TIME NOT NULL
       );

       -- Datos semilla
       IF NOT EXISTS(SELECT 1 FROM dbo.CATEGORIA WHERE nombre_categoria=''1 km'')
         INSERT dbo.CATEGORIA(nombre_categoria)
         VALUES(''1 km'');
       IF NOT EXISTS(SELECT 1 FROM dbo.ADMINISTRADOR WHERE uss_admin=''Admin0001A'')
         INSERT dbo.ADMINISTRADOR(uss_admin,pass_admin)
         VALUES(''Admin0001A'',''CambiarMe123!'');

       COMMIT;
     END TRY
     BEGIN CATCH
       ROLLBACK;
       THROW;
     END CATCH;';

EXEC sp_executesql @sql;
GO
