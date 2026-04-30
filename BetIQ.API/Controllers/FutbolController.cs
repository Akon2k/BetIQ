using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BetIQ.API.Data;
using BetIQ.API.Models;
using BetIQ.API.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BetIQ.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FutbolController : ControllerBase
    {
        private readonly BetIQContext _context;
        private readonly IEloService _eloService;

        public FutbolController(BetIQContext context, IEloService eloService)
        {
            _context = context;
            _eloService = eloService;
        }

        [HttpGet("ligas")]
        public async Task<ActionResult<IEnumerable<string>>> GetLigas()
        {
            // Devuelve todas las ligas únicas no nulas registradas en la BD
            var ligas = await _context.Partidos_Futbol
                .Where(p => p.Liga != null && p.Liga != "")
                .Select(p => p.Liga!)
                .Distinct()
                .OrderBy(l => l)
                .ToListAsync();
            
            return Ok(ligas);
        }

        [HttpGet("matches")]
        public async Task<ActionResult<IEnumerable<object>>> GetMatches([FromQuery] string? liga = null)
        {
            var query = _context.Partidos_Futbol
                .Include(p => p.PartidoMaestro)
                .AsQueryable();

            // Filtrar por liga si se especifica
            if (!string.IsNullOrEmpty(liga))
                query = query.Where(p => p.Liga == liga);

            var partidos = await query
                .OrderByDescending(p => p.PartidoMaestro.Fecha_Evento)
                .Select(p => new {
                    p.ID_Partido,
                    p.EquipoLocal,
                    p.EquipoVisitante,
                    p.Liga,
                    FechaEvento = p.PartidoMaestro.Fecha_Evento,
                    Estado = p.PartidoMaestro.Estado,
                    Probabilidades = (p.FuerzaAtaqueLocal.HasValue && p.FuerzaDefensaVisita.HasValue) 
                        ? (object)new { 
                            probLocal = _eloService.CalcularProbabilidadesPoisson(p.FuerzaAtaqueLocal.Value, p.FuerzaDefensaVisita.Value, p.FuerzaAtaqueVisita ?? 1, p.FuerzaDefensaLocal ?? 1).ProbLocal,
                            probEmpate = _eloService.CalcularProbabilidadesPoisson(p.FuerzaAtaqueLocal.Value, p.FuerzaDefensaVisita.Value, p.FuerzaAtaqueVisita ?? 1, p.FuerzaDefensaLocal ?? 1).ProbEmpate,
                            probVisita = _eloService.CalcularProbabilidadesPoisson(p.FuerzaAtaqueLocal.Value, p.FuerzaDefensaVisita.Value, p.FuerzaAtaqueVisita ?? 1, p.FuerzaDefensaLocal ?? 1).ProbVisita,
                            marcadorPropuesto = _eloService.ObtenerMarcadorMasProbable(p.FuerzaAtaqueLocal.Value, p.FuerzaDefensaVisita.Value, p.FuerzaAtaqueVisita ?? 1, p.FuerzaDefensaLocal ?? 1).GolesLocal + "-" + _eloService.ObtenerMarcadorMasProbable(p.FuerzaAtaqueLocal.Value, p.FuerzaDefensaVisita.Value, p.FuerzaAtaqueVisita ?? 1, p.FuerzaDefensaLocal ?? 1).GolesVisita
                          }
                        : null
                })
                .ToListAsync();

            return Ok(partidos);
        }

        [HttpGet("prediction/{id}")]
        public async Task<ActionResult<object>> GetPrediction(int id)
        {
            var p = await _context.Partidos_Futbol
                .Include(p => p.PartidoMaestro)
                .FirstOrDefaultAsync(x => x.ID_Partido == id);

            if (p == null) return NotFound();

            if (!p.FuerzaAtaqueLocal.HasValue || !p.FuerzaDefensaVisita.HasValue)
                return BadRequest("Faltan datos de fuerza (Ataque/Defensa) para este partido.");

            var probs = _eloService.CalcularProbabilidadesPoisson(
                p.FuerzaAtaqueLocal.Value, p.FuerzaDefensaVisita.Value, 
                p.FuerzaAtaqueVisita ?? 1, p.FuerzaDefensaLocal ?? 1);

            return Ok(new {
                p.EquipoLocal,
                p.EquipoVisitante,
                Probabilidades = probs
            });
        }
        [HttpPost("registrar")]
        public async Task<IActionResult> RegistrarPartido([FromBody] FutbolMatchDto dto)
        {
            // Aseguramos que los equipos existan en la tabla maestra de Equipos (Rating ELO)
            await _eloService.ObtenerEloActual(dto.EquipoLocal, "Futbol");
            await _eloService.ObtenerEloActual(dto.EquipoVisitante, "Futbol");

            var maestro = new PartidoMaestro { Deporte = "Futbol", Fecha_Evento = dto.FechaEvento, Estado = "Programado" };
            var futbol = new FutbolMatch
            {
                EquipoLocal = dto.EquipoLocal,
                EquipoVisitante = dto.EquipoVisitante,
                Liga = dto.Liga,   // Nombre de liga ESPN (Premier League, LaLiga, etc.)
                FuerzaAtaqueLocal = (decimal)dto.FuerzaAtaqueLocal,
                FuerzaDefensaLocal = (decimal)dto.FuerzaDefensaLocal,
                FuerzaAtaqueVisita = (decimal)dto.FuerzaAtaqueVisita,
                FuerzaDefensaVisita = (decimal)dto.FuerzaDefensaVisita,
                PartidoMaestro = maestro
            };
            _context.Partidos_Futbol.Add(futbol);
            await _context.SaveChangesAsync();
            return Ok(new { id = futbol.ID_Partido, mensaje = "Partido de fútbol registrado." });
        }
        [HttpPost("odds/batch")]
        public async Task<IActionResult> InsertarCuotasLote([FromBody] List<NbaOddsBatchDto> oddsBatch)
        {
            int actualizados = 0;
            foreach (var odd in oddsBatch)
            {
                if (!DateTime.TryParseExact(odd.FechaEventoString, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
                    continue;

                var partido = await _context.Partidos_Futbol
                    .Include(p => p.PartidoMaestro)
                    .FirstOrDefaultAsync(p => 
                        p.EquipoLocal == odd.EquipoLocal && 
                        p.EquipoVisitante == odd.EquipoVisitante &&
                        p.PartidoMaestro.Fecha_Evento.Date == parsedDate.Date);

                if (partido != null)
                {
                    partido.CuotaLocal = (decimal)odd.CuotaPromedioLocal;
                    partido.CuotaVisitante = (decimal)odd.CuotaPromedioVisita;
                    partido.CuotaEmpate = (decimal)odd.CuotaPromedioEmpate; // Necesitaremos añadir esto al DTO o usar uno nuevo
                    actualizados++;
                }
            }
            await _context.SaveChangesAsync();
            return Ok(new { mensaje = $"Se actualizaron cuotas para {actualizados} partidos de fútbol." });
        }

        [HttpPut("{id}/resultado")]
        public async Task<IActionResult> RegistrarResultado(int id, [FromBody] NbaMatchResultDto resultado) // Reusing DTO for simplicity (PuntosLocal/Visitante maps to Goles)
        {
            var p = await _context.Partidos_Futbol.Include(x => x.PartidoMaestro).FirstOrDefaultAsync(x => x.ID_Partido == id);
            if (p == null) return NotFound();

            p.GolesLocal = resultado.PuntosLocal;
            p.GolesVisitante = resultado.PuntosVisitante;
            p.PartidoMaestro.Estado = "Finalizado";

            // Recalcular ELO de Futbol (asumimos que EloService soporta genérico o creamos lógica)
            var eloService = HttpContext.RequestServices.GetRequiredService<IEloService>();
            int eloLocal = await eloService.ObtenerEloActual(p.EquipoLocal, "Futbol");
            int eloVisita = await eloService.ObtenerEloActual(p.EquipoVisitante, "Futbol");
            
            double probLocal = eloService.CalcularProbabilidadVictoria(eloLocal, eloVisita, true);
            double probVisita = eloService.CalcularProbabilidadVictoria(eloVisita, eloLocal, false);

            int k = 32;
            int actLocal = p.GolesLocal > p.GolesVisitante ? 1 : (p.GolesLocal == p.GolesVisitante ? 0 : 0);
            int actVisita = p.GolesVisitante > p.GolesLocal ? 1 : (p.GolesLocal == p.GolesVisitante ? 0 : 0);
            if (p.GolesLocal == p.GolesVisitante) { actLocal = 1; actVisita = 1; } // Tie adjustment can be improved later

            int nuevoLocal = eloLocal + (int)(k * (actLocal - probLocal));
            int nuevoVisita = eloVisita + (int)(k * (actVisita - probVisita));

            var equipoLocalObj = await _context.Equipos.FirstOrDefaultAsync(x => x.NombreEquipo == p.EquipoLocal && x.Deporte == "Futbol");
            var equipoVisitaObj = await _context.Equipos.FirstOrDefaultAsync(x => x.NombreEquipo == p.EquipoVisitante && x.Deporte == "Futbol");

            if (equipoLocalObj != null) equipoLocalObj.EloActual = nuevoLocal;
            if (equipoVisitaObj != null) equipoVisitaObj.EloActual = nuevoVisita;

            await _context.SaveChangesAsync();

            return Ok(new { mensaje = $"Resultado registrado. ELO de {p.EquipoLocal}: {nuevoLocal}, ELO de {p.EquipoVisitante}: {nuevoVisita}." });
        }
    }
}
