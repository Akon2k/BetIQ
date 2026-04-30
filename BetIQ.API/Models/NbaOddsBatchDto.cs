namespace BetIQ.API.Models
{
    public class NbaOddsBatchDto
    {
        public string EquipoLocal { get; set; } = string.Empty;
        public string EquipoVisitante { get; set; } = string.Empty;
        public string FechaEventoString { get; set; } = string.Empty;
        public double CuotaPromedioLocal { get; set; }
        public double CuotaPromedioVisita { get; set; }
        public double CuotaPromedioEmpate { get; set; }
    }
}
