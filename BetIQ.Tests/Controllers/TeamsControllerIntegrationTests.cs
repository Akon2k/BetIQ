using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using BetIQ.API.Controllers;
using BetIQ.API.Data;
using BetIQ.API.Models;
using BetIQ.API.Services;
using Xunit;

namespace BetIQ.Tests.Controllers
{
    public class TeamsControllerIntegrationTests
    {
        private (TeamsController controller, BetIQContext context) GetController()
        {
            var options = new DbContextOptionsBuilder<BetIQContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new BetIQContext(options);
            
            // Setup cache
            var services = new ServiceCollection();
            services.AddMemoryCache();
            var serviceProvider = services.BuildServiceProvider();
            var cache = serviceProvider.GetRequiredService<IMemoryCache>();

            // Setup Controller with NullLogger
            var controller = new TeamsController(context, cache, NullLogger<TeamsController>.Instance);
            
            // Mock IEloService in HttpContext scoped services (used by some endpoints)
            var eloService = new EloService(context);
            var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
            httpContext.RequestServices = new ServiceCollection()
                .AddScoped<IEloService>(_ => eloService)
                .BuildServiceProvider();
            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            return (controller, context);
        }

        [Fact]
        public async Task GetStandings_ShouldReturnEmpty_WhenNoMatchesExist()
        {
            // Arrange
            var (controller, _) = GetController();

            // Act
            var actionResult = await controller.GetStandings();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            
            var json = JsonSerializer.Serialize(okResult.Value);
            using var doc = JsonDocument.Parse(json);
            
            Assert.Empty(doc.RootElement.GetProperty("Este").EnumerateArray());
            Assert.Empty(doc.RootElement.GetProperty("Oeste").EnumerateArray());
        }

        [Fact]
        public async Task GetStandings_ShouldCalculateStatsCorrectly_AfterMatch()
        {
            // Arrange
            var (controller, context) = GetController();

            var maestro = new PartidoMaestro { Deporte = "NBA", Estado = "Finalizado", Fecha_Evento = DateTime.Now };
            var match = new NBAMatch 
            { 
                EquipoLocal = "Lakers", 
                EquipoVisitante = "Bulls", 
                PuntosLocal = 110, 
                PuntosVisitante = 100,
                PartidoMaestro = maestro
            };

            context.Partidos_NBA.Add(match);
            await context.SaveChangesAsync();

            // Act
            var actionResult = await controller.GetStandings();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var json = JsonSerializer.Serialize(okResult.Value);
            using var doc = JsonDocument.Parse(json);

            // Los Lakers están mapeados como Oeste, los Bulls como Este en el controlador
            var oeste = doc.RootElement.GetProperty("Oeste").EnumerateArray().ToList();
            Assert.Single(oeste);
            Assert.Equal("Lakers", oeste[0].GetProperty("Equipo").GetString());
            
            var este = doc.RootElement.GetProperty("Este").EnumerateArray().ToList();
            Assert.Single(este);
            Assert.Equal("Bulls", este[0].GetProperty("Equipo").GetString());
        }

        [Fact]
        public async Task GetProbability_ShouldReturn200_ForValidTeams()
        {
            // Arrange
            var (controller, _) = GetController();

            // Act
            var actionResult = await controller.GetProbability("Lakers", "Bulls");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var json = JsonSerializer.Serialize(okResult.Value);
            using var doc = JsonDocument.Parse(json);

            Assert.Equal("Lakers", doc.RootElement.GetProperty("Local").GetProperty("Equipo").GetString());
            Assert.Equal("Bulls", doc.RootElement.GetProperty("Visitante").GetProperty("Equipo").GetString());
        }
    }
}
