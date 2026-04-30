using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using BetIQ.API.Data;
using BetIQ.API.Models;
using BetIQ.API.Services;

namespace BetIQ.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NBAMatchesController : ControllerBase
    {
        private readonly IEloService _eloService;
        private readonly BetIQContext _context;
        private readonly IMemoryCache _cache;

        public NBAMatchesController(IEloService eloService, BetIQContext context, IMemoryCache cache)
        {
            _eloService = eloService;
            _context = context;
            _cache = cache;
        }

        private static readonly Dictionary<string, string> NbaStadiums = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Hawks", "State Farm Arena" }, { "Celtics", "TD Garden" }, { "Nets", "Barclays Center" },
            { "Hornets", "Spectrum Center" }, { "Bulls", "United Center" }, { "Cavaliers", "Rocket Mortgage FieldHouse" },
            { "Mavericks", "American Airlines Center" }, { "Nuggets", "Ball Arena" }, { "Pistons", "Little Caesars Arena" },
            { "Warriors", "Chase Center" }, { "Rockets", "Toyota Center" }, { "Pacers", "Gainbridge Fieldhouse" },
            { "Clippers", "Intuit Dome" }, { "Lakers", "Crypto.com Arena" }, { "Grizzlies", "FedExForum" },
            { "Heat", "Kaseya Center" }, { "Bucks", "Fiserv Forum" }, { "Timberwolves", "Target Center" },
            { "Pelicans", "Smoothie King Center" }, { "Knicks", "Madison Square Garden" }, { "Thunder", "Paycom Center" },
            { "Magic", "Kia Center" }, { "76ers", "Wells Fargo Center" }, { "Suns", "Footprint Center" },
            { "Blazers", "Moda Center" }, { "Kings", "Golden 1 Center" }, { "Spurs", "Frost Bank Center" },
            { "Raptors", "Scotiabank Arena" }, { "Jazz", "Delta Center" }, { "Wizards", "Capital One Arena" }
        };

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetPartidos([FromQuery] DateTime? date)
        {
            string cacheKey = $"Matches_{date?.ToString("yyyyMMdd") ?? "all"}";
            
            if (!_cache.TryGetValue(cacheKey, out List<NBAMatch>? partidos))
            {
                var query = _context.Partidos_NBA
                    .Include(nba => nba.PartidoMaestro)
                    .AsQueryable();

                if (date.HasValue)
                {
                    query = query.Where(p => p.PartidoMaestro.Fecha_Evento.Date == date.Value.Date);
                }

                partidos = await query.OrderBy(p => p.PartidoMaestro.Fecha_Evento).ToListAsync();

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(5))
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(20));

                _cache.Set(cacheKey, partidos, cacheEntryOptions);
            }

            if (partidos == null) return new List<object>();

            // Construir respuesta con el estadio
            var result = partidos.Select(p => new 
            {
                p.ID_Partido,
                p.EquipoLocal,
                p.EquipoVisitante,
                p.PuntosLocal,
                p.PuntosVisitante,
                p.EficienciaOfensivaLocal,
                p.EficienciaDefensivaLocal,
                p.ELOLocal,
                p.ELOVisita,
                p.PromedioPuntosTotal,
                p.CuotaLocal,
                p.CuotaVisitante,
                p.PartidoMaestro,
                Estadio = NbaStadiums.TryGetValue(p.EquipoLocal, out string s) ? s : "Estadio por confirmar"
            }).ToList();

            return Ok(result);
        }

        [HttpPost("registrar")]
        public async Task<IActionResult> RegistrarPartido([FromBody] NbaMatchDto nuevoPartidoDto)
        {
            if (nuevoPartidoDto == null) return BadRequest("Datos del partido inválidos.");

            // 1. Creamos la entidad Maestra
            var partidoMaestro = new PartidoMaestro
            {
                Deporte = "NBA",
                Fecha_Evento = nuevoPartidoDto.FechaEvento,
                Estado = "Programado" // Estado inicial
            };

            // 2. Creamos la entidad de NBA y la vinculamos
            var nuevoPartidoNba = new NBAMatch
            {
                // El ID_Partido será establecido por la relación con PartidoMaestro
                EquipoLocal = nuevoPartidoDto.EquipoLocal,
                EquipoVisitante = nuevoPartidoDto.EquipoVisitante,
                ELOLocal = await _eloService.ObtenerEloActual(nuevoPartidoDto.EquipoLocal, "NBA"),
                ELOVisita = await _eloService.ObtenerEloActual(nuevoPartidoDto.EquipoVisitante, "NBA"),
                EficienciaOfensivaLocal = (decimal)nuevoPartidoDto.EficienciaOfensivaLocal,
                EficienciaDefensivaLocal = (decimal)nuevoPartidoDto.EficienciaDefensivaLocal,
                PromedioPuntosTotal = (decimal)nuevoPartidoDto.PromedioPuntosTotal,
                TrueShootingLocal = (decimal?)nuevoPartidoDto.TrueShootingLocal,
                TrueShootingVisitante = (decimal?)nuevoPartidoDto.TrueShootingVisitante,
                NetRatingLocal = (decimal?)nuevoPartidoDto.NetRatingLocal,
                NetRatingVisitante = (decimal?)nuevoPartidoDto.NetRatingVisitante,
                PartidoMaestro = partidoMaestro // Vinculamos al maestro
            };
            
            // 3. Añadimos el nuevo partido al contexto y guardamos
            // Como NBAMatch tiene una referencia a PartidoMaestro, EF Core se encarga de insertar ambos
            _context.Partidos_NBA.Add(nuevoPartidoNba);
            await _context.SaveChangesAsync();

            // Invalidamos el caché para esta fecha y para la lista general
            string cacheKeyDate = $"Matches_{nuevoPartidoDto.FechaEvento:yyyyMMdd}";
            _cache.Remove(cacheKeyDate);
            _cache.Remove("Matches_all");
            _cache.Remove($"Matches_{nuevoPartidoDto.EquipoLocal}");
            _cache.Remove($"Matches_{nuevoPartidoDto.EquipoVisitante}");
            _cache.Remove("AllTeams");

            // 4. Devolvemos la respuesta que el script de Python/Powershell espera
            var respuesta = new 
            {
                mensaje = "Partido registrado con éxito en BetIQ API",
                idPartido = nuevoPartidoNba.ID_Partido,
                eloLocalAsignado = nuevoPartidoNba.ELOLocal,
                eloVisitaAsignado = nuevoPartidoNba.ELOVisita
            };

            return Ok(respuesta);
        }

        [HttpPut("{id}/resultado")]
        public async Task<IActionResult> RegistrarResultado(int id, [FromBody] NbaMatchResultDto resultado)
        {
            if (resultado == null)
            {
                return BadRequest("El resultado proporcionado es inválido.");
            }

            // Buscamos el partido de NBA junto con su entidad maestra
            var partidoNba = await _context.Partidos_NBA
                .Include(p => p.PartidoMaestro)
                .FirstOrDefaultAsync(p => p.ID_Partido == id);

            if (partidoNba == null)
            {
                return NotFound($"No se encontró un partido con el ID {id}.");
            }

            // Actualizamos los datos del resultado
            partidoNba.PuntosLocal = resultado.PuntosLocal;
            partidoNba.PuntosVisitante = resultado.PuntosVisitante;
            partidoNba.PartidoMaestro.Estado = "Finalizado";
            
            // Actualizamos los ELOs de los equipos implicados
            await _eloService.ActualizarElo(partidoNba);

            // Guardamos todos los cambios en la base de datos
            await _context.SaveChangesAsync();

            // Invalidamos los cachés relevantes
            string cacheKeyDate = $"Matches_{partidoNba.PartidoMaestro.Fecha_Evento:yyyyMMdd}";
            _cache.Remove(cacheKeyDate);
            _cache.Remove("Matches_all");
            _cache.Remove($"Matches_{partidoNba.EquipoLocal}");
            _cache.Remove($"Matches_{partidoNba.EquipoVisitante}");
            _cache.Remove("AllTeams");

            return Ok(new { mensaje = $"Resultado del partido {id} actualizado y ELO recalculado." });
        }
        [HttpPost("odds/batch")]
        public async Task<IActionResult> InsertarCuotasLote([FromBody] List<NbaOddsBatchDto> oddsBatch)
        {
            if (oddsBatch == null || !oddsBatch.Any())
                return BadRequest("Lote de cuotas vacío.");

            int actualizados = 0;

            foreach (var odd in oddsBatch)
            {
                if (!DateTime.TryParseExact(odd.FechaEventoString, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
                    continue;

                // Buscamos el partido basándonos en local, visitante y fecha
                var partido = await _context.Partidos_NBA
                    .Include(p => p.PartidoMaestro)
                    .FirstOrDefaultAsync(p => 
                        p.EquipoLocal == odd.EquipoLocal && 
                        p.EquipoVisitante == odd.EquipoVisitante &&
                        p.PartidoMaestro.Fecha_Evento.Date == parsedDate.Date);

                if (partido != null)
                {
                    partido.CuotaLocal = (decimal)odd.CuotaPromedioLocal;
                    partido.CuotaVisitante = (decimal)odd.CuotaPromedioVisita;
                    actualizados++;
                }
                else
                {
                    // ¡Auto-crear el partido si no existe!
                    var pm = new PartidoMaestro
                    {
                        Deporte = "NBA",
                        Fecha_Evento = parsedDate,
                        Estado = "Programado"
                    };
                    
                    var nuevoNba = new NBAMatch
                    {
                        EquipoLocal = odd.EquipoLocal,
                        EquipoVisitante = odd.EquipoVisitante,
                        ELOLocal = await _eloService.ObtenerEloActual(odd.EquipoLocal, "NBA"),
                        ELOVisita = await _eloService.ObtenerEloActual(odd.EquipoVisitante, "NBA"),
                        CuotaLocal = (decimal)odd.CuotaPromedioLocal,
                        CuotaVisitante = (decimal)odd.CuotaPromedioVisita,
                        PartidoMaestro = pm
                    };
                    
                    _context.Partidos_NBA.Add(nuevoNba);
                    actualizados++;
                }
            }

            await _context.SaveChangesAsync();

            // Limpiamos cache principal de partidos, pero no todas para optimizar
            _cache.Remove("Matches_all");
            
            return Ok(new { mensaje = $"Se actualizaron cuotas para {actualizados} partidos." });
        }
    }
}
