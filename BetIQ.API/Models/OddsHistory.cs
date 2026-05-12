using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BetIQ.API.Models
{
    [Table("Odds_History")]
    public class OddsHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Deporte { get; set; } = string.Empty;
        public string EquipoLocal { get; set; } = string.Empty;
        public string EquipoVisitante { get; set; } = string.Empty;
        
        public DateTime TimestampCaptura { get; set; }

        [Column(TypeName = "decimal(18, 4)")]
        public decimal CuotaLocalRegistrada { get; set; }

        [Column(TypeName = "decimal(18, 4)")]
        public decimal CuotaVisitanteRegistrada { get; set; }

        [Column(TypeName = "decimal(18, 4)")]
        public decimal ProbabilidadModeloLocal { get; set; }

        [Column(TypeName = "decimal(18, 4)")]
        public decimal ExpectedValueLocal { get; set; }

        [Column(TypeName = "decimal(18, 4)")]
        public decimal ExpectedValueVisita { get; set; }
        
        public bool EsValueBet { get; set; } // Si marcamos esta entrada como una oportunidad real
        public bool EsLineaDeCierre { get; set; } // Determina si fue la última registrada antes del partido
    }
}
