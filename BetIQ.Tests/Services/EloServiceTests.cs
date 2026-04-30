using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BetIQ.API.Data;
using BetIQ.API.Models;
using BetIQ.API.Services;
using Xunit;

namespace BetIQ.Tests.Services
{
    public class EloServiceTests
    {
        private BetIQContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<BetIQContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new BetIQContext(options);
        }

        [Fact]
        public async Task ObtenerEloActual_ShouldReturnInitialElo_WhenTeamDoesNotExist()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new EloService(context);

            // Act
            var elo = await service.ObtenerEloActual("Lakers", "NBA");

            // Assert
            Assert.Equal(1500, elo); // 1500 is EloInicial
            
            var savedTeam = await context.Equipos.FindAsync("Lakers");
            Assert.NotNull(savedTeam);
            Assert.Equal(1500, savedTeam.EloActual);
        }

        [Fact]
        public async Task ObtenerEloActual_ShouldReturnExistingElo_WhenTeamExists()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            context.Equipos.Add(new Equipo { NombreEquipo = "Bulls", Deporte = "NBA", EloActual = 1600 });
            await context.SaveChangesAsync();
            
            var service = new EloService(context);

            // Act
            var elo = await service.ObtenerEloActual("Bulls", "NBA");

            // Assert
            Assert.Equal(1600, elo);
        }

        [Fact]
        public async Task ActualizarElo_ShouldNotUpdate_WhenPointsAreNull()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            context.Equipos.Add(new Equipo { NombreEquipo = "TeamA", EloActual = 1500 });
            context.Equipos.Add(new Equipo { NombreEquipo = "TeamB", EloActual = 1500 });
            await context.SaveChangesAsync();

            var service = new EloService(context);
            var match = new NBAMatch { EquipoLocal = "TeamA", EquipoVisitante = "TeamB", PuntosLocal = null, PuntosVisitante = null };

            // Act
            await service.ActualizarElo(match);

            // Assert
            var teamA = await context.Equipos.FindAsync("TeamA");
            var teamB = await context.Equipos.FindAsync("TeamB");
            
            Assert.Equal(1500, teamA?.EloActual);
            Assert.Equal(1500, teamB?.EloActual);
        }

        [Fact]
        public async Task ActualizarElo_ShouldUpdateCorrectly_WhenLocalWins()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            context.Equipos.Add(new Equipo { NombreEquipo = "TeamA", EloActual = 1500 });
            context.Equipos.Add(new Equipo { NombreEquipo = "TeamB", EloActual = 1500 });
            await context.SaveChangesAsync();

            var service = new EloService(context);
            var match = new NBAMatch { EquipoLocal = "TeamA", EquipoVisitante = "TeamB", PuntosLocal = 100, PuntosVisitante = 90 };

            // Act
            await service.ActualizarElo(match);

            // Assert
            var teamA = await context.Equipos.FindAsync("TeamA");
            var teamB = await context.Equipos.FindAsync("TeamB");
            
            // Si ambos tienen 1500, pero TeamA es local, tiene bono virtual +100.
            // Probabilidad Local (TeamA) = 0.64
            // Nuevo Elo Local = 1500 + 32 * (1 - 0.64) = 1500 + 32 * 0.36 = 1511.52 -> 1512
            // Nuevo Elo Visita = 1500 + 32 * (0 - 0.36) = 1488.48 -> 1488
            Assert.Equal(1512, teamA?.EloActual);
            Assert.Equal(1488, teamB?.EloActual);
        }
        
        [Fact]
        public async Task ActualizarElo_ShouldUpdateCorrectly_WhenVisitorWins()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            context.Equipos.Add(new Equipo { NombreEquipo = "TeamA", EloActual = 1500 });
            context.Equipos.Add(new Equipo { NombreEquipo = "TeamB", EloActual = 1500 });
            await context.SaveChangesAsync();

            var service = new EloService(context);
            var match = new NBAMatch { EquipoLocal = "TeamA", EquipoVisitante = "TeamB", PuntosLocal = 90, PuntosVisitante = 100 };

            // Act
            await service.ActualizarElo(match);

            // Assert
            var teamA = await context.Equipos.FindAsync("TeamA");
            var teamB = await context.Equipos.FindAsync("TeamB");
            
            // Si ambos tienen 1500, pero TeamA es local, tiene bono virtual +100.
            // Probabilidad Local (TeamA) = 0.64, Probabilidad Visita (TeamB) = 0.36
            // Nuevo Elo Local = 1500 + 32 * (0 - 0.64) = 1500 - 20.48 = 1480
            // Nuevo Elo Visita = 1500 + 32 * (1 - 0.36) = 1500 + 20.48 = 1520
            Assert.Equal(1480, teamA?.EloActual);
            Assert.Equal(1520, teamB?.EloActual);
        }
    }
}
