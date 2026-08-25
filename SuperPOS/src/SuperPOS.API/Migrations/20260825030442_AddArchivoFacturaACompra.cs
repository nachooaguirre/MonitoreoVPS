using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SuperPOS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddArchivoFacturaACompra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ArchivoFacturaNombre",
                table: "Compras",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArchivoFacturaRutaRelativa",
                table: "Compras",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArchivoFacturaNombre",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "ArchivoFacturaRutaRelativa",
                table: "Compras");
        }
    }
}
