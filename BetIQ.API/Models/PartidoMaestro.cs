using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BetIQ.API.Models
{
    // Este modelo representa la tabla central que une todos los partidos.
    [Table("Partidos_Maestro")]
    public class PartidoMaestro
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // El ID es autogenerado por la BD
        public int ID_Partido { get; set; }

        [Required]
        public string Deporte { get; set; } = string.Empty;

        public DateTime Fecha_Evento { get; set; }

        public string Estado { get; set; } = "Programado";

        // Propiedad de navegación para las relaciones uno a uno
        [JsonIgnore]
        public virtual NBAMatch? NbaMatch { get; set; }

        [JsonIgnore]
        public virtual FutbolMatch? FutbolMatch { get; set; }

        [JsonIgnore]
        public virtual TenisMatch? TenisMatch { get; set; }
    }
}
