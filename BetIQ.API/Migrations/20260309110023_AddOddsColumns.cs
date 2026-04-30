using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BetIQ.API.Migrations
{
    /// <inheritdoc />
    public partial class AddOddsColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CuotaLocal",
                table: "Partidos_NBA",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CuotaVisitante",
                table: "Partidos_NBA",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CuotaLocal",
                table: "Partidos_NBA");

            migrationBuilder.DropColumn(
                name: "CuotaVisitante",
                table: "Partidos_NBA");
        }
    }
}
