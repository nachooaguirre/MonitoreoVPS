using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SuperPOS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddInventarioDetalleFueConteado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "FueConteado",
                table: "InventariosDetalle",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Fila ya contada en versiones anteriores (no podemos detectar "conteo cero" explícito)
            migrationBuilder.Sql("""UPDATE "InventariosDetalle" SET "FueConteado" = true WHERE "StockContado" <> 0;""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FueConteado",
                table: "InventariosDetalle");
        }
    }
}
