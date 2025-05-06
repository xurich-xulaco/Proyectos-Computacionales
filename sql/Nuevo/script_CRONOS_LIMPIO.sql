CREATE DATABASE [Cronometraje]

USE [Cronometraje]
GO

/****** Object:  Database [Cronometraje_Carreras]    Script Date: 05/04/2025 03:39:16 a. m. ******/
CREATE DATABASE [Cronometraje]
GO
ALTER DATABASE [Cronometraje] SET COMPATIBILITY_LEVEL = 150
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [Cronometraje].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [Cronometraje] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [Cronometraje] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [Cronometraje] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [Cronometraje] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [Cronometraje] SET ARITHABORT OFF 
GO
ALTER DATABASE [Cronometraje] SET AUTO_CLOSE OFF 
GO
ALTER DATABASE [Cronometraje] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [Cronometraje] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [Cronometraje] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [Cronometraje] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [Cronometraje] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [Cronometraje] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [Cronometraje] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [Cronometraje] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [Cronometraje] SET  ENABLE_BROKER 
GO
ALTER DATABASE [Cronometraje] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [Cronometraje] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [Cronometraje] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [Cronometraje] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [Cronometraje] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [Cronometraje] SET READ_COMMITTED_SNAPSHOT OFF 
GO
ALTER DATABASE [Cronometraje] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [Cronometraje] SET RECOVERY FULL 
GO
ALTER DATABASE [Cronometraje] SET  MULTI_USER 
GO
ALTER DATABASE [Cronometraje] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [Cronometraje] SET DB_CHAINING OFF 
GO
ALTER DATABASE [Cronometraje] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [Cronometraje] SET TARGET_RECOVERY_TIME = 60 SECONDS 
GO
ALTER DATABASE [Cronometraje] SET DELAYED_DURABILITY = DISABLED 
GO
ALTER DATABASE [Cronometraje] SET ACCELERATED_DATABASE_RECOVERY = OFF  
GO
EXEC sys.sp_db_vardecimal_storage_format N'Cronometraje_Carreras', N'ON'
GO
ALTER DATABASE [Cronometraje] SET QUERY_STORE = OFF
GO
USE [Cronometraje]
GO
/****** Object:  Table [dbo].[ADMINISTRADOR]    Script Date: 05/04/2025 03:39:16 a. m. ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ADMINISTRADOR](
	[ID_admin] [int] IDENTITY(1,1) NOT NULL,
	[uss_admin] [varchar](10) NOT NULL,
	[pass_admin] [varchar](10) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[ID_admin] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CARR_Cat]    Script Date: 05/04/2025 03:39:16 a. m. ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CARR_Cat](
	[ID_carr_cat] [int] IDENTITY(1,1) NOT NULL,
	[ID_carrera] [int] NOT NULL,
	[ID_categoria] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[ID_carr_cat] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CARRERA]    Script Date: 05/04/2025 03:39:16 a. m. ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CARRERA](
	[ID_carrera] [int] IDENTITY(1,1) NOT NULL,
	[nom_carrera] [varchar](40) NOT NULL,
	[year_carrera] [int] NOT NULL,
	[edi_carrera] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[ID_carrera] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UC_Carrera] UNIQUE NONCLUSTERED 
(
	[nom_carrera] ASC,
	[year_carrera] ASC,
	[edi_carrera] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CATEGORIA]    Script Date: 05/04/2025 03:39:16 a. m. ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CATEGORIA](
	[ID_categoria] [int] IDENTITY(1,1) NOT NULL,
	[nombre_categoria] [varchar](6) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[ID_categoria] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CORREDOR]    Script Date: 05/04/2025 03:39:16 a. m. ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CORREDOR](
	[ID_corredor]  AS (CONVERT([varbinary](5),hashbytes('SHA1',concat([nom_corredor],[apP_corredor],coalesce([apM_corredor],''),CONVERT([varchar],[f_corredor],(112)))))) PERSISTED NOT NULL,
	[nom_corredor] [varchar](25) NOT NULL,
	[apP_corredor] [varchar](20) NOT NULL,
	[apM_corredor] [varchar](20) NULL,
	[f_corredor] [date] NOT NULL,
	[sex_corredor] [char](1) NOT NULL,
	[c_corredor] [varchar](50) NULL,
	[pais] [varchar](30) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[ID_corredor] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TELEFONO]    Script Date: 05/04/2025 03:39:16 a. m. ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TELEFONO](
	[ID_tele] [int] IDENTITY(1,1) NOT NULL,
	[numero] [varchar](15) NOT NULL,
	[ID_Corredor] [varchar](50) NULL,
PRIMARY KEY CLUSTERED 
(
	[ID_tele] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Tiempo]    Script Date: 05/04/2025 03:39:16 a. m. ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Tiempo](
	[ID_tiempo] [int] IDENTITY(1,1) NOT NULL,
	[folio_chip] [varchar](20) NOT NULL,
	[tiempo_registrado] [time](7) NOT NULL,
 CONSTRAINT [PK_Tiempo] PRIMARY KEY CLUSTERED 
(
	[ID_tiempo] ASC,
	[folio_chip] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Vincula_participante]    Script Date: 05/04/2025 03:39:16 a. m. ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Vincula_participante](
	[ID_vinculo] [uniqueidentifier] NOT NULL,
	[ID_corredor] [varbinary](5) NOT NULL,
	[ID_carr_cat] [int] NOT NULL,
	[num_corredor] [int] NOT NULL,
	[folio_chip] [varchar](20) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[ID_vinculo] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_NumCorredorPorCarrera] UNIQUE NONCLUSTERED 
(
	[ID_carr_cat] ASC,
	[num_corredor] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[CARRERA] ADD  DEFAULT ((1)) FOR [edi_carrera]
GO
ALTER TABLE [dbo].[Vincula_participante] ADD  DEFAULT (newid()) FOR [ID_vinculo]
GO
ALTER TABLE [dbo].[CARR_Cat]  WITH CHECK ADD  CONSTRAINT [FK_CARRERA] FOREIGN KEY([ID_carrera])
REFERENCES [dbo].[CARRERA] ([ID_carrera])
GO
ALTER TABLE [dbo].[CARR_Cat] CHECK CONSTRAINT [FK_CARRERA]
GO
ALTER TABLE [dbo].[CARR_Cat]  WITH CHECK ADD  CONSTRAINT [FK_CATEGORIA] FOREIGN KEY([ID_categoria])
REFERENCES [dbo].[CATEGORIA] ([ID_categoria])
GO
ALTER TABLE [dbo].[CARR_Cat] CHECK CONSTRAINT [FK_CATEGORIA]
GO
ALTER TABLE [dbo].[Vincula_participante]  WITH CHECK ADD  CONSTRAINT [FK_IDCarrera] FOREIGN KEY([ID_carr_cat])
REFERENCES [dbo].[CARR_Cat] ([ID_carr_cat])
GO
ALTER TABLE [dbo].[Vincula_participante] CHECK CONSTRAINT [FK_IDCarrera]
GO
ALTER TABLE [dbo].[Vincula_participante]  WITH CHECK ADD  CONSTRAINT [FK_IDCorredor] FOREIGN KEY([ID_corredor])
REFERENCES [dbo].[CORREDOR] ([ID_corredor])
GO
ALTER TABLE [dbo].[Vincula_participante] CHECK CONSTRAINT [FK_IDCorredor]
GO
ALTER TABLE [dbo].[ADMINISTRADOR]  WITH CHECK ADD  CONSTRAINT [chk_password_valid] CHECK  ((len([pass_admin])>=(8) AND patindex('%[A-Z]%',[pass_admin])>(0) AND patindex('%[a-z]%',[pass_admin])>(0) AND patindex('%[0-9]%',[pass_admin])>(0) AND patindex('%[^a-zA-Z0-9]%',[pass_admin])>(0)))
GO
ALTER TABLE [dbo].[ADMINISTRADOR] CHECK CONSTRAINT [chk_password_valid]
GO
ALTER TABLE [dbo].[ADMINISTRADOR]  WITH CHECK ADD  CONSTRAINT [chk_usser_valid] CHECK  ((len([uss_admin])=(10) AND patindex('%[A-Z]%',[uss_admin])>(0) AND patindex('%[a-z]%',[uss_admin])>(0) AND patindex('%[0-9]%',[uss_admin])>(0)))
GO
ALTER TABLE [dbo].[ADMINISTRADOR] CHECK CONSTRAINT [chk_usser_valid]
GO
ALTER TABLE [dbo].[CORREDOR]  WITH CHECK ADD  CONSTRAINT [chk_correo_corredor] CHECK  (([c_corredor] like '%@%.%'))
GO
ALTER TABLE [dbo].[CORREDOR] CHECK CONSTRAINT [chk_correo_corredor]
GO
ALTER TABLE [dbo].[CORREDOR]  WITH CHECK ADD CHECK  ((len([pais])>=(4)))
GO
ALTER TABLE [dbo].[CORREDOR]  WITH CHECK ADD CHECK  (([sex_corredor]='F' OR [sex_corredor]='M'))
GO
ALTER TABLE [dbo].[CORREDOR]  WITH CHECK ADD CHECK  (([sex_corredor]='F' OR [sex_corredor]='M'))
GO
USE [master]
GO
ALTER DATABASE [Cronometraje] SET  READ_WRITE 
GO



INSERT INTO ADMINISTRADOR (uss_admin, pass_admin)
VALUES ('1234567Aab', 'Xj45!87Aa');

INSERT INTO CATEGORIA (nombre_categoria)
VALUES 
    ('1 km'), ('2 km'), ('3 km'), ('4 km'), ('5 km'), ('6 km'), ('7 km'), ('8 km'), ('9 km'), ('10 km'),
    ('11 km'), ('12 km'), ('13 km'), ('14 km'), ('15 km'), ('16 km'), ('17 km'), ('18 km'), ('19 km'), ('20 km'),
    ('21 km'), ('22 km'), ('23 km'), ('24 km'), ('25 km'), ('26 km'), ('27 km'), ('28 km'), ('29 km'), ('30 km'),
    ('31 km'), ('32 km'), ('33 km'), ('34 km'), ('35 km'), ('36 km'), ('37 km'), ('38 km'), ('39 km'), ('40 km'),
    ('41 km'), ('42 km'), ('43 km'), ('44 km'), ('45 km'), ('46 km'), ('47 km'), ('48 km'), ('49 km'), ('50 km');