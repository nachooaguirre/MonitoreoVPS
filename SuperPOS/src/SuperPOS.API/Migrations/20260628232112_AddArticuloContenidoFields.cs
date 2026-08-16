using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SuperPOS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddArticuloContenidoFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContenidoUnidad",
                table: "Articulos",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "UN");

            migrationBuilder.AddColumn<decimal>(
                name: "ContenidoValor",
                table: "Articulos",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 1m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContenidoUnidad",
                table: "Articulos");

            migrationBuilder.DropColumn(
                name: "ContenidoValor",
                table: "Articulos");
        }
    }
}
