using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SuperPOS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddBancoCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bancos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bancos", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Bancos",
                columns: new[] { "Id", "Activo", "Codigo", "Nombre" },
                values: new object[,]
                {
                    { 1, true, "011", "Banco de la Nación Argentina" },
                    { 2, true, "014", "Banco de la Provincia de Buenos Aires" },
                    { 3, true, "007", "Banco Galicia" },
                    { 4, true, "017", "BBVA Argentina" },
                    { 5, true, "072", "Banco Santander Argentina" },
                    { 6, true, "285", "Banco Macro" },
                    { 7, true, "191", "Banco Credicoop" },
                    { 8, true, "034", "Banco Patagonia" },
                    { 9, true, "029", "Banco Ciudad de Buenos Aires" },
                    { 10, true, "027", "Banco Supervielle" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bancos");
        }
    }
}
