# BetIQ | Guía Detallada de Arquitectura y Archivos 🏗️

Este documento explica la función de cada carpeta y archivo dentro del ecosistema de **BetIQ** para facilitar su mantenimiento y expansión.

---

## 📂 1. Backend (.NET 10 API)
Ubicación: `BetIQ.API/`

Es el núcleo del sistema, encargado de la persistencia de datos, cálculos matemáticos y exposición de servicios vía REST.

### 🎮 Controllers (`/Controllers`)
Los controladores son los puntos de entrada para las peticiones HTTP:
- **`NBAMatchesController.cs`**: Punto central para la NBA. Permite registrar partidos, resultados y actualizar cuotas por lote.
- **`FutbolController.cs`**: Implementa la lógica para el fútbol, incluyendo el cálculo de probabilidades vía Poisson.
- **`TenisController.cs`**: Gestiona los partidos de tenis y el ranking Elo individual de los jugadores.
- **`TeamsController.cs`**: Ofrece información general de equipos, el ranking ELO global y el motor de detección de "Value Bets".

### 🧠 Services (`/Services`)
- **`EloService.cs`**: El motor matemático del proyecto. Aquí residen las fórmulas de:
  - **Elo Rating**: Probabilidad de victoria basada en fuerza relativa.
  - **Distribución de Poisson**: Predicción de marcadores y probabilidades 1X2 para fútbol.
  - **Criterio de Kelly**: Sugerencia de gestión de banca (Money Management).
  - **EV (Expected Value)**: Identificación de apuestas con valor matemático.

### 📦 Models & Data (`/Models`, `/Data`)
- **`Models/`**: Contiene las clases de C# que representan las tablas de la DB y los **DTOs** (Data Transfer Objects) usados para la comunicación con los scripts de Python.
- **`BetIQContext.cs`**: Configuración de Entity Framework Core. Mapea la lógica de C# a las tablas físicas de SQL Server.

---

## 🎨 2. Frontend (Dashboard Web)
Ubicación: `BetIQ.Web/`

Una interfaz moderna diseñada para la toma de decisiones rápida.

- **`index.html`**: SPA (Single Page Application) que organiza los módulos de Calculadora, Radar de Oportunidades, Eventos y Rankings.
- **`css/styles.css`**: Sistema de diseño basado en **Glassmorphism**. Utiliza variables CSS para un tema oscuro profundo con acentos neón y animaciones fluidas.
- **`js/app.js`**: El orquestador del lado del cliente. Maneja:
  - Cambio de contexto entre deportes.
  - Formateo de moneda (CLP).
  - Actualización asíncrona de datos desde la API.

---

## 🐍 3. Automatización y Scripts
Ubicación: `BetIQ.Scripts/`

Herramientas externas (principalmente Python) para la ingesta y mantenimiento de datos.

- **`sync_odds.py`**: Conecta con *The Odds API*. Obtiene cuotas en tiempo real para todos los deportes y las inyecta en la API.
- **`sync_soccer.py`**: Procesa estadísticas de ligas de fútbol para calcular la "Fuerza de Ataque" y "Defensa" necesaria para Poisson.
- **`sync_tennis.py`**: Importa rankings y resultados de torneos de tenis profesionales.
- **`seed_data.ps1` / `.py`**: Generadores de datos simulados para poblar la base de datos durante el desarrollo.

---

## 🗄️ 4. Base de Datos
- **`schema.sql`**: Script DDL (Data Definition Language) que define la estructura de 125+ líneas para soportar multi-deporte con integridad referencial.
- **`BetIQ.db`**: Archivo de base de datos SQLite (si se usa en modo local simple) o referencia a SQL Server según `appsettings.json`.

---

## 📝 5. Documentación de Gestión
- **`README.md`**: Guía rápida de instalación y visión general.
- **`ROADMAP.md`**: Seguimiento detallado de las fases del proyecto (actualmente en Fase 11).

---
*BetIQ | Arquitectura diseñada para la escalabilidad y precisión matemática.*
