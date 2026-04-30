# BetIQ - Motor de Análisis e Inteligencia Deportiva

BetIQ es una plataforma de análisis deportivo enfocada en la **NBA**, que combina ratings Elo, cálculo de Expected Value (EV+) y criterio de Kelly para apoyar la toma de decisiones de apuestas con base matemática. El sistema está completamente funcional con backend .NET, base de datos SQL Server y un dashboard web en tiempo real.

---

## Tecnologías Utilizadas

| Capa | Tecnología |
|------|-----------|
| **API Backend** | .NET 10 / ASP.NET Core / C# |
| **ORM** | Entity Framework Core 10 |
| **Base de Datos** | SQL Server (local) |
| **API Docs** | Swagger / OpenAPI |
| **Frontend** | HTML5 / Vanilla CSS / JavaScript (ES6+) |
| **Scripts de Datos** | Python 3 (Scraper ESPN + The Odds API) |
| **Testing** | xUnit + Moq + EF InMemory |
| **Caché** | IMemoryCache (.NET) |

---

## 🏗️ Arquitectura y Archivos

El sistema sigue una arquitectura modular separada en Backend (.NET), Frontend (JS/CSS) y Scripts de Automatización (Python).

Para una explicación detallada de cada archivo y la estructura del proyecto, consulta:

👉 [**Guía Detallada de Arquitectura (ARCHITECTURE.md)**](ARCHITECTURE.md)

---

---

## Base de Datos

El esquema (`schema.sql`) implementa un modelo normalizado para soportar múltiples deportes. Puerto de conexión por defecto: SQL Server local.

### Tablas Principales

| Tabla | Descripción |
|-------|-------------|
| `Partidos_Maestro` | Tabla central: ID único para cada evento deportivo (NBA, Fútbol, Tenis) |
| `Partidos_NBA` | Datos específicos NBA: puntos, ELO, cuotas, eficiencias por partido |
| `Equipos` | Fuente de verdad del rating ELO actual de cada equipo |
| `Partidos_Futbol` | Datos de fútbol con métricas para modelo de Poisson (extensible) |
| `Partidos_Tenis` | Datos de tenis con ELO y superficie (extensible) |
| `Cuotas_Bet` | Cuotas de casa de apuestas vinculadas a cualquier partido |

---

## Endpoints de la API

### `NBAMatchesController` — `/api/nbamatches`

| Método | Ruta | Descripción |
|--------|------|-------------|
| `GET` | `/api/nbamatches` | Lista todos los partidos (filtrable por `?date=YYYY-MM-DD`) |
| `POST` | `/api/nbamatches/registrar` | Registra un nuevo partido programado (asigna ELO automáticamente) |
| `PUT` | `/api/nbamatches/{id}/resultado` | Registra el resultado final y recalcula ELO de ambos equipos |
| `POST` | `/api/nbamatches/odds/batch` | Actualiza cuotas en lote desde The Odds API |

### `TeamsController` — `/api/teams`

| Método | Ruta | Descripción |
|--------|------|-------------|
| `GET` | `/api/teams` | Lista todos los equipos con su ELO actual |
| `GET` | `/api/teams/{id}/matches` | Historial de partidos de un equipo |
| `GET` | `/api/teams/{local}/probability-vs/{visita}` | Probabilidad de victoria (ELO vs ELO) |
| `GET` | `/api/teams/{local}/ev-vs/{visita}` | Expected Value y Kelly Criterion con cuotas del mercado |
| `GET` | `/api/teams/standings` | Tabla de posiciones separada por conferencia Este/Oeste |
| `GET` | `/api/analysis/value-bets` | Detección automática de Value Bets (EV > 0) |

---

## Lógica de Negocio — `EloService`

El `EloService` centraliza toda la matemática predictiva:

- **`CalcularProbabilidadVictoria(eloA, eloB)`** → Fórmula Elo estándar: `1 / (1 + 10^((eloB-eloA)/400))`
- **`ObtenerEloActual(equipo, deporte)`** → Retorna ELO de BD o crea el equipo con ELO 1500
- **`ActualizarElo(partido)`** → Actualiza ELOs post-partido con factor K=32
- **`CalcularEV(probabilidad, cuota)`** → `(prob * cuota) - 1` (EV positivo = value bet)
- **`CalcularPorcentajeKelly(prob, cuota)`** → `f* = (b*p - q) / b` (protege el bankroll)

---

## Dashboard Web

El frontend (`BetIQ.Web`) es un dashboard de página única con modo oscuro y diseño glassmorphism:

- **Calculadora de Probabilidad:** Selecciona Local vs. Visitante y calcula ELO + probabilidad + sugerencia Kelly con Bankroll en CLP (pesos chilenos, `Intl.NumberFormat('es-CL')`).
- **Oportunidades del Mercado (Value Bets):** Cards dinámicas con alertas visuales para EV+ detectado.
- **Partidos del Día:** Lista de partidos con estadio, hora en Horario Chile (America/Santiago) y cuotas.
- **Tablas de Posiciones NBA:** Conferencia Este y Oeste con zonas de Playoff y Play-In destacadas.
- **Ranking ELO:** Top equipos ordenados por rating.

---

## Instalación y Puesta en Marcha

### 1. Prerequisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (Express, Developer o superior)
- Python 3.9+

### 2. Base de Datos

```sql
-- En SQL Server Management Studio (SSMS):
CREATE DATABASE BetIQ_DB;
-- Luego ejecutar:
USE BetIQ_DB;
-- Ejecutar schema.sql completo
-- (Opcional) Ejecutar setup_db.sql para datos de ejemplo
```

### 3. Configurar la API

Editar `BetIQ.API/appsettings.json`:

```json
"ConnectionStrings": {
  "BetIQConnection": "Server=.;Database=BetIQ_DB;Trusted_Connection=True;TrustServerCertificate=True"
}
```

### 4. Ejecutar

**Terminal 1 – API Backend:**
```powershell
cd BetIQ.API
dotnet run
# API disponible en http://localhost:5023
# Swagger en http://localhost:5023/swagger
```

**Terminal 2 – Frontend (servidor local):**
```powershell
cd BetIQ.Web
python -m http.server 8080
# Dashboard en http://localhost:8080
```

**Terminal 3 – Scripts de datos (opcional):**
```powershell
cd BetIQ.Scripts
pip install -r requirements.txt
python sync_espn.py   # Importar partidos del día desde ESPN
python sync_odds.py   # Sincronizar cuotas desde The Odds API
```

### 5. Ejecutar Tests

```powershell
cd # raíz del proyecto
dotnet test --verbosity normal
```

---

## Estado del Proyecto

Ver [**ROADMAP.md**](ROADMAP.md) para el detalle de fases completadas y próximos pasos.

> BetIQ | Inteligencia Deportiva · Localizado para Chile 🏀🇨🇱
