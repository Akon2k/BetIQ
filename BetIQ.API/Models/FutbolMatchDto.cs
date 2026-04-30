using System;

namespace BetIQ.API.Models
{
    public class FutbolMatchDto
    {
        public string EquipoLocal { get; set; } = string.Empty;
        public string EquipoVisitante { get; set; } = string.Empty;
        public string? Liga { get; set; }  // Nombre de la liga (Premier League, LaLiga, etc.)
        public DateTime FechaEvento { get; set; }
        public double FuerzaAtaqueLocal { get; set; }
        public double FuerzaDefensaLocal { get; set; }
        public double FuerzaAtaqueVisita { get; set; }
        public double FuerzaDefensaVisita { get; set; }
    }
}
