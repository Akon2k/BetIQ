-- =================================================================
-- Script para la creación de la Base de Datos de BetIQ
-- Versión: 2.0
-- Descripción: Implementa un modelo de datos normalizado con una
--              tabla maestra de partidos para soportar múltiples
--              deportes (Fútbol, Tenis, NBA).
-- Fecha de Actualización: 2026-03-07
-- =================================================================

-- == Limpieza de entorno: Eliminar tablas existentes para empezar de cero ==
-- Se eliminan en orden inverso a su creación para respetar las dependencias
IF OBJECT_ID('dbo.Cuotas_Bet', 'U') IS NOT NULL DROP TABLE dbo.Cuotas_Bet;
IF OBJECT_ID('dbo.Partidos_Futbol', 'U') IS NOT NULL DROP TABLE dbo.Partidos_Futbol;
IF OBJECT_ID('dbo.Partidos_Tenis', 'U') IS NOT NULL DROP TABLE dbo.Partidos_Tenis;
IF OBJECT_ID('dbo.Partidos_NBA', 'U') IS NOT NULL DROP TABLE dbo.Partidos_NBA;
IF OBJECT_ID('dbo.Equipos', 'U') IS NOT NULL DROP TABLE dbo.Equipos;
IF OBJECT_ID('dbo.Partidos_Maestro', 'U') IS NOT NULL DROP TABLE dbo.Partidos_Maestro;

GO

-- 1. Tabla Maestra: El origen de todos los IDs de partidos
-- Esta tabla centraliza todos los eventos y asigna un ID único.
PRINT 'Creando Tabla Maestra: Partidos_Maestro...';
CREATE TABLE dbo.Partidos_Maestro (
    ID_Partido INT PRIMARY KEY IDENTITY(1,1),
    Deporte VARCHAR(20) NOT NULL, -- 'Futbol', 'Tenis', 'NBA'
    Fecha_Evento DATETIME NOT NULL,
    Estado VARCHAR(20) DEFAULT 'Programado', -- 'Programado', 'En Vivo', 'Finalizado'
    CONSTRAINT CHK_Deporte CHECK (Deporte IN ('Futbol', 'Tenis', 'NBA')),
    CONSTRAINT CHK_Estado CHECK (Estado IN ('Programado', 'En Vivo', 'Finalizado', 'Pospuesto'))
);
GO

-- 2. Tabla de Equipos: Fuente de verdad para Ratings ELO
-- Almacena el ELO actual para cualquier equipo o jugador en cualquier deporte.
PRINT 'Creando Tabla de Equipos...';
CREATE TABLE dbo.Equipos (
    Nombre_Equipo VARCHAR(100) PRIMARY KEY,
    ELO_Actual INT NOT NULL DEFAULT 1500,
    Deporte VARCHAR(20) NOT NULL -- 'Futbol', 'Tenis', 'NBA', etc.
);
GO

-- 3. Tabla de Fútbol: Con métricas para modelo de Poisson
-- Almacena datos específicos del fútbol, vinculados a la tabla maestra.
PRINT 'Creando Tabla de Fútbol: Partidos_Futbol...';
CREATE TABLE dbo.Partidos_Futbol (
    ID_Partido INT PRIMARY KEY,
    Equipo_Local VARCHAR(100) NOT NULL,
    Equipo_Visitante VARCHAR(100) NOT NULL,
    Fuerza_Ataque_Local DECIMAL(5,2) NULL,
    Fuerza_Defensa_Local DECIMAL(5,2) NULL,
    Fuerza_Ataque_Visita DECIMAL(5,2) NULL,
    Fuerza_Defensa_Visita DECIMAL(5,2) NULL,
    CONSTRAINT FK_Futbol_Maestro FOREIGN KEY (ID_Partido) REFERENCES dbo.Partidos_Maestro(ID_Partido) ON DELETE CASCADE,
    CONSTRAINT FK_Futbol_Equipo_Local FOREIGN KEY (Equipo_Local) REFERENCES dbo.Equipos(Nombre_Equipo),
    CONSTRAINT FK_Futbol_Equipo_Visitante FOREIGN KEY (Equipo_Visitante) REFERENCES dbo.Equipos(Nombre_Equipo)
);
GO

-- 4. Tabla de Tenis: Con ELO y Superficie
-- Almacena datos específicos del tenis.
PRINT 'Creando Tabla de Tenis: Partidos_Tenis...';
CREATE TABLE dbo.Partidos_Tenis (
    ID_Partido INT PRIMARY KEY,
    Jugador_1 VARCHAR(100) NOT NULL,
    Jugador_2 VARCHAR(100) NOT NULL,
    Superficie VARCHAR(20) NULL, -- 'Arcilla', 'Pasto', 'Dura'
    ELO_Jugador_1 INT NULL,
    ELO_Jugador_2 INT NULL,
    Torneo VARCHAR(100) NULL,
    CONSTRAINT FK_Tenis_Maestro FOREIGN KEY (ID_Partido) REFERENCES dbo.Partidos_Maestro(ID_Partido) ON DELETE CASCADE,
    CONSTRAINT FK_Tenis_Jugador_1 FOREIGN KEY (Jugador_1) REFERENCES dbo.Equipos(Nombre_Equipo),
    CONSTRAINT FK_Tenis_Jugador_2 FOREIGN KEY (Jugador_2) REFERENCES dbo.Equipos(Nombre_Equipo)
);
GO

-- 5. Tabla de NBA: Con Eficiencias y ELO
-- Almacena datos específicos de baloncesto (NBA).
PRINT 'Creando Tabla de NBA: Partidos_NBA...';
CREATE TABLE dbo.Partidos_NBA (
    ID_Partido INT PRIMARY KEY,
    Equipo_Local VARCHAR(50) NOT NULL,
    Equipo_Visitante VARCHAR(50) NOT NULL,
    Puntos_Local INT NULL,
    Puntos_Visitante INT NULL,
    Asistencias_Local INT NULL,
    Asistencias_Visitante INT NULL,
    Rebotes_Local INT NULL,
    Rebotes_Visitante INT NULL,
    Porcentaje_Tiros_Campo_Local DECIMAL(5,2) NULL,
    Porcentaje_Tiros_Campo_Visitante DECIMAL(5,2) NULL,
    Porcentaje_Tiros_Libres_Local DECIMAL(5,2) NULL,
    Porcentaje_Tiros_Libres_Visitante DECIMAL(5,2) NULL,
    Porcentaje_Tiros_Tres_Local DECIMAL(5,2) NULL,
    Porcentaje_Tiros_Tres_Visitante DECIMAL(5,2) NULL,
    Eficiencia_Ofensiva_Local DECIMAL(5,2) NULL,
    Eficiencia_Defensiva_Local DECIMAL(5,2) NULL,
    ELO_Local INT NULL,
    ELO_Visita INT NULL,
    Promedio_Puntos_Total DECIMAL(6,2) NULL,
    CONSTRAINT FK_NBA_Maestro FOREIGN KEY (ID_Partido) REFERENCES dbo.Partidos_Maestro(ID_Partido) ON DELETE CASCADE
    -- No se añade FK a Equipos aquí para mantener la flexibilidad de 
    -- registrar partidos incluso si los equipos no están en la tabla Equipos.
    -- La lógica de la aplicación se encargará de sincronizarlos.
);
GO

-- 6. Tabla de Cuotas Bet: Relacionada por el ID único
-- Centraliza todas las cuotas, vinculadas directamente al partido en la tabla maestra.
PRINT 'Creando Tabla de Cuotas: Cuotas_Bet...';
CREATE TABLE dbo.Cuotas_Bet (
    ID_Cuota INT PRIMARY KEY IDENTITY(1,1),
    ID_Partido INT NOT NULL,
    Cuota_Local_1 DECIMAL(5,2) NULL,
    Cuota_Empate_X DECIMAL(5,2) NULL, -- Puede ser NULL para deportes sin empate (Tenis, NBA)
    Cuota_Visita_2 DECIMAL(5,2) NULL,
    Ultima_Actualizacion DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_Cuotas_Maestro FOREIGN KEY (ID_Partido) REFERENCES dbo.Partidos_Maestro(ID_Partido) ON DELETE CASCADE
);
GO

PRINT '======== Creación de la Base de Datos completada exitosamente. ========';
GO
