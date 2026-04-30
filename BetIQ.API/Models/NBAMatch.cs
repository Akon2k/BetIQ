using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BetIQ.API.Models
{
    [Table("Partidos_NBA")]
    public class NBAMatch
    {
        [Key]
        [ForeignKey("PartidoMaestro")]
        public int ID_Partido { get; set; }

        public string EquipoLocal { get; set; } = string.Empty;
        public string EquipoVisitante { get; set; } = string.Empty;
        public int? PuntosLocal { get; set; }
        public int? PuntosVisitante { get; set; }
        public decimal EficienciaOfensivaLocal { get; set; }
        public decimal EficienciaDefensivaLocal { get; set; }
        public int ELOLocal { get; set; }
        public int ELOVisita { get; set; }
        public decimal PromedioPuntosTotal { get; set; }
        public decimal? CuotaLocal { get; set; }
        public decimal? CuotaVisitante { get; set; }

        public decimal? TrueShootingLocal { get; set; }
        public decimal? TrueShootingVisitante { get; set; }
        public decimal? NetRatingLocal { get; set; }
        public decimal? NetRatingVisitante { get; set; }

        // Propiedad de navegación para la relación uno a uno
        public virtual PartidoMaestro PartidoMaestro { get; set; } = null!;
    }
}
