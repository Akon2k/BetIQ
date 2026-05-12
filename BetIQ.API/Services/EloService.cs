using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using BetIQ.API.Data;
using BetIQ.API.Models;

namespace BetIQ.API.Services
{
    public interface IEloService
    {
        Task<int> ObtenerEloActual(string equipo, string deporte, string? superficie = null);
        Task ActualizarElo(NBAMatch partidoConResultado);
        Task ActualizarEloTenis(TenisMatch partidoConResultado);
        double CalcularProbabilidadVictoria(int eloEquipoA, int eloEquipoB, bool equipoA_EsLocal = false, decimal? netRatingA = null, decimal? netRatingB = null);
        double CalcularEV(double probabilidadVictoria, double cuotaDecimal);
        double CalcularPorcentajeKelly(double probabilidadVictoria, double cuotaDecimal);
        Task<int> CalcularKFactorDinamico(string nombreEquipo, string deporte = "NBA");
        
        // Fútbol - Modelo de Poisson
        (double ProbLocal, double ProbEmpate, double ProbVisita) CalcularProbabilidadesPoisson(decimal faL, decimal fdL, decimal faV, decimal fdV);
        (int GolesLocal, int GolesVisita, double Probabilidad) ObtenerMarcadorMasProbable(decimal faL, decimal fdL, decimal faV, decimal fdV);
    }

    public class EloService : IEloService
    {
        private readonly BetIQContext _context;
        private const int EloInicial = 1500;
        private const int FactorKDefault = 32;
        private const int HomeAdvantage = 100; // Bono virtual para el equipo local en la NBA

        public EloService(BetIQContext context)
        {
            _context = context;
        }

        public double CalcularProbabilidadVictoria(int eloEquipoA, int eloEquipoB, bool equipoA_EsLocal = false, decimal? netRatingA = null, decimal? netRatingB = null)
        {
            // Si el equipo A es local, le sumamos el bono de ventaja de campo
            double eloVirtualA = equipoA_EsLocal ? eloEquipoA + HomeAdvantage : eloEquipoA;
            
            // Ajuste por Net Rating: Cada punto de Net Rating positivo aporta un bono de ~10 puntos de Elo virtual
            if (netRatingA.HasValue) eloVirtualA += (double)netRatingA.Value * 10;
            double eloVirtualB = eloEquipoB + (netRatingB.HasValue ? (double)netRatingB.Value * 10 : 0);

            return 1.0 / (1.0 + Math.Pow(10, (eloVirtualB - eloVirtualA) / 400.0));
        }

        public double CalcularEV(double probabilidadVictoria, double cuotaDecimal)
        {
            // Fórmula EV (Esperanza Matemática): (Probabilidad * Cuota) - 1
            // Un valor mayor a 0 indica rentabilidad matemática (Value Bet).
            return (probabilidadVictoria * cuotaDecimal) - 1.0;
        }

        public double CalcularPorcentajeKelly(double probabilidadVictoria, double cuotaDecimal)
        {
            // Fórmula de Kelly: f* = (bp - q) / b
            // p = probabilidad de ganar
            // q = probabilidad de perder = (1 - p)
            // b = cuota decimal - 1 (probabilidad implícita de pago)
            
            double p = probabilidadVictoria;
            double q = 1.0 - p;
            double b = cuotaDecimal - 1.0;

            if (b <= 0) return 0.0; // Evitar división por cero o cuotas sin sentido

            double f = ((b * p) - q) / b;

            // Si el resultado es negativo, Kelly sugiere NO apostar
            return f > 0 ? f : 0.0;
        }

        public async Task<int> CalcularKFactorDinamico(string nombreEquipo, string deporte = "NBA")
        {
            if (deporte == "NBA")
            {
                // Obtener los últimos 5 partidos finalizados del equipo
                var ultimosPartidos = await _context.Partidos_NBA
                    .Include(p => p.PartidoMaestro)
                    .Where(p => (p.EquipoLocal == nombreEquipo || p.EquipoVisitante == nombreEquipo) 
                               && p.PartidoMaestro.Estado == "Finalizado")
                    .OrderByDescending(p => p.PartidoMaestro.Fecha_Evento)
                    .Take(5)
                    .ToListAsync();

                if (ultimosPartidos.Count < 3) return FactorKDefault;

                int victorias = 0;
                foreach (var p in ultimosPartidos)
                {
                    bool gano = (p.EquipoLocal == nombreEquipo && p.PuntosLocal > p.PuntosVisitante) ||
                               (p.EquipoVisitante == nombreEquipo && p.PuntosVisitante > p.PuntosLocal);
                    if (gano) victorias++;
                }

                if (victorias >= 4) return 40; 
                if (victorias <= 1) return 40;
            }

            return FactorKDefault;
        }

        // --- FÚTBOL: MODELO DE POISSON ---
        public (int GolesLocal, int GolesVisita, double Probabilidad) ObtenerMarcadorMasProbable(decimal faL, decimal fdV, decimal faV, decimal fdL)
        {
            double lambdaLocal = (double)(faL * fdV);
            double lambdaVisita = (double)(faV * fdL);

            int bestGolesL = 0, bestGolesV = 0;
            double maxProb = 0;

            for (int i = 0; i <= 5; i++)
            {
                double pI = Poisson(i, lambdaLocal);
                for (int j = 0; j <= 5; j++)
                {
                    double pJ = Poisson(j, lambdaVisita);
                    double pFinal = pI * pJ;

                    if (pFinal > maxProb)
                    {
                        maxProb = pFinal;
                        bestGolesL = i;
                        bestGolesV = j;
                    }
                }
            }

            return (bestGolesL, bestGolesV, Math.Round(maxProb, 4));
        }

        public (double ProbLocal, double ProbEmpate, double ProbVisita) CalcularProbabilidadesPoisson(decimal faL, decimal fdV, decimal faV, decimal fdL)
        {
            // Lambda es el promedio esperado de goles
            double lambdaLocal = (double)(faL * fdV);
            double lambdaVisita = (double)(faV * fdL);

            double probLocal = 0, probEmpate = 0, probVisita = 0;

            // Calculamos la matriz de resultados hasta 6 goles por equipo (suficiente para el 99% de los casos)
            for (int i = 0; i <= 6; i++) // Goles Local
            {
                double pI = Poisson(i, lambdaLocal);
                for (int j = 0; j <= 6; j++) // Goles Visita
                {
                    double pJ = Poisson(j, lambdaVisita);
                    double pFinal = pI * pJ;

                    if (i > j) probLocal += pFinal;
                    else if (i < j) probVisita += pFinal;
                    else probEmpate += pFinal;
                }
            }

            return (Math.Round(probLocal, 4), Math.Round(probEmpate, 4), Math.Round(probVisita, 4));
        }

        private double Poisson(int k, double lambda)
        {
            return (Math.Pow(lambda, k) * Math.Exp(-lambda)) / Factorial(k);
        }

        private double Factorial(int n)
        {
            double res = 1;
            for (int i = 2; i <= n; i++) res *= i;
            return res;
        }

        public async Task<int> ObtenerEloActual(string nombreEquipo, string deporte, string? superficie = null)
        {
            var equipo = await _context.Equipos.FindAsync(nombreEquipo);

            if (equipo == null)
            {
                equipo = new Equipo
                {
                    NombreEquipo = nombreEquipo,
                    Deporte = deporte,
                    EloActual = EloInicial,
                    EloArcilla = deporte == "Tenis" ? EloInicial : null,
                    EloPasto = deporte == "Tenis" ? EloInicial : null,
                    EloDura = deporte == "Tenis" ? EloInicial : null
                };
                _context.Equipos.Add(equipo);
                await _context.SaveChangesAsync();
            }

            if (deporte == "Tenis" && !string.IsNullOrEmpty(superficie))
            {
                string sup = superficie.ToLower();
                if (sup.Contains("arcilla") || sup.Contains("clay")) return equipo.EloArcilla ?? equipo.EloActual;
                if (sup.Contains("pasto") || sup.Contains("hierba") || sup.Contains("grass")) return equipo.EloPasto ?? equipo.EloActual;
                if (sup.Contains("dura") || sup.Contains("hard")) return equipo.EloDura ?? equipo.EloActual;
            }

            return equipo.EloActual;
        }

        public async Task ActualizarElo(NBAMatch partido)
        {
            if (partido.PuntosLocal == null || partido.PuntosVisitante == null)
            {
                // No se puede calcular el ELO sin un resultado
                return;
            }

            var equipoLocal = await _context.Equipos.FindAsync(partido.EquipoLocal);
            var equipoVisitante = await _context.Equipos.FindAsync(partido.EquipoVisitante);

            // Si por alguna razón los equipos no existen, no podemos actualizar ELO.
            if (equipoLocal == null || equipoVisitante == null) return;

            // Ratings actuales
            int eloLocal = equipoLocal.EloActual;
            int eloVisitante = equipoVisitante.EloActual;

            // 1. Calcular la probabilidad de victoria esperada para el local (con ventaja de local)
            double probabilidadLocal = CalcularProbabilidadVictoria(eloLocal, eloVisitante, true);
            // La probabilidad del visitante es el complemento para asegurar que sumen 1.0
            double probabilidadVisitante = 1.0 - probabilidadLocal;

            // 2. Determinar el resultado real (Score)
            double scoreLocal, scoreVisitante;
            if (partido.PuntosLocal > partido.PuntosVisitante)
            {
                scoreLocal = 1.0; // Local gana
                scoreVisitante = 0.0; // Visitante pierde
            }
            else
            {
                scoreLocal = 0.0; // Local pierde
                scoreVisitante = 1.0; // Visitante gana
            }
            // Nota: NBA no tiene empates, así que no se maneja el caso 0.5

            // 3. Determinar K-Factors dinámicos para cada equipo
            int kLocal = await CalcularKFactorDinamico(partido.EquipoLocal);
            int kVisitante = await CalcularKFactorDinamico(partido.EquipoVisitante);

            // 4. Calcular el nuevo ELO
            int nuevoEloLocal = (int)Math.Round(eloLocal + kLocal * (scoreLocal - probabilidadLocal));
            int nuevoEloVisitante = (int)Math.Round(eloVisitante + kVisitante * (scoreVisitante - probabilidadVisitante));

            // 4. Actualizar los ratings en la tabla Equipos
            equipoLocal.EloActual = nuevoEloLocal;
            equipoVisitante.EloActual = nuevoEloVisitante;

            _context.Equipos.Update(equipoLocal);
            _context.Equipos.Update(equipoVisitante);
            
            await _context.SaveChangesAsync();
        }

        private int CalcularKFactorTenis(string? torneo)
        {
            if (string.IsNullOrEmpty(torneo)) return FactorKDefault;
            string t = torneo.ToLower();
            if (t.Contains("grand slam")) return 60; // Max factor
            if (t.Contains("masters 1000")) return 45;
            if (t.Contains("atp 500")) return 35;
            if (t.Contains("atp 250")) return 25;
            if (t.Contains("challenger")) return 15;
            return FactorKDefault;
        }

        public async Task ActualizarEloTenis(TenisMatch partido)
        {
            // El resultado en tenis suele ser binario (Gana J1 o Gana J2)
            if (partido.PartidoMaestro == null) await _context.Entry(partido).Reference(p => p.PartidoMaestro).LoadAsync();
            
            if (partido.PartidoMaestro!.Estado != "Finalizado") return;

            var j1 = await _context.Equipos.FindAsync(partido.Jugador1);
            var j2 = await _context.Equipos.FindAsync(partido.Jugador2);

            if (j1 == null || j2 == null) return;

            // Determinar ganador. Por convención del importador, J1 es el ganador.
            // TODO: Leer ResultadoSets para confirmar ganador real (ej: "6-4 6-2" → J1 gana).
            double score1 = 1.0; 
            double score2 = 0.0;

            int elo1 = await ObtenerEloActual(partido.Jugador1, "Tenis", partido.Superficie);
            int elo2 = await ObtenerEloActual(partido.Jugador2, "Tenis", partido.Superficie);

            double prob1 = CalcularProbabilidadVictoria(elo1, elo2, false);
            double prob2 = 1.0 - prob1;

            int k = CalcularKFactorTenis(partido.Torneo);
            
            double delta1 = k * (score1 - prob1);
            double delta2 = k * (score2 - prob2);

            // Actualizar ELO general con el mismo delta
            j1.EloActual = (int)Math.Round(j1.EloActual + delta1);
            j2.EloActual = (int)Math.Round(j2.EloActual + delta2);

            // Actualizar el ELO de la superficie específica si aplica
            if (!string.IsNullOrEmpty(partido.Superficie))
            {
                string sup = partido.Superficie.ToLower();
                if (sup.Contains("arcilla") || sup.Contains("clay"))
                {
                    j1.EloArcilla = (int)Math.Round((j1.EloArcilla ?? EloInicial) + delta1);
                    j2.EloArcilla = (int)Math.Round((j2.EloArcilla ?? EloInicial) + delta2);
                }
                else if (sup.Contains("pasto") || sup.Contains("hierba") || sup.Contains("grass"))
                {
                    j1.EloPasto = (int)Math.Round((j1.EloPasto ?? EloInicial) + delta1);
                    j2.EloPasto = (int)Math.Round((j2.EloPasto ?? EloInicial) + delta2);
                }
                else if (sup.Contains("dura") || sup.Contains("hard"))
                {
                    j1.EloDura = (int)Math.Round((j1.EloDura ?? EloInicial) + delta1);
                    j2.EloDura = (int)Math.Round((j2.EloDura ?? EloInicial) + delta2);
                }
            }

            _context.Equipos.Update(j1);
            _context.Equipos.Update(j2);
            await _context.SaveChangesAsync();
        }

    }
}
