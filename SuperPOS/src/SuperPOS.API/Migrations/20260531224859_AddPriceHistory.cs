using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SuperPOS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HistorialPrecios",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdArticulo = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IdUsuario = table.Column<int>(type: "integer", nullable: true),
                    IdSucursal = table.Column<int>(type: "integer", nullable: true),
                    PrecioAnterior = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    PrecioNuevo = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Campo = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialPrecios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialPrecios_Articulos_IdArticulo",
                        column: x => x.IdArticulo,
                        principalTable: "Articulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HistorialPrecios_Sucursales_IdSucursal",
                        column: x => x.IdSucursal,
                        principalTable: "Sucursales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistorialPrecios_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HistorialPrecios_IdArticulo",
                table: "HistorialPrecios",
                column: "IdArticulo");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialPrecios_IdArticulo_Fecha",
                table: "HistorialPrecios",
                columns: new[] { "IdArticulo", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_HistorialPrecios_IdSucursal",
                table: "HistorialPrecios",
                column: "IdSucursal");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialPrecios_IdUsuario",
                table: "HistorialPrecios",
                column: "IdUsuario");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistorialPrecios");
        }
    }
}
