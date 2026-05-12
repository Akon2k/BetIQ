using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BetIQ.API.Migrations
{
    /// <inheritdoc />
    public partial class AddOddsHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Odds_History",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Deporte = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EquipoLocal = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EquipoVisitante = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TimestampCaptura = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CuotaLocalRegistrada = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CuotaVisitanteRegistrada = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ProbabilidadModeloLocal = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ExpectedValueLocal = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ExpectedValueVisita = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    EsValueBet = table.Column<bool>(type: "bit", nullable: false),
                    EsLineaDeCierre = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Odds_History", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Odds_History");
        }
    }
}
