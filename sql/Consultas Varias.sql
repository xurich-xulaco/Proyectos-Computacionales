ALTER TABLE ADMINISTRADOR
ADD CONSTRAINT chk_pass_admin
CHECK (
    LEN(pass_admin) >= 8 AND 
    pass_admin LIKE '%[A-Z]%' AND
    pass_admin LIKE '%[a-z]%' AND
    pass_admin LIKE '%[0-9]%' AND
    pass_admin LIKE '%[@$!%*?&]'
);

CREATE TRIGGER trg_validar_contrasena
ON ADMINISTRADOR
INSTEAD OF INSERT, UPDATE
AS
BEGIN
    DECLARE @pass_admin NVARCHAR(255);

    -- Obtiene el valor de pass_admin en la operación actual
    SELECT @pass_admin = pass_admin FROM inserted;

    -- Verifica si cumple los criterios de validación
    IF LEN(@pass_admin) < 8 OR 
       @pass_admin NOT LIKE '%[A-Z]%' OR 
       @pass_admin NOT LIKE '%[a-z]%' OR 
       @pass_admin NOT LIKE '%[0-9]%' OR 
       @pass_admin NOT LIKE '%[@$!%*?&]%'
    BEGIN
        RAISERROR ('La contraseña debe tener al menos 8 caracteres, incluyendo una letra mayúscula, una minúscula, un número y un símbolo especial.', 16, 1);
        ROLLBACK TRANSACTION;
    END
    ELSE
    BEGIN
        -- Si cumple, realiza la inserción o actualización
        INSERT INTO ADMINISTRADOR (ID_admin, uss_admin, pass_admin)
        SELECT ID_admin, uss_admin, pass_admin FROM inserted;
    END
END;


INSERT INTO ADMINISTRADOR (uss_admin, pass_admin)
VALUES ('Eladmin57', 'Xj45!87Aa');

DROP TRIGGER trg_validar_contrasena;

CREATE TRIGGER trg_validar_contrasena
ON ADMINISTRADOR
INSTEAD OF INSERT, UPDATE
AS
BEGIN
    DECLARE @pass_admin NVARCHAR(255);

    -- Obtiene el valor de pass_admin en la operación actual
    SELECT @pass_admin = pass_admin FROM inserted;

    -- Verifica si cumple los criterios de validación
    IF LEN(@pass_admin) < 8 OR 
       @pass_admin NOT LIKE '%[A-Z]%' OR 
       @pass_admin NOT LIKE '%[a-z]%' OR 
       @pass_admin NOT LIKE '%[0-9]%' OR 
       @pass_admin NOT LIKE '%[@$!%*?&]%'
    BEGIN
        RAISERROR ('La contraseña debe tener al menos 8 caracteres, incluyendo una letra mayúscula, una minúscula, un número y un símbolo especial.', 16, 1);
        ROLLBACK TRANSACTION;
    END
    ELSE
    BEGIN
        -- Inserta los datos sin especificar ID_admin para que SQL Server lo genere automáticamente
        INSERT INTO ADMINISTRADOR (uss_admin, pass_admin)
        SELECT uss_admin, pass_admin FROM inserted;
    END
END;

INSERT INTO ADMINISTRADOR (uss_admin, pass_admin)
VALUES ('Eladmin357', 'Xj45!87Aa');


ALTER TABLE ADMINISTRADOR
ADD CONSTRAINT chk_password_valid CHECK (
    LEN(pass_admin) >= 8 AND
    PATINDEX('%[A-Z]%', pass_admin) > 0 AND  -- Al menos una mayúscula
    PATINDEX('%[a-z]%', pass_admin) > 0 AND  -- Al menos una minúscula
    PATINDEX('%[0-9]%', pass_admin) > 0 AND  -- Al menos un número
    PATINDEX('%[^a-zA-Z0-9]%', pass_admin) > 0 -- Al menos un símbolo
);


ALTER TABLE ADMINISTRADOR
ADD CONSTRAINT chk_usser_valid CHECK (
    LEN(uss_admin) = 10 AND
    PATINDEX('%[A-Z]%', uss_admin) > 0 AND  -- Al menos una mayúscula
    PATINDEX('%[a-z]%', uss_admin) > 0 AND  -- Al menos una minúscula
    PATINDEX('%[0-9]%', uss_admin) > 0  -- Al menos un número
);

Use Cronometraje_Carreras
GO


select * from CORREDOR

ALTER TABLE CARRERA
DROP CONSTRAINT UC_Carrera;

ALTER TABLE CARRERA
ADD CONSTRAINT UC_Carrera UNIQUE (nom_carrera, year_carrera, edi_carrera);


select * from CARRERA

drop table TELEFONO
CREATE TABLE TELEFONO (
    ID_tele INT IDENTITY(1,1) PRIMARY KEY,  -- ID_tele como columna auto incremental
    numero VARCHAR(15) NOT NULL,            -- columna para el número de teléfono
    ID_Corredor int NOT NULL                -- columna para el ID del corredor
);



ALTER TABLE TELEFONO
ALTER COLUMN ID_Corredor VARCHAR(50);  -- o el tamaño adecuado

DROP TABLE Vincula_participante

CREATE TABLE Vincula_participante (
    ID_vinculo UNIQUEIDENTIFIER DEFAULT NEWID() PRIMARY KEY,  -- Genera un valor único automáticamente
    ID_corredor VARBINARY(5) NOT NULL,                        -- Tipo de dato que coincide con la columna en 'CORREDOR'
    ID_carr_cat INT NOT NULL,                                  -- Relación con la tabla de carreras (suponiendo tipo INT)
    num_corredor INT NOT NULL,
    folio_chip VARCHAR(20) NOT NULL,
    CONSTRAINT FK_IDCorredor FOREIGN KEY (ID_corredor) REFERENCES CORREDOR(ID_corredor),
    CONSTRAINT FK_IDCarrera FOREIGN KEY (ID_carr_cat) REFERENCES CARR_Cat(ID_carr_cat),
    CONSTRAINT UQ_NumCorredorPorCarrera UNIQUE (ID_carr_cat, num_corredor)  -- Restringe un solo número de corredor por carrera
);

CREATE TABLE CATEGORIA (
    ID_categoria INT PRIMARY KEY IDENTITY(1,1),
    nombre_categoria VARCHAR(6) NOT NULL
);

CREATE TABLE CARR_Cat (
    ID_carr_cat INT PRIMARY KEY IDENTITY(1,1),
    ID_carrera INT NOT NULL,
    ID_categoria INT NOT NULL,
    CONSTRAINT FK_CARRERA FOREIGN KEY (ID_carrera) REFERENCES Carrera(ID_carrera),
    CONSTRAINT FK_CATEGORIA FOREIGN KEY (ID_categoria) REFERENCES CATEGORIA(ID_categoria)
);

ALTER TABLE Vincula_participante
DROP COLUMN categoria;

ALTER TABLE Vincula_participante
DROP CONSTRAINT FK_IDCarrera;

ALTER TABLE Vincula_participante
ADD ID_carr_cat INT NOT NULL;

ALTER TABLE Vincula_participante
ADD CONSTRAINT FK_IDCarrCat
FOREIGN KEY (ID_carr_cat) REFERENCES CARR_Cat(ID_carr_cat);



Use Cronometraje_Carreras
GO

INSERT INTO ADMINISTRADOR (uss_admin, pass_admin)
VALUES ('1234567Aab', 'Xj45!87Aa');
INSERT INTO CARR_Cat(ID_carrera, ID_categoria)
VALUES ('1','5');
INSERT INTO CATEGORIA (nombre_categoria)
VALUES 
    ('1 km'), ('2 km'), ('3 km'), ('4 km'), ('5 km'), ('6 km'), ('7 km'), ('8 km'), ('9 km'), ('10 km'),
    ('11 km'), ('12 km'), ('13 km'), ('14 km'), ('15 km'), ('16 km'), ('17 km'), ('18 km'), ('19 km'), ('20 km'),
    ('21 km'), ('22 km'), ('23 km'), ('24 km'), ('25 km'), ('26 km'), ('27 km'), ('28 km'), ('29 km'), ('30 km'),
    ('31 km'), ('32 km'), ('33 km'), ('34 km'), ('35 km'), ('36 km'), ('37 km'), ('38 km'), ('39 km'), ('40 km'),
    ('41 km'), ('42 km'), ('43 km'), ('44 km'), ('45 km'), ('46 km'), ('47 km'), ('48 km'), ('49 km'), ('50 km');

-- Consulta para insertar carrera y calcular automáticamente la edición
INSERT INTO CARRERA (nom_carrera, year_carrera, edi_carrera)
VALUES ('Medio Maraton UASLP', 2024, 
    COALESCE(
        (SELECT MAX(edi_carrera) + 1 
         FROM CARRERA 
         WHERE nom_carrera = 'Medio Maraton UASLP' AND year_carrera = 2024),
        1 -- Si no hay registros previos, inicia con edición 1
    )
);
INSERT INTO CORREDOR(nom_corredor, apP_corredor,apM_corredor,f_corredor,sex_corredor, c_corredor, pais)
VALUES ('Jesus Emanuel', 'Gonzalez', 'Reyes', '2006-11-13','M','jesusglz2016@gmail.com', 'Mexico');
INSERT INTO TELEFONO (numero, ID_Corredor)
VALUES ('4861096389','0xE8CB95D19D');
INSERT INTO Tiempo(folio_chip, tiempo_registrado)
VALUES ('RFID0000000001', CONVERT(TIME, GETDATE()));
INSERT INTO Vincula_participante(ID_corredor, ID_carr_cat, num_corredor, folio_chip)
VALUES (0xEE410D1AD2, 1, 205, 'RFID0000000001');

delete from CORREDOR where nom_corredor= 'Luis Daniel'
delete from Vincula_participante where folio_chip= 'RFID1000000004'
delete from TELEFONO where numero = '4441006969'

select * from ADMINISTRADOR
select * from CARR_Cat
select * from CATEGORIA
select * from CARRERA
select * from CORREDOR
select * from Tiempo
select * from Vincula_participante
select * from TELEFONO


SELECT ID_carrera, CONCAT(nom_carrera, ' - ', year_carrera, ' (Edición: ', edi_carrera, ')') AS CarreraInfo
FROM CARRERA
ORDER BY year_carrera, edi_carrera;



WITH TiemposCorredor AS (
    SELECT 
        folio_chip,
        tiempo_registrado,
        ROW_NUMBER() OVER (PARTITION BY folio_chip ORDER BY tiempo_registrado) AS NumTiempo
    FROM dbo.Tiempo
)
SELECT 
    RANK() OVER (ORDER BY 
        DATEDIFF(SECOND, 0, COALESCE(T1.tiempo_registrado, '00:00:00')) +
        DATEDIFF(SECOND, 0, COALESCE(T2.tiempo_registrado, '00:00:00')) +
        DATEDIFF(SECOND, 0, COALESCE(T3.tiempo_registrado, '00:00:00'))
    ) AS Posicion,
    v.num_corredor AS NumCorredor, 
    c.nom_corredor AS Nombre,
    COALESCE(T1.tiempo_registrado, '00:00:00') AS T1,
    COALESCE(T2.tiempo_registrado, '00:00:00') AS T2,
    COALESCE(T3.tiempo_registrado, '00:00:00') AS T3,
    CONVERT(TIME, DATEADD(SECOND, 
        DATEDIFF(SECOND, 0, COALESCE(T1.tiempo_registrado, '00:00:00')) +
        DATEDIFF(SECOND, 0, COALESCE(T2.tiempo_registrado, '00:00:00')) +
        DATEDIFF(SECOND, 0, COALESCE(T3.tiempo_registrado, '00:00:00')),
        0
    )) AS TiempoTotal
FROM CARRERA ca
JOIN CARR_Cat cc ON ca.ID_carrera = cc.ID_carrera  
JOIN CATEGORIA cat ON cc.ID_categoria = cat.ID_categoria
JOIN Vincula_participante v ON cc.ID_carr_cat = v.ID_Carr_cat
JOIN dbo.CORREDOR c ON v.ID_corredor = c.ID_corredor
LEFT JOIN TiemposCorredor T1 ON v.folio_chip = T1.folio_chip AND T1.NumTiempo = 1
LEFT JOIN TiemposCorredor T2 ON v.folio_chip = T2.folio_chip AND T2.NumTiempo = 2
LEFT JOIN TiemposCorredor T3 ON v.folio_chip = T3.folio_chip AND T3.NumTiempo = 3
WHERE ca.year_carrera = 2024
  AND ca.edi_carrera = 1
  AND cat.nombre_categoria = '5 km'
ORDER BY Posicion;


SELECT 
        ca.nom_carrera AS NombreCarrera,
        ca.year_carrera AS Año,
        ca.edi_carrera AS Edición,
        STRING_AGG(cat.nombre_categoria, ', ') AS Categorias
    FROM CARRERA ca
    JOIN CARR_CAT cc ON ca.ID_carrera = cc.ID_carrera
    JOIN CATEGORIA cat ON cc.ID_categoria = cat.ID_categoria
    GROUP BY ca.nom_carrera, ca.year_carrera, ca.edi_carrera
    ORDER BY ca.year_carrera, ca.edi_carrera;

SELECT 
    ca.ID_carrera,
    CONCAT(ca.nom_carrera, ' - ', ca.year_carrera, ' (Edición: ', ca.edi_carrera, ')','(',(STRING_AGG(cat.nombre_categoria, ', ')),')') AS Carrera
FROM CARRERA ca
JOIN CARR_CAT cc ON ca.ID_carrera = cc.ID_carrera
JOIN CATEGORIA cat ON cc.ID_categoria = cat.ID_categoria
GROUP BY ca.ID_carrera, ca.nom_carrera, ca.year_carrera, ca.edi_carrera
ORDER BY ca.year_carrera DESC, ca.edi_carrera DESC

delete from CARR_Cat where ID_carrera=2007;


SELECT 
    ca.ID_carrera,
    CONCAT(ca.nom_carrera, ' - ', ca.year_carrera, ' (Edición: ', ca.edi_carrera, ')', ' (', STRING_AGG(cat.nombre_categoria, ', '), ')') AS Carrera
FROM CARRERA ca
JOIN CARR_CAT cc ON ca.ID_carrera = cc.ID_carrera
JOIN CATEGORIA cat ON cc.ID_categoria = cat.ID_categoria
GROUP BY ca.ID_carrera, ca.nom_carrera, ca.year_carrera, ca.edi_carrera
HAVING COUNT(cc.ID_categoria) = 3
ORDER BY ca.year_carrera DESC, ca.edi_carrera DESC

SELECT DISTINCT cat.nombre_categoria
            FROM CATEGORIA cat
            INNER JOIN CARR_Cat cc ON cat.ID_categoria = cc.ID_categoria
            INNER JOIN CARRERA ca ON cc.ID_carrera = ca.ID_carrera
            WHERE ca.year_carrera = 2024 AND ca.edi_carrera = 1
            ORDER BY cat.nombre_categoria


SELECT c.ID_corredor, CONCAT(c.nom_corredor, ' ', c.apP_corredor, ' ', c.apM_corredor) AS NombreCompleto
            FROM CORREDOR c
            INNER JOIN Vincula_participante vp ON c.ID_corredor = vp.ID_corredor
            INNER JOIN CARR_Cat cc ON vp.ID_carr_cat = cc.ID_carr_cat
            INNER JOIN CARRERA ca ON cc.ID_carrera = ca.ID_carrera
            INNER JOIN CATEGORIA cat ON cc.ID_categoria = cat.ID_categoria
            WHERE ca.year_carrera = 2024 AND ca.edi_carrera = 1 AND cat.nombre_categoria = '5 km'
            ORDER BY c.apP_corredor, c.apM_corredor, c.nom_corredor






**************************************************************************************************************************************************


use Cronometraje_Carreras
go
drop table TELEFONO
CREATE TABLE TELEFONO (
    ID_tele INT IDENTITY(1,1) PRIMARY KEY,  -- ID_tele como columna auto incremental
    numero VARCHAR(15) NOT NULL,            -- columna para el número de teléfono
    ID_Corredor varbinary(5) NOT NULL                -- columna para el ID del corredor
);

SELECT 
    c.nom_corredor, 
    c.apP_corredor, 
    c.apM_corredor, 
    c.f_corredor, 
    c.sex_corredor, 
    c.pais, 
    c.c_corredor
FROM CORREDOR c
INNER JOIN Vincula_participante vp ON c.ID_corredor = vp.ID_corredor
INNER JOIN CARR_Cat cc ON vp.ID_carr_cat = cc.ID_carr_cat
INNER JOIN CARRERA ca ON cc.ID_carrera = ca.ID_carrera
INNER JOIN CATEGORIA cat ON cc.ID_categoria = cat.ID_categoria
WHERE vp.num_corredor = 206
  AND ca.year_carrera = 2024
  AND ca.edi_carrera = 1
  AND cat.nombre_categoria ='5 km';

SELECT c.nom_corredor, c.apP_corredor, c.apM_corredor, c.f_corredor, 
       c.sex_corredor, c.pais, c.c_corredor, vp.ID_corredor
FROM CORREDOR c
JOIN Vincula_participante vp ON c.ID_corredor = vp.ID_corredor
JOIN CARR_Cat cc ON vp.ID_carr_cat = cc.ID_carr_cat
JOIN CARRERA ca ON cc.ID_carrera = ca.ID_carrera
JOIN CATEGORIA cat ON cc.ID_categoria = cat.ID_categoria
WHERE ca.year_carrera = 2024 
  AND ca.edi_carrera = 1 
  AND cat.nombre_categoria = '5 km' 
  AND vp.num_corredor = 206

