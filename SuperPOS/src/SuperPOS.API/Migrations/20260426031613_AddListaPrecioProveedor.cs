using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SuperPOS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddListaPrecioProveedor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ListasPrecioProveedor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdProveedor = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Notas = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    FechaCargaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ArchivoOrigenNombre = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                    ArchivoOrigenRutaRelativa = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListasPrecioProveedor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ListasPrecioProveedor_Proveedores_IdProveedor",
                        column: x => x.IdProveedor,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ListasPrecioProveedorLineas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdLista = table.Column<int>(type: "integer", nullable: false),
                    IdArticulo = table.Column<int>(type: "integer", nullable: true),
                    CodigoProveedor = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    IvaPorcentaje = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    BonificacionesJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListasPrecioProveedorLineas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ListasPrecioProveedorLineas_Articulos_IdArticulo",
                        column: x => x.IdArticulo,
                        principalTable: "Articulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ListasPrecioProveedorLineas_ListasPrecioProveedor_IdLista",
                        column: x => x.IdLista,
                        principalTable: "ListasPrecioProveedor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ListasPrecioProveedor_IdProveedor_Nombre",
                table: "ListasPrecioProveedor",
                columns: new[] { "IdProveedor", "Nombre" });

            migrationBuilder.CreateIndex(
                name: "IX_ListasPrecioProveedorLineas_IdArticulo",
                table: "ListasPrecioProveedorLineas",
                column: "IdArticulo");

            migrationBuilder.CreateIndex(
                name: "IX_ListasPrecioProveedorLineas_IdLista_CodigoProveedor",
                table: "ListasPrecioProveedorLineas",
                columns: new[] { "IdLista", "CodigoProveedor" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ListasPrecioProveedorLineas");

            migrationBuilder.DropTable(
                name: "ListasPrecioProveedor");
        }
    }
}
