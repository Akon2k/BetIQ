using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BetIQ.API.Data;
using BetIQ.API.Models;
using BetIQ.API.Services;
using Microsoft.Extensions.Logging;

namespace BetIQ.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TeamsController : ControllerBase
    {
        private readonly BetIQContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<TeamsController> _logger;
        private readonly IEloService _eloService;

        public TeamsController(BetIQContext context, IMemoryCache cache, ILogger<TeamsController> logger, IEloService eloService)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
            _eloService = eloService;
        }

        // Diccionario estático para mapear los equipos de la NBA a sus estadios y conferencias
        private static readonly Dictionary<string, (string Estadio, string Conferencia)> NbaTeamsInfo = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            { "Hawks", ("State Farm Arena", "Este") }, { "Celtics", ("TD Garden", "Este") }, { "Nets", ("Barclays Center", "Este") },
            { "Hornets", ("Spectrum Center", "Este") }, { "Bulls", ("United Center", "Este") }, { "Cavaliers", ("Rocket Mortgage FieldHouse", "Este") },
            { "Heat", ("Kaseya Center", "Este") }, { "Bucks", ("Fiserv Forum", "Este") }, { "Knicks", ("Madison Square Garden", "Este") },
            { "Magic", ("Kia Center", "Este") }, { "76ers", ("Wells Fargo Center", "Este") }, { "Pacers", ("Gainbridge Fieldhouse", "Este") },
            { "Raptors", ("Scotiabank Arena", "Este") }, { "Wizards", ("Capital One Arena", "Este") }, { "Pistons", ("Little Caesars Arena", "Este") },
            
            { "Mavericks", ("American Airlines Center", "Oeste") }, { "Nuggets", ("Ball Arena", "Oeste") }, { "Warriors", ("Chase Center", "Oeste") },
            { "Rockets", ("Toyota Center", "Oeste") }, { "Clippers", ("Intuit Dome", "Oeste") }, { "Lakers", ("Crypto.com Arena", "Oeste") },
            { "Grizzlies", ("FedExForum", "Oeste") }, { "Timberwolves", ("Target Center", "Oeste") }, { "Pelicans", ("Smoothie King Center", "Oeste") },
            { "Thunder", ("Paycom Center", "Oeste") }, { "Suns", ("Footprint Center", "Oeste") }, { "Blazers", ("Moda Center", "Oeste") },
            { "Kings", ("Golden 1 Center", "Oeste") }, { "Spurs", ("Frost Bank Center", "Oeste") }, { "Jazz", ("Delta Center", "Oeste") }
        };

        // GET: api/teams
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Equipo>>> GetTeams()
        {
            const string cacheKey = "AllTeams";
            if (!_cache.TryGetValue(cacheKey, out List<Equipo>? teams))
            {
                // Si no está en caché, la obtenemos de la BD
                teams = await _context.Equipos.OrderBy(e => e.NombreEquipo).ToListAsync();

                // Configuramos las opciones del caché
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(5)) // Expira si no se accede en 5 min
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(20)); // Expira después de 20 min

                // Guardamos la lista en el caché
                _cache.Set(cacheKey, teams, cacheEntryOptions);
            }

            return teams ?? new List<Equipo>();
        }

        // GET: api/teams/{teamId}/matches
        [HttpGet("{teamId}/matches")]
        public async Task<ActionResult<IEnumerable<NBAMatch>>> GetTeamMatches(string teamId)
        {
            string cacheKey = $"Matches_{teamId}";
            if (!_cache.TryGetValue(cacheKey, out List<NBAMatch>? matches))
            {
                var teamExists = await _context.Equipos.AnyAsync(e => e.NombreEquipo == teamId);
                if (!teamExists)
                {
                    return NotFound($"No se encontró un equipo con el ID '{teamId}'.");
                }

                matches = await _context.Partidos_NBA
                    .Include(p => p.PartidoMaestro)
                    .Where(p => p.EquipoLocal == teamId || p.EquipoVisitante == teamId)
                    .OrderByDescending(p => p.PartidoMaestro.Fecha_Evento)
                    .ToListAsync();
                
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(5))
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(20));

                _cache.Set(cacheKey, matches, cacheEntryOptions);
            }

            return matches ?? new List<NBAMatch>();
        }

        // GET: api/teams/{teamLocal}/probability-vs/{teamVisitante}?sport=NBA&superficie=Arcilla
        [HttpGet("{teamLocal}/probability-vs/{teamVisitante}")]
        public async Task<ActionResult<object>> GetProbability(string teamLocal, string teamVisitante, [FromQuery] string sport = "NBA", [FromQuery] string? superficie = null)
        {
            var eloService = _eloService;
            
            int eloLocal = await eloService.ObtenerEloActual(teamLocal, sport, superficie);
            int eloVisitante = await eloService.ObtenerEloActual(teamVisitante, sport, superficie);

            double probLocal = eloService.CalcularProbabilidadVictoria(eloLocal, eloVisitante, true);
            double probVisitante = eloService.CalcularProbabilidadVictoria(eloVisitante, eloLocal, false);

            return Ok(new
            {
                Local = new { Equipo = teamLocal, Elo = eloLocal, ProbabilidadVictoria = Math.Round(probLocal * 100, 2) + "%" },
                Visitante = new { Equipo = teamVisitante, Elo = eloVisitante, ProbabilidadVictoria = Math.Round(probVisitante * 100, 2) + "%" }
            });
        }

        // GET: api/teams/{teamLocal}/ev-vs/{teamVisitante}?cuotaLocal=X&cuotaVisita=Y&sport=NBA&superficie=Arcilla
        [HttpGet("{teamLocal}/ev-vs/{teamVisitante}")]
        public async Task<ActionResult<object>> GetExpectedValue(string teamLocal, string teamVisitante, [FromQuery] double cuotaLocal = 1.0, [FromQuery] double cuotaVisita = 1.0, [FromQuery] string sport = "NBA", [FromQuery] string? superficie = null)
        {
            var eloService = _eloService;
            
            int eloLocal = await eloService.ObtenerEloActual(teamLocal, sport, superficie);
            int eloVisitante = await eloService.ObtenerEloActual(teamVisitante, sport, superficie);

            double probLocal = eloService.CalcularProbabilidadVictoria(eloLocal, eloVisitante, true);
            double probVisitante = eloService.CalcularProbabilidadVictoria(eloVisitante, eloLocal, false);

            double evLocal = eloService.CalcularEV(probLocal, cuotaLocal);
            double evVisitante = eloService.CalcularEV(probVisitante, cuotaVisita);
            
            double kellyLocal = eloService.CalcularPorcentajeKelly(probLocal, cuotaLocal);
            double kellyVisitante = eloService.CalcularPorcentajeKelly(probVisitante, cuotaVisita);

            return Ok(new
            {
                Local = new 
                { 
                    Equipo = teamLocal, 
                    Elo = eloLocal, 
                    ProbabilidadVictoria = probLocal, 
                    CuotaIngresada = cuotaLocal, 
                    ExpectedValue = Math.Round(evLocal, 4),
                    IsValueBet = evLocal > 0,
                    PorcentajeKelly = Math.Round(kellyLocal, 4)
                },
                Visitante = new 
                { 
                    Equipo = teamVisitante, 
                    Elo = eloVisitante, 
                    ProbabilidadVictoria = probVisitante, 
                    CuotaIngresada = cuotaVisita, 
                    ExpectedValue = Math.Round(evVisitante, 4),
                    IsValueBet = evVisitante > 0,
                    PorcentajeKelly = Math.Round(kellyVisitante, 4)
                }
            });
        }

        // GET: api/teams/standings
        [HttpGet("standings")]
        public async Task<ActionResult<object>> GetStandings()
        {
            string cacheKey = "NBA_Standings";
            if (_cache.TryGetValue(cacheKey, out object? cachedStandings))
            {
                _logger.LogInformation("Retornando Standings desde el caché.");
                return Ok(cachedStandings);
            }

            _logger.LogInformation("Cache miss para Standings. Calculando desde la base de datos...");

            // Traer todos los partidos finalizados
            var partidosFinalizados = await _context.Partidos_NBA
                .Include(p => p.PartidoMaestro)
                .Where(p => p.PartidoMaestro.Estado == "Finalizado" && p.PuntosLocal != null && p.PuntosVisitante != null)
                .ToListAsync();

            // Diccionario para acumular las estadísticas por equipo
            var stats = new Dictionary<string, TeamStats>();

            foreach (var p in partidosFinalizados)
            {
                // Asegurarse de que ambos equipos existan en el diccionario
                if (!stats.ContainsKey(p.EquipoLocal)) stats[p.EquipoLocal] = new TeamStats { Equipo = p.EquipoLocal };
                if (!stats.ContainsKey(p.EquipoVisitante)) stats[p.EquipoVisitante] = new TeamStats { Equipo = p.EquipoVisitante };

                var local = stats[p.EquipoLocal];
                var visita = stats[p.EquipoVisitante];

                local.PartidosJugados++;
                visita.PartidosJugados++;

                local.PuntosFavor += p.PuntosLocal!.Value;
                local.PuntosContra += p.PuntosVisitante!.Value;

                visita.PuntosFavor += p.PuntosVisitante.Value;
                visita.PuntosContra += p.PuntosLocal.Value;

                if (p.PuntosLocal > p.PuntosVisitante)
                {
                    local.Victorias++;
                    visita.Derrotas++;
                }
                else if (p.PuntosVisitante > p.PuntosLocal)
                {
                    visita.Victorias++;
                    local.Derrotas++;
                }
            }

            // Dar formato de salida y separar por conferencia
            var standingsList = stats.Values.Select(s => new
            {
                Equipo = s.Equipo,
                Conferencia = NbaTeamsInfo.TryGetValue(s.Equipo, out var info) ? info.Conferencia : "Desconocida",
                PJ = s.PartidosJugados,
                V = s.Victorias,
                D = s.Derrotas,
                PCT = s.PartidosJugados > 0 ? Math.Round((double)s.Victorias / s.PartidosJugados, 3) : 0,
                PF = s.PuntosFavor,
                PC = s.PuntosContra,
                DIF = s.PuntosFavor - s.PuntosContra
            }).ToList();

            var conferenciaEste = standingsList
                .Where(s => s.Conferencia == "Este")
                .OrderByDescending(s => s.PCT)
                .ThenByDescending(s => s.DIF)
                .Select((s, index) => new { Posicion = index + 1, s.Equipo, s.PJ, s.V, s.D, s.PCT, s.PF, s.PC, s.DIF })
                .ToList();

            var conferenciaOeste = standingsList
                .Where(s => s.Conferencia == "Oeste")
                .OrderByDescending(s => s.PCT)
                .ThenByDescending(s => s.DIF)
                .Select((s, index) => new { Posicion = index + 1, s.Equipo, s.PJ, s.V, s.D, s.PCT, s.PF, s.PC, s.DIF })
                .ToList();

            var result = new { Este = conferenciaEste, Oeste = conferenciaOeste };

            _cache.Set(cacheKey, result, new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(30))); // Caché más largo para posiciones

            return Ok(result);
        }

        private class TeamStats
        {
            public string Equipo { get; set; } = "";
            public int PartidosJugados { get; set; }
            public int Victorias { get; set; }
            public int Derrotas { get; set; }
            public int PuntosFavor { get; set; }
            public int PuntosContra { get; set; }
        }

        [HttpGet("/api/analysis/value-bets")]
        public async Task<ActionResult<object>> GetValueBets([FromQuery] DateTime? date, [FromQuery] string sport = "NBA")
        {
            var eloService = _eloService;
            var resultados = new List<object>();

            try
            {
                if (sport == "NBA")
                {
                    var query = _context.Partidos_NBA
                        .Include(p => p.PartidoMaestro)
                        .Where(p => p.CuotaLocal != null && p.CuotaVisitante != null && p.PartidoMaestro.Estado != "Finalizado");

                    if (date.HasValue) query = query.Where(p => p.PartidoMaestro.Fecha_Evento.Date == date.Value.Date);

                    var partidos = await query.ToListAsync();
                    foreach (var p in partidos)
                    {
                        ProcesarValueBet(resultados, eloService, p.ID_Partido, p.EquipoLocal, p.EquipoVisitante, 
                            p.PartidoMaestro.Fecha_Evento, p.ELOLocal, p.ELOVisita, (double)p.CuotaLocal!, (double)p.CuotaVisitante!, sport);
                    }
                }
                else if (sport == "Futbol")
                {
                    var query = _context.Partidos_Futbol
                        .Include(p => p.PartidoMaestro)
                        .Where(p => p.CuotaLocal != null && p.CuotaVisitante != null && p.PartidoMaestro.Estado != "Finalizado");

                    if (date.HasValue) query = query.Where(p => p.PartidoMaestro.Fecha_Evento.Date == date.Value.Date);

                    var partidos = await query.ToListAsync();
                    foreach (var p in partidos)
                    {
                        int eloLocal = await eloService.ObtenerEloActual(p.EquipoLocal, "Futbol");
                        int eloVisita = await eloService.ObtenerEloActual(p.EquipoVisitante, "Futbol");
                        ProcesarValueBet(resultados, eloService, p.ID_Partido, p.EquipoLocal, p.EquipoVisitante, 
                            p.PartidoMaestro.Fecha_Evento, eloLocal, eloVisita, (double)p.CuotaLocal!, (double)p.CuotaVisitante!, sport);
                    }
                }
                else if (sport == "Tenis")
                {
                    var query = _context.Partidos_Tenis
                        .Include(p => p.PartidoMaestro)
                        .Where(p => p.CuotaJ1 != null && p.CuotaJ2 != null && p.PartidoMaestro.Estado != "Finalizado");

                    if (date.HasValue) query = query.Where(p => p.PartidoMaestro.Fecha_Evento.Date == date.Value.Date);

                    var partidos = await query.ToListAsync();
                    foreach (var p in partidos)
                    {
                        int eloLocal = (int)(p.EloJugador1 ?? 1500);
                        int eloVisita = (int)(p.EloJugador2 ?? 1500);
                        ProcesarValueBet(resultados, eloService, p.ID_Partido, p.Jugador1, p.Jugador2, 
                            p.PartidoMaestro.Fecha_Evento, eloLocal, eloVisita, (double)p.CuotaJ1!, (double)p.CuotaJ2!, sport);
                    }
                }

                _logger.LogInformation("Análisis de Value Bets completado para {Sport}. Oportunidades: {Count}", sport, resultados.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al calcular Value Bets para {Sport}", sport);
            }

            return Ok(new 
            {
                Analisis_Para = date?.ToString("yyyy-MM-dd") ?? "Todos los disponibles",
                Deporte = sport,
                Total_ValueBets_Encontradas = resultados.Count,
                Oportunidades = resultados
            });
        }

        private void ProcesarValueBet(List<object> resultados, IEloService eloService, int idPartido, string local, string visita, DateTime fecha, int eloLocal, int eloVisita, double cuotaLocal, double cuotaVisita, string sport)
        {
            // For now, assume home advantage applies universally if true, maybe customize per sport later
            double probLocal = eloService.CalcularProbabilidadVictoria(eloLocal, eloVisita, true);
            double probVisita = eloService.CalcularProbabilidadVictoria(eloVisita, eloLocal, false);

            double evLocal = eloService.CalcularEV(probLocal, cuotaLocal);
            double evVisita = eloService.CalcularEV(probVisita, cuotaVisita);

            double kellyLocal = eloService.CalcularPorcentajeKelly(probLocal, cuotaLocal);
            double kellyVisita = eloService.CalcularPorcentajeKelly(probVisita, cuotaVisita);

            string estadio = sport;
            if (sport == "NBA" && NbaTeamsInfo.TryGetValue(local, out var info)) estadio = info.Estadio;

            resultados.Add(new
            {
                PartidoId = idPartido, Fecha = fecha, Estadio = estadio,
                Local = new { Equipo = local, Probabilidad_Real = Math.Round(probLocal * 100, 2) + "%", Cuota_Mercado = cuotaLocal, EV = Math.Round(evLocal, 4), Sugerencia_Kelly = (kellyLocal > 0 ? Math.Round(kellyLocal * 100, 2) + "% del Bankroll" : ""), ValueBet = evLocal > 0 },
                Visitante = new { Equipo = visita, Probabilidad_Real = Math.Round(probVisita * 100, 2) + "%", Cuota_Mercado = cuotaVisita, EV = Math.Round(evVisita, 4), Sugerencia_Kelly = (kellyVisita > 0 ? Math.Round(kellyVisita * 100, 2) + "% del Bankroll" : ""), ValueBet = evVisita > 0 }
            });
        }
    }
}
