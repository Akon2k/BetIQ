using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BetIQ.API.Models
{
    [Table("Partidos_Futbol")]
    public class FutbolMatch
    {
        [Key]
        public int ID_Partido { get; set; }

        public string EquipoLocal { get; set; } = string.Empty;
        public string EquipoVisitante { get; set; } = string.Empty;
        
        // Liga o competición (ej: "Premier League", "La Liga", "Champions League")
        public string? Liga { get; set; }

        public decimal? FuerzaAtaqueLocal { get; set; }
        public decimal? FuerzaDefensaLocal { get; set; }
        public decimal? FuerzaAtaqueVisita { get; set; }
        public decimal? FuerzaDefensaVisita { get; set; }

        // Goles (aunque no están en la tabla SQL original de Partidos_Futbol, 
        // se suelen necesitar para cerrar resultados. 
        // Si no están, usaremos el estado del PartidoMaestro)
        public int? GolesLocal { get; set; }
        public int? GolesVisitante { get; set; }

        // Cuotas de mercado
        public decimal? CuotaLocal { get; set; }
        public decimal? CuotaVisitante { get; set; }
        public decimal? CuotaEmpate { get; set; }

        [ForeignKey("ID_Partido")]
        public virtual PartidoMaestro? PartidoMaestro { get; set; }
    }
}
