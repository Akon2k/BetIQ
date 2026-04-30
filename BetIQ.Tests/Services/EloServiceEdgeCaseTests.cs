using BetIQ.API.Services;
using Xunit;

namespace BetIQ.Tests.Services
{
    public class EloServiceEdgeCaseTests
    {
        private readonly EloService _service;

        public EloServiceEdgeCaseTests()
        {
            // Nota: Para estos tests de cálculo puro no necesitamos el DBContext
            _service = new EloService(null!); 
        }

        [Theory]
        [InlineData(0.5, 2.0, 0.0)]    // (0.5 * 2) - 1 = 0
        [InlineData(0.6, 2.0, 0.2)]    // (0.6 * 2) - 1 = 0.2
        [InlineData(0.4, 2.0, -0.2)]   // (0.4 * 2) - 1 = -0.2
        public void CalcularEV_ShouldReturnCorrectValue(double prob, double cuota, double expected)
        {
            // Act
            double result = _service.CalcularEV(prob, cuota);

            // Assert
            Assert.Equal(expected, result, 4);
        }

        [Fact]
        public void CalcularPorcentajeKelly_ShouldReturnZero_WhenEVIsNegative()
        {
            // Arrange
            double prob = 0.3; // 30%
            double cuota = 2.0; 
            // b = 2-1 = 1. p = 0.3. q = 0.7.
            // Kelly = (1*0.3 - 0.7) / 1 = -0.4

            // Act
            double result = _service.CalcularPorcentajeKelly(prob, cuota);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void CalcularPorcentajeKelly_ShouldReturnPositive_WhenEVIsPositive()
        {
            // Arrange
            double prob = 0.6; 
            double cuota = 2.0;
            // b = 1. p = 0.6. q = 0.4.
            // Kelly = (1*0.6 - 0.4) / 1 = 0.2 (20%)

            // Act
            double result = _service.CalcularPorcentajeKelly(prob, cuota);

            // Assert
            Assert.Equal(0.2, result, 4);
        }

        [Fact]
        public void CalcularPorcentajeKelly_ShouldHandleZeroOrNegativeCuota()
        {
            // Act
            double resultZero = _service.CalcularPorcentajeKelly(0.5, 1.0);
            double resultNegative = _service.CalcularPorcentajeKelly(0.5, 0.5);

            // Assert
            Assert.Equal(0, resultZero);
            Assert.Equal(0, resultNegative);
        }
    }
}
