# Roadmap del Proyecto BetIQ

Este documento describe el estado actual y los planes futuros del proyecto BetIQ. Las fases representan los principales hitos de desarrollo.

---

## ✅ Fase 1: Funcionalidad Principal (Completada)

Estructura base del proyecto y primer ciclo de vida de un partido NBA.

- [x] Estructura inicial de la API en .NET con arquitectura por capas (Controllers / Services / Models / Data).
- [x] Creación del esquema de base de datos normalizado (`schema.sql` v2.0) con soporte multi-deporte.
- [x] Endpoint `POST /api/nbamatches/registrar` para ingresar partidos programados.
- [x] `EloService`: asignación de ELO inicial 1500 y persistencia en tabla `Equipos`.
- [x] Script Python `NBA_Data_Pusher.py` para simular envío de datos a la API.

---

## ✅ Fase 2: Cálculo de Elo y Resultados de Partidos (Completada)

Implementación del núcleo matemático del sistema: actualización de ratings Elo post-partido.

- [x] Endpoint `PUT /api/nbamatches/{id}/resultado` para registrar marcadores finales.
- [x] `EloService.ActualizarElo()`: recalcula ratings con Factor K=32 según resultado real vs. esperado.
- [x] Tabla `Partidos_Maestro` con campo `Estado` (`Programado` / `En Vivo` / `Finalizado` / `Pospuesto`).
- [x] Tabla `Partidos_NBA` con campos de puntos, ELO snapshot, eficiencias y cuotas.

---

## ✅ Fase 3: Ingesta y Automatización de Datos (Completada)

Automatización de la obtención de datos reales de la NBA.

- [x] **`sync_espn.py`**: Scraper que extrae partidos del día desde ESPN y los registra en la API.
- [x] **`sync_odds.py`**: Script que sincroniza cuotas de casas de apuestas desde The Odds API.
- [x] Endpoint `POST /api/nbamatches/odds/batch` para actualización masiva de cuotas.
- [x] **`seed_data.py` / `seed_data.ps1`**: Scripts de población de datos para entorno de desarrollo.

---

## ✅ Fase 4: Refinamiento de la API y los Datos (Completada)

Enriquecimiento de los endpoints y optimización de rendimiento.

- [x] `GET /api/teams` — Lista de equipos con ELO actual.
- [x] `GET /api/teams/{id}/matches` — Historial completo de partidos por equipo.
- [x] `GET /api/nbamatches?date=YYYY-MM-DD` — Partidos filtrados por fecha.
- [x] `GET /api/teams/{local}/probability-vs/{visita}` — Probabilidad de victoria ELO vs ELO.
- [x] Sistema de caché en memoria (`IMemoryCache`) con TTL diferenciado por tipo de dato (5-30 min).
- [x] `ExceptionHandlingMiddleware` para manejo global de errores con respuesta JSON uniforme.
- [x] DTOs tipados: `NbaMatchDto`, `NbaMatchResultDto`, `NbaOddsBatchDto`.

---

## 🔄 Fase 5: Calidad y Preparación para Producción (En Progreso)

Estabilidad, trazabilidad y observabilidad del sistema.

- [x] Proyecto `BetIQ.Tests` configurado con xUnit + Moq + EF InMemory.
- [x] 14 tests automatizados: 4 unitarios de EloService, 3 de integración de TeamsController y 7 casos borde.
- [x] Tests para `CalcularEV` y `CalcularPorcentajeKelly` cubriendo casos positivos, negativos y cuotas <= 1.
- [x] Integrar **Serilog** para logging estructurado y coloreado en consola.
- [x] Enriquecer `ExceptionHandlingMiddleware` con path de request, método HTTP y parámetros dinámicos.
- [x] Logs detallados en `TeamsController` (cache hits/misses, conteo de oportunidades en tiempo real).
- [ ] Migrar persistencia de datos a la nueva instancia SQL Server (Instalación SSMS manual).
- [ ] Configurar pipeline CI/CD para compilación y test automatizados.

*(Nota: La Fase 6 fue absorbida por mejoras de UI y UX en fases posteriores).*

---

## ✅ Fase 7: Dashboard Frontend Web (Completada)

Interfaz de usuario web que consume la API en tiempo real.

- [x] Dashboard con diseño **glassmorphism** y modo oscuro premium.
- [x] Calculadora ELO interactiva: selección de equipos, probabilidad de victoria y sugerencia de apuesta.
- [x] Integración completa de JS con todos los endpoints de la API.
- [x] Tabla de Posiciones NBA separada por **Conferencia Este y Oeste**, con zonas Playoff y Play-In.
- [x] Sección "Partidos del Día" con estadio, hora en Horario Chile (America/Santiago).
- [x] Formato monetario en **Pesos Chilenos (CLP)** usando `Intl.NumberFormat('es-CL')`.

---

## ✅ Fase 8: Herramientas de Apuestas — EV+ y Kelly Criterion (Completada)

Motor matemático de apuestas basado en valor esperado.

- [x] `CalcularEV(prob, cuota)` → `(probabilidad * cuota) - 1` en `EloService`.
- [x] `CalcularPorcentajeKelly(prob, cuota)` → `f* = (b*p - q) / b` en `EloService`.
- [x] Endpoint `GET /api/analysis/value-bets` con detección automática de EV > 0.
- [x] Cards de "Oportunidades del Mercado" en el Dashboard con alertas visuales de Value Bet.
- [x] Sugerencia de monto a apostar (Kelly) calculada en tiempo real sobre el Bankroll del usuario.

---

## ✅ Fase 9: Evolución del Modelo Predictivo (Completada)

Mejoras al motor de predicción para mayor precisión.

- [x] Incorporar **home/away advantage** en la fórmula de la NBA.
- [x] Modelo de **Poisson** pro con predicción de marcador exacto para fútbol.
- [x] Factor de **racha** (últimos 5 partidos) para ponderar el K factor dinámico.
- [x] Integrar estadísticas avanzadas (**TS%**, **Net Rating**) para NBA.

---

## ✅ Fase 10: Multi-Deporte (Completada)

Extender el sistema a otros deportes aprovechando el modelo normalizado.

- [x] Activar el módulo de **Tenis** con Elo especializado.
- [x] Activar el módulo de **Fútbol** con modelo de Poisson.
- [x] UI con selector de deporte dinámico en el Dashboard.

---

## ✅ Fase 11: Ingesta Automatizada de Datos (Completada)

Sincronización con APIs en tiempo real para mantener el sistema actualizado.

- [x] **Sincronización Real**: Configuración de `sync_soccer.py` y `sync_tennis.py` con integración a RapidAPI.
- [x] **Reparación de Esquema**: Actualización física de la base de datos (SQLEXPRESS) con columnas para cuotas, goles y sets.
- [x] **Optimización de API**: Refactorización de controladores para asegurar el registro automático de equipos.
- [x] **Verificación UI**: Validación completa de la visualización de probabilidades en el Dashboard.
