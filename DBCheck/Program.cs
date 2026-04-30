using System;
using Microsoft.Data.SqlClient;

class Program
{
    static void Main()
    {
        string connectionString = "Server=.\\SQLEXPRESS;Database=BetIQ_DB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
        
        try 
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                Console.WriteLine("Conexión exitosa a SQL Server!");
                
                var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT PM.ID_Partido, PM.Fecha_Evento, PN.Equipo_Local, PN.Equipo_Visitante, PN.CuotaLocal, PN.CuotaVisitante, PM.Estado 
                    FROM Partidos_Maestro PM 
                    JOIN Partidos_NBA PN ON PM.ID_Partido = PN.ID_Partido 
                    WHERE PM.Estado != 'Finalizado' AND PN.CuotaLocal IS NOT NULL;";
                    
                using (var reader = command.ExecuteReader())
                {
                    Console.WriteLine("Partidos NBA con cuotas pendientes:");
                    bool found = false;
                    while (reader.Read())
                    {
                        found = true;
                        var fecha = reader.GetDateTime(1);
                        var local = reader.GetString(2);
                        var visita = reader.GetString(3);
                        var cuotaL = reader.GetDecimal(4);
                        var cuotaV = reader.GetDecimal(5);
                        var estado = reader.GetString(6);
                        Console.WriteLine($"{fecha:yyyy-MM-dd HH:mm} | {local} ({cuotaL}) vs {visita} ({cuotaV}) | Estado: {estado}");
                    }
                    if (!found) Console.WriteLine("No se encontraron partidos pendientes con cuotas.");
                }
            }
        } 
        catch (Exception ex) 
        {
            Console.WriteLine("Error fatal: " + ex.Message);
        }
    }
}
