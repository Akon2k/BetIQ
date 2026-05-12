using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BetIQ.API.Migrations
{
    /// <inheritdoc />
    public partial class SyncSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "NetRatingLocal",
                table: "Partidos_NBA",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NetRatingVisitante",
                table: "Partidos_NBA",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TrueShootingLocal",
                table: "Partidos_NBA",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TrueShootingVisitante",
                table: "Partidos_NBA",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ELO_Arcilla",
                table: "Equipos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ELO_Dura",
                table: "Equipos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ELO_Pasto",
                table: "Equipos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Partidos_Futbol",
                columns: table => new
                {
                    ID_Partido = table.Column<int>(type: "int", nullable: false),
                    Equipo_Local = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Equipo_Visitante = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Liga = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Fuerza_Ataque_Local = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Fuerza_Defensa_Local = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Fuerza_Ataque_Visita = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Fuerza_Defensa_Visita = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    GolesLocal = table.Column<int>(type: "int", nullable: true),
                    GolesVisitante = table.Column<int>(type: "int", nullable: true),
                    CuotaLocal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CuotaVisitante = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CuotaEmpate = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Partidos_Futbol", x => x.ID_Partido);
                    table.ForeignKey(
                        name: "FK_Partidos_Futbol_Partidos_Maestro_ID_Partido",
                        column: x => x.ID_Partido,
                        principalTable: "Partidos_Maestro",
                        principalColumn: "ID_Partido",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Partidos_Tenis",
                columns: table => new
                {
                    ID_Partido = table.Column<int>(type: "int", nullable: false),
                    Jugador_1 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Jugador_2 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Superficie = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ELO_Jugador_1 = table.Column<int>(type: "int", nullable: true),
                    ELO_Jugador_2 = table.Column<int>(type: "int", nullable: true),
                    Torneo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResultadoSets = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CuotaJ1 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CuotaJ2 = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Partidos_Tenis", x => x.ID_Partido);
                    table.ForeignKey(
                        name: "FK_Partidos_Tenis_Partidos_Maestro_ID_Partido",
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
                name: "Partidos_Futbol");

            migrationBuilder.DropTable(
                name: "Partidos_Tenis");

            migrationBuilder.DropColumn(
                name: "NetRatingLocal",
                table: "Partidos_NBA");

            migrationBuilder.DropColumn(
                name: "NetRatingVisitante",
                table: "Partidos_NBA");

            migrationBuilder.DropColumn(
                name: "TrueShootingLocal",
                table: "Partidos_NBA");

            migrationBuilder.DropColumn(
                name: "TrueShootingVisitante",
                table: "Partidos_NBA");

            migrationBuilder.DropColumn(
                name: "ELO_Arcilla",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "ELO_Dura",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "ELO_Pasto",
                table: "Equipos");
        }
    }
}
