using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SuperPOS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddNotasDebitoYConciliacionCompra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Conciliada",
                table: "Compras",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.Sql(@"
                INSERT INTO ""TiposComprobante"" (""Id"", ""Abreviatura"", ""Activo"", ""CodigoAfip"", ""EsCompra"", ""EsVenta"", ""Nombre"", ""RequiereCAE"")
                VALUES
                    (10, 'NDA', true, 2, false, true, 'Nota de Débito A', true),
                    (11, 'NDB', true, 7, false, true, 'Nota de Débito B', true),
                    (12, 'NDC', true, 12, false, true, 'Nota de Débito C', true)
                ON CONFLICT (""Id"") DO NOTHING;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "TiposComprobante",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "TiposComprobante",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "TiposComprobante",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DropColumn(
                name: "Conciliada",
                table: "Compras");
        }
    }
}
