using System;
using Microsoft.Data.Sqlite;

class Program
{
    static void Main()
    {
        string connectionString = "Data Source=BetIQDB.db";
        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();
            
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT pm.Fecha_Evento, p.EquipoLocal, p.EquipoVisitante, p.CuotaLocal, p.CuotaVisitante, pm.Estado
                FROM Partidos_NBA p
                JOIN Partidos_Maestro pm ON p.ID_Partido = pm.ID_Partido
                WHERE p.CuotaLocal IS NOT NULL 
                AND pm.Estado != 'Finalizado'
                ORDER BY pm.Fecha_Evento ASC
                LIMIT 10;";
                
            using (var reader = command.ExecuteReader())
            {
                Console.WriteLine("Partidos NBA con cuotas pendientes:");
                bool found = false;
                while (reader.Read())
                {
                    found = true;
                    var fecha = reader.GetDateTime(0);
                    var local = reader.GetString(1);
                    var visita = reader.GetString(2);
                    var cuotaL = reader.GetDouble(3);
                    var cuotaV = reader.GetDouble(4);
                    var estado = reader.GetString(5);
                    Console.WriteLine($"{fecha:yyyy-MM-dd HH:mm} | {local} ({cuotaL}) vs {visita} ({cuotaV}) | Estado: {estado}");
                }
                if (!found) Console.WriteLine("No se encontraron partidos pendientes con cuotas.");
            }
        }
    }
}
