using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using BetIQ.API.Data;
using BetIQ.API.Models;

namespace BetIQ.API.Services
{
    public class ClvTrackerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public ClvTrackerService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Esperar un poco antes de arrancar la primera vez
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RastrearCierreDeCuotas();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error en ClvTrackerService: {ex.Message}");
                }

                // Ejecutar cada 15 minutos
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }
        }

        private async Task RastrearCierreDeCuotas()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<BetIQContext>();

            var ahora = DateTime.UtcNow;
            var ventanaCierre = ahora.AddMinutes(30);

            // Buscar partidos que empiecen en los próximos 30 minutos (Línea de Cierre)
            var partidosPorCerrarNBA = await context.Partidos_NBA
                .Include(p => p.PartidoMaestro)
                .Where(p => p.PartidoMaestro.Fecha_Evento > ahora 
                         && p.PartidoMaestro.Fecha_Evento <= ventanaCierre
                         && p.CuotaLocal != null 
                         && p.CuotaVisitante != null)
                .ToListAsync();

            foreach (var p in partidosPorCerrarNBA)
            {
                // Verificar si ya guardamos el snapshot de cierre para este partido
                var yaRegistrado = await context.OddsHistory
                    .AnyAsync(h => h.EquipoLocal == p.EquipoLocal 
                                && h.EquipoVisitante == p.EquipoVisitante
                                && h.EsLineaDeCierre == true
                                && h.TimestampCaptura.Date == ahora.Date);

                if (!yaRegistrado)
                {
                    var snapshotCierre = new OddsHistory
                    {
                        Deporte = "NBA",
                        EquipoLocal = p.EquipoLocal,
                        EquipoVisitante = p.EquipoVisitante,
                        TimestampCaptura = ahora,
                        CuotaLocalRegistrada = (decimal)p.CuotaLocal!,
                        CuotaVisitanteRegistrada = (decimal)p.CuotaVisitante!,
                        ProbabilidadModeloLocal = 0, // No hace falta recalcular ELO aquí, solo rastreamos la cuota real
                        ExpectedValueLocal = 0,
                        ExpectedValueVisita = 0,
                        EsValueBet = false,
                        EsLineaDeCierre = true
                    };
                    
                    context.OddsHistory.Add(snapshotCierre);
                    Console.WriteLine($"[CLV Tracker] Línea de cierre capturada para NBA: {p.EquipoLocal} vs {p.EquipoVisitante} -> {p.CuotaLocal} / {p.CuotaVisitante}");
                }
            }
            
            await context.SaveChangesAsync();
        }
    }
}
