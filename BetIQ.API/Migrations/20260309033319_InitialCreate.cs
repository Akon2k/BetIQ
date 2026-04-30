using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BetIQ.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Equipos",
                columns: table => new
                {
                    Nombre_Equipo = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ELO_Actual = table.Column<int>(type: "int", nullable: false),
                    Deporte = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipos", x => x.Nombre_Equipo);
                });

            migrationBuilder.CreateTable(
                name: "Partidos_Maestro",
                columns: table => new
                {
                    ID_Partido = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Deporte = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Fecha_Evento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Partidos_Maestro", x => x.ID_Partido);
                });

            migrationBuilder.CreateTable(
                name: "Partidos_NBA",
                columns: table => new
                {
                    ID_Partido = table.Column<int>(type: "int", nullable: false),
                    Equipo_Local = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Equipo_Visitante = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Puntos_Local = table.Column<int>(type: "int", nullable: true),
                    Puntos_Visitante = table.Column<int>(type: "int", nullable: true),
                    Eficiencia_Ofensiva_Local = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Eficiencia_Defensiva_Local = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ELO_Local = table.Column<int>(type: "int", nullable: false),
                    ELO_Visita = table.Column<int>(type: "int", nullable: false),
                    Promedio_Puntos_Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Partidos_NBA", x => x.ID_Partido);
                    table.ForeignKey(
                        name: "FK_Partidos_NBA_Partidos_Maestro_ID_Partido",
                        column: x => x.ID_Partido,
                        principalTable: "Partidos_Maestro",
                        principalColumn: "ID_Partido",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Equipos");

            migrationBuilder.DropTable(
                name: "Partidos_NBA");

            migrationBuilder.DropTable(
                name: "Partidos_Maestro");
        }
    }
}
