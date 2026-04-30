using System;

namespace BetIQ.API.Models
{
    public class TenisMatchDto
    {
        public string Jugador1 { get; set; } = string.Empty;
        public string Jugador2 { get; set; } = string.Empty;
        public DateTime FechaEvento { get; set; }
        public string? Torneo { get; set; }
        public string? Superficie { get; set; }
    }
}
