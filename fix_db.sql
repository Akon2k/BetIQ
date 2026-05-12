USE [BetIQ_DB];
GO

-- Columnas faltantes en Partidos_NBA
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Partidos_NBA]') AND name = N'CuotaLocal')
    ALTER TABLE [Partidos_NBA] ADD [CuotaLocal] DECIMAL(18,2) NULL;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Partidos_NBA]') AND name = N'CuotaVisitante')
    ALTER TABLE [Partidos_NBA] ADD [CuotaVisitante] DECIMAL(18,2) NULL;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Partidos_NBA]') AND name = N'NetRatingLocal')
    ALTER TABLE [Partidos_NBA] ADD [NetRatingLocal] DECIMAL(18,2) NULL;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Partidos_NBA]') AND name = N'NetRatingVisitante')
    ALTER TABLE [Partidos_NBA] ADD [NetRatingVisitante] DECIMAL(18,2) NULL;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Partidos_NBA]') AND name = N'TrueShootingLocal')
    ALTER TABLE [Partidos_NBA] ADD [TrueShootingLocal] DECIMAL(18,2) NULL;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Partidos_NBA]') AND name = N'TrueShootingVisitante')
    ALTER TABLE [Partidos_NBA] ADD [TrueShootingVisitante] DECIMAL(18,2) NULL;

-- Columnas faltantes en Partidos_Futbol
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Partidos_Futbol]') AND name = N'CuotaLocal')
    ALTER TABLE [Partidos_Futbol] ADD [CuotaLocal] DECIMAL(18,2) NULL;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Partidos_Futbol]') AND name = N'CuotaVisitante')
    ALTER TABLE [Partidos_Futbol] ADD [CuotaVisitante] DECIMAL(18,2) NULL;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Partidos_Futbol]') AND name = N'CuotaEmpate')
    ALTER TABLE [Partidos_Futbol] ADD [CuotaEmpate] DECIMAL(18,2) NULL;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Partidos_Futbol]') AND name = N'GolesLocal')
    ALTER TABLE [Partidos_Futbol] ADD [GolesLocal] INT NULL;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Partidos_Futbol]') AND name = N'GolesVisitante')
    ALTER TABLE [Partidos_Futbol] ADD [GolesVisitante] INT NULL;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Partidos_Futbol]') AND name = N'Liga')
    ALTER TABLE [Partidos_Futbol] ADD [Liga] NVARCHAR(MAX) NULL;

-- Columnas ELO por superficie en Equipos
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Equipos]') AND name = N'ELO_Arcilla')
    ALTER TABLE [Equipos] ADD [ELO_Arcilla] INT NULL;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Equipos]') AND name = N'ELO_Pasto')
    ALTER TABLE [Equipos] ADD [ELO_Pasto] INT NULL;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Equipos]') AND name = N'ELO_Dura')
    ALTER TABLE [Equipos] ADD [ELO_Dura] INT NULL;
GO
