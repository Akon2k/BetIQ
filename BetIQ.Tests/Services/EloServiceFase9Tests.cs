using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BetIQ.API.Data;
using BetIQ.API.Models;
using BetIQ.API.Services;
using Xunit;

namespace BetIQ.Tests.Services
{
    public class EloServiceFase9Tests
    {
        private BetIQContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<BetIQContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new BetIQContext(options);
        }

        [Fact]
        public void CalcularProbabilidadVictoria_ShouldApplyHomeAdvantage()
        {
            // Arrange
            var service = new EloService(null!);
            int eloA = 1500;
            int eloB = 1500;

            // Act
            double probNeutral = service.CalcularProbabilidadVictoria(eloA, eloB, false);
            double probHome = service.CalcularProbabilidadVictoria(eloA, eloB, true);

            // Assert
            Assert.Equal(0.5, probNeutral);
            Assert.True(probHome > 0.6, "La probabilidad del local debería ser > 60% con bono de 100.");
            // 1 / (1 + 10^((1500-(1500+100))/400)) = 1 / (1 + 10^(-0.25)) = 0.64
            Assert.Equal(0.640, probHome, 3);
        }

        [Fact]
        public async Task CalcularKFactorDinamico_ShouldReturn40_OnWinningStreak()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new EloService(context);
            string team = "Winners";

            // Agregar 4 victorias en los últimos 5 partidos
            for (int i = 0; i < 4; i++)
            {
                var maestro = new PartidoMaestro { Deporte = "NBA", Estado = "Finalizado", Fecha_Evento = DateTime.Now.AddDays(-i) };
                var match = new NBAMatch { EquipoLocal = team, EquipoVisitante = "Opp", PuntosLocal = 110, PuntosVisitante = 100, PartidoMaestro = maestro };
                context.Partidos_NBA.Add(match);
            }
            await context.SaveChangesAsync();

            // Act
            int k = await service.CalcularKFactorDinamico(team);

            // Assert
            Assert.Equal(40, k);
        }

        [Fact]
        public async Task CalcularKFactorDinamico_ShouldReturn32_OnNormalPerformance()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new EloService(context);
            string team = "Regulars";

            // 2 victorias, 2 derrotas
            for (int i = 0; i < 4; i++)
            {
                var maestro = new PartidoMaestro { Deporte = "NBA", Estado = "Finalizado", Fecha_Evento = DateTime.Now.AddDays(-i) };
                var match = new NBAMatch { 
                    EquipoLocal = team, 
                    EquipoVisitante = "Opp", 
                    PuntosLocal = (i < 2 ? 110 : 90), 
                    PuntosVisitante = 100, 
                    PartidoMaestro = maestro 
                };
                context.Partidos_NBA.Add(match);
            }
            await context.SaveChangesAsync();

            // Act
            int k = await service.CalcularKFactorDinamico(team);

            // Assert
            Assert.Equal(32, k);
        }
    }
}
