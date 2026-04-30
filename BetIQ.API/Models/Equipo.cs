using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BetIQ.API.Models
{
    [Table("Equipos")]
    public class Equipo
    {
        [Key]
        [Column("Nombre_Equipo")]
        public string NombreEquipo { get; set; } = string.Empty;

        [Column("ELO_Actual")]
        public int EloActual { get; set; }

        [Column("ELO_Arcilla")]
        public int? EloArcilla { get; set; }

        [Column("ELO_Pasto")]
        public int? EloPasto { get; set; }

        [Column("ELO_Dura")]
        public int? EloDura { get; set; }

        [Column("Deporte")]
        public string Deporte { get; set; } = string.Empty;
    }
}
