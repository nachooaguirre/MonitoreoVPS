using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SuperPOS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddTarjetasMarca : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TarjetasMarca",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "text", nullable: false),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    EsCredito = table.Column<bool>(type: "boolean", nullable: false),
                    PorcentajeRecargo = table.Column<decimal>(type: "numeric(6,3)", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TarjetasMarca", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "TarjetasMarca",
                columns: new[] { "Id", "Activo", "Codigo", "EsCredito", "Nombre", "PorcentajeRecargo" },
                values: new object[,]
                {
                    { 1, true, "visa-cred", true, "Visa Crédito", 0m },
                    { 2, true, "visa-deb", false, "Visa Débito", 0m },
                    { 3, true, "master-cred", true, "Mastercard Crédito", 0m },
                    { 4, true, "master-deb", false, "Mastercard Débito", 0m },
                    { 5, true, "cabal-cred", true, "Cabal Crédito", 0m },
                    { 6, true, "cabal-deb", false, "Cabal Débito", 0m },
                    { 7, true, "amex", true, "American Express", 0m },
                    { 8, true, "naranja", true, "Naranja", 0m },
                    { 9, true, "nativa", true, "Nativa", 0m }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TarjetasMarca");
        }
    }
}
