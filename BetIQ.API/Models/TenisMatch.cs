using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BetIQ.API.Models
{
    [Table("Partidos_Tenis")]
    public class TenisMatch
    {
        [Key]
        public int ID_Partido { get; set; }

        public string Jugador1 { get; set; } = string.Empty;
        public string Jugador2 { get; set; } = string.Empty;

        public string? Superficie { get; set; }
        public int? EloJugador1 { get; set; }
        public int? EloJugador2 { get; set; }
        public string? Torneo { get; set; }

        // Resultados de sets
        public string? ResultadoSets { get; set; } 

        // Cuotas de mercado
        public decimal? CuotaJ1 { get; set; }
        public decimal? CuotaJ2 { get; set; }

        [ForeignKey("ID_Partido")]
        public virtual PartidoMaestro? PartidoMaestro { get; set; }
    }
}
