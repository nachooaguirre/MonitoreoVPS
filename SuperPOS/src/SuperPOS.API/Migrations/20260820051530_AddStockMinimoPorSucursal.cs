using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SuperPOS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddStockMinimoPorSucursal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "StockMinimo",
                table: "ArticulosStockPorSucursal",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "StockMinimoGondola",
                table: "ArticulosStockPorSucursal",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StockMinimo",
                table: "ArticulosStockPorSucursal");

            migrationBuilder.DropColumn(
                name: "StockMinimoGondola",
                table: "ArticulosStockPorSucursal");
        }
    }
}
