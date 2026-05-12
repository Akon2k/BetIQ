# 🚀 BetIQ - Inteligencia Deportiva Cuantitativa

BetIQ es un sistema institucional de análisis deportivo y *Quant Betting*. Su objetivo no es predecir "quién ganará", sino identificar **ineficiencias matemáticas** (Value Bets) en las cuotas ofrecidas por las casas de apuestas a nivel mundial.

---

## 🏀 Arquitectura NBA: Estado = COMPLETADA (Grado Profesional)

El motor de predicción para la NBA se encuentra en una madurez técnica avanzada. Combina modelos estadísticos históricos con información de forma reciente y factores biológicos, procesando cada partido a través de múltiples capas de inteligencia:

### 1. Modelo ELO Histórico Dinámico
La base del modelo. Asigna una fuerza matemática a cada franquicia basada en todo su historial de resultados, ajustando la puntuación después de cada partido según la calidad del oponente vencido.

### 2. Simulación de Monte Carlo (Over/Under & Spreads)
El motor no usa simples promedios. Para cada partido, ejecuta **10,000 iteraciones** simuladas (usando la distribución normal "Box-Muller"). Esto proyecta:
- **Spread (Hándicap):** Diferencia de puntos exacta esperada.
- **Totales:** Total de puntos combinados esperados para el partido.

### 3. Analítica Avanzada (Estado de Forma)
La simulación de Monte Carlo ya no usa promedios estándar de liga (114 pts). Antes de simular, el algoritmo extrae en tiempo real los **últimos 5 partidos** de la base de datos para ambos equipos y calcula:
- **Offensive & Defensive Rating:** Cuántos puntos produce y permite un equipo por cada 100 posesiones.
- **Pace (Ritmo de Juego):** Velocidad a la que juega el equipo (tiros intentados).
El resultado es una proyección que cruza la sabiduría histórica del ELO (40%) con el estado de forma físico reciente (60%).

### 4. Contexto Biológico y Fatiga (Back-to-Back)
El sistema revisa el calendario. Si detecta que un equipo está jugando su segunda noche consecutiva (*Back-to-Back*), el algoritmo inyecta automáticamente una penalización de **-45 puntos de ELO virtual**, emulando el cansancio físico y bajando orgánicamente su probabilidad de victoria.

### 5. Tracking de CLV Automático (Closing Line Value)
La auditoría del modelo es automática. 
- **Apertura:** Al simular temprano, guarda la cuota actual y el EV detectado (`Odds_History`).
- **Cierre:** Un servicio oculto en segundo plano (`ClvTrackerService`) revisa cada 15 minutos la base de datos. Cuando detecta que un partido de NBA está a 30 minutos de empezar, captura la cuota final del mercado.
Esto permite cruzar las apuestas recomendadas por BetIQ contra las cuotas finales de Las Vegas, demostrando matemáticamente la rentabilidad del modelo a largo plazo.

---

## ⏳ Próximos Desarrollos (Roadmap)

Con la NBA operando de manera autónoma, las siguientes fases de desarrollo priorizan la modernización de los demás deportes:

1. **⚽ Fútbol (Poisson Avanzado):** 
   - Transicionar de ELO simple a **Distribución de Poisson**.
   - Objetivo: Predecir marcadores exactos (ej. 2-1) y cálculo matemático de *Over/Under 2.5 Goles*.

2. **🎾 Tenis (Fatiga Acumulada):**
   - Análisis de duración de sets/minutos del partido anterior dentro del mismo torneo para penalizar a jugadores exhaustos.

3. **📊 Dashboard UI:**
   - Panel visual en el Frontend para graficar el éxito del CLV capturado por el servicio de fondo.

4. **⚡ Steam Moves (Dinero Inteligente):**
   - Detección de caídas bruscas de cuotas (Sindicatos apostando) con alertas automáticas.
