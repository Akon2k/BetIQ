namespace BetIQ.API.Models
{
    // Data Transfer Object para registrar un nuevo partido de la NBA.
    // Esto desacopla el modelo de la API del modelo de la base de datos.
    public class NbaMatchDto
    {
        public string EquipoLocal { get; set; } = string.Empty;
        public string EquipoVisitante { get; set; } = string.Empty;
        public DateTime FechaEvento { get; set; }
        public double EficienciaOfensivaLocal { get; set; }
        public double EficienciaDefensivaLocal { get; set; }
        public double PromedioPuntosTotal { get; set; }
        public double? TrueShootingLocal { get; set; }
        public double? TrueShootingVisitante { get; set; }
        public double? NetRatingLocal { get; set; }
        public double? NetRatingVisitante { get; set; }
    }
}
