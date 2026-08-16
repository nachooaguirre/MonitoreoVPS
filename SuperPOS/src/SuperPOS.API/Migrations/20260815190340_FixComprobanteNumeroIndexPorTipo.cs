using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SuperPOS.API.Migrations
{
    /// <inheritdoc />
    public partial class FixComprobanteNumeroIndexPorTipo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Comprobantes_IdSucursal_PuntoVenta_Numero_Letra",
                table: "Comprobantes");

            migrationBuilder.CreateIndex(
                name: "IX_Comprobantes_IdSucursal_PuntoVenta_IdTipoComprobante_Numero~",
                table: "Comprobantes",
                columns: new[] { "IdSucursal", "PuntoVenta", "IdTipoComprobante", "Numero", "Letra" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Comprobantes_IdSucursal_PuntoVenta_IdTipoComprobante_Numero~",
                table: "Comprobantes");

            migrationBuilder.CreateIndex(
                name: "IX_Comprobantes_IdSucursal_PuntoVenta_Numero_Letra",
                table: "Comprobantes",
                columns: new[] { "IdSucursal", "PuntoVenta", "Numero", "Letra" },
                unique: true);
        }
    }
}
