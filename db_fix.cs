using Microsoft.Data.SqlClient;
using System;
using System.IO;

string connectionString = "Server=localhost;Database=BetIQ_DB;Trusted_Connection=True;TrustServerCertificate=True";
string sqlPath = @"c:\Users\iscv\OneDrive\Escritorio\BetIQ\schema.sql";

try
{
    Console.WriteLine("Iniciando reconstruccion de tablas para Futbol y Tenis...");
    string script = File.ReadAllText(sqlPath);
    
    using (var conn = new SqlConnection(connectionString))
    {
        conn.Open();
        
        // Dropping tables in order to avoid FK conflicts if they exist
        Console.WriteLine("Limpiando tablas antiguas...");
        var dropCmd = new SqlCommand(@"
            IF OBJECT_ID('Partidos_NBA', 'U') IS NOT NULL DROP TABLE Partidos_NBA;
            IF OBJECT_ID('Partidos_Futbol', 'U') IS NOT NULL DROP TABLE Partidos_Futbol;
            IF OBJECT_ID('Partidos_Tenis', 'U') IS NOT NULL DROP TABLE Partidos_Tenis;
            IF OBJECT_ID('Partidos_Maestro', 'U') IS NOT NULL DROP TABLE Partidos_Maestro;
            IF OBJECT_ID('Equipos', 'U') IS NOT NULL DROP TABLE Equipos;
        ", conn);
        dropCmd.ExecuteNonQuery();

        Console.WriteLine("Ejecutando schema.sql...");
        var cmd = new SqlCommand(script, conn);
        cmd.ExecuteNonQuery();
        
        // Agregar las columnas de cuotas si no estaban en el schema.sql original
        Console.WriteLine("Asegurando columnas de cuotas...");
        var alterCmd = new SqlCommand(@"
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Partidos_Futbol') AND name = 'CuotaLocal')
                ALTER TABLE Partidos_Futbol ADD CuotaLocal DECIMAL(10,2), CuotaVisitante DECIMAL(10,2), CuotaEmpate DECIMAL(10,2);
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Partidos_Tenis') AND name = 'CuotaJ1')
                ALTER TABLE Partidos_Tenis ADD CuotaJ1 DECIMAL(10,2), CuotaJ2 DECIMAL(10,2);
        ", conn);
        alterCmd.ExecuteNonQuery();
    }
    Console.WriteLine("¡Base de datos sincronizada con exito!");
}
catch (Exception ex)
{
    Console.WriteLine("Error: " + ex.Message);
}
