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
    public class TenisController : ControllerBase
    {
        private readonly BetIQContext _context;
        private readonly IEloService _eloService;

        public TenisController(BetIQContext context, IEloService eloService)
        {
            _context = context;
            _eloService = eloService;
        }

        [HttpGet("matches")]
        public async Task<ActionResult<IEnumerable<object>>> GetMatches()
        {
            var partidos = await _context.Partidos_Tenis
                .Include(p => p.PartidoMaestro)
                .OrderByDescending(p => p.PartidoMaestro.Fecha_Evento)
                .Select(p => new {
                    p.ID_Partido,
                    p.Jugador1,
                    p.Jugador2,
                    p.Torneo,
                    p.Superficie,
                    p.PartidoMaestro.Fecha_Evento,
                    p.PartidoMaestro.Estado,
                    EloJ1 = p.EloJugador1,
                    EloJ2 = p.EloJugador2,
                    ProbabilidadJ1 = p.EloJugador1.HasValue && p.EloJugador2.HasValue 
                        ? _eloService.CalcularProbabilidadVictoria(p.EloJugador1.Value, p.EloJugador2.Value, false)
                        : 0.5
                })
                .ToListAsync();

            return Ok(partidos);
        }
        [HttpPost("registrar")]
        public async Task<IActionResult> RegistrarPartido([FromBody] TenisMatchDto dto)
        {
            var maestro = new PartidoMaestro { Deporte = "Tenis", Fecha_Evento = dto.FechaEvento, Estado = "Programado" };
            var tenis = new TenisMatch
            {
                Jugador1 = dto.Jugador1,
                Jugador2 = dto.Jugador2,
                Torneo = dto.Torneo,
                Superficie = dto.Superficie,
                EloJugador1 = await _eloService.ObtenerEloActual(dto.Jugador1, "Tenis", dto.Superficie),
                EloJugador2 = await _eloService.ObtenerEloActual(dto.Jugador2, "Tenis", dto.Superficie),
                PartidoMaestro = maestro
            };
            _context.Partidos_Tenis.Add(tenis);
            await _context.SaveChangesAsync();
            return Ok(new { id = tenis.ID_Partido, mensaje = "Partido de tenis registrado." });
        }
        [HttpPost("odds/batch")]
        public async Task<IActionResult> InsertarCuotasLote([FromBody] List<NbaOddsBatchDto> oddsBatch)
        {
            int actualizados = 0;
            foreach (var odd in oddsBatch)
            {
                if (!DateTime.TryParseExact(odd.FechaEventoString, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
                    continue;

                var partido = await _context.Partidos_Tenis
                    .Include(p => p.PartidoMaestro)
                    .FirstOrDefaultAsync(p => 
                        p.Jugador1 == odd.EquipoLocal && 
                        p.Jugador2 == odd.EquipoVisitante &&
                        p.PartidoMaestro.Fecha_Evento.Date == parsedDate.Date);

                if (partido != null)
                {
                    partido.CuotaJ1 = (decimal)odd.CuotaPromedioLocal;
                    partido.CuotaJ2 = (decimal)odd.CuotaPromedioVisita;
                    actualizados++;
                }
            }
            await _context.SaveChangesAsync();
            return Ok(new { mensaje = $"Se actualizaron cuotas para {actualizados} partidos de tenis." });
        }

        [HttpPut("{id}/resultado")]
        public async Task<IActionResult> RegistrarResultado(int id, [FromBody] NbaMatchResultDto resultado)
        {
            var p = await _context.Partidos_Tenis
                .Include(x => x.PartidoMaestro)
                .FirstOrDefaultAsync(x => x.ID_Partido == id);

            if (p == null) return NotFound();

            p.PartidoMaestro.Estado = "Finalizado";
            
            // En el importador de tenis, Jugador1 es el ganador. 
            // Si puntosLocal (Sets J1) es mayor, confirmamos victoria J1.
            // La lógica de ActualizarEloTenis ya se encarga de los cálculos por superficie.
            await _eloService.ActualizarEloTenis(p);

            return Ok(new { mensaje = $"Resultado tenis registrado y ELO actualizado para {p.Jugador1} y {p.Jugador2}." });
        }
    }
}
