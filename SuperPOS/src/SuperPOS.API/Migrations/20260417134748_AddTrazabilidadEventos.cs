using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SuperPOS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddTrazabilidadEventos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrazabilidadEventos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IdArticulo = table.Column<int>(type: "integer", nullable: false),
                    Cantidad = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Ubicacion = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    IdUsuario = table.Column<int>(type: "integer", nullable: true),
                    IdRemito = table.Column<int>(type: "integer", nullable: true),
                    IdRemitoDetalle = table.Column<int>(type: "integer", nullable: true),
                    IdComprobante = table.Column<long>(type: "bigint", nullable: true),
                    IdComprobanteDetalle = table.Column<long>(type: "bigint", nullable: true),
                    IdInventario = table.Column<int>(type: "integer", nullable: true),
                    IdInventarioDetalle = table.Column<int>(type: "integer", nullable: true),
                    LoteNro = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    NroSerie = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    FechaVencimiento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Observaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrazabilidadEventos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrazabilidadEventos_Articulos_IdArticulo",
                        column: x => x.IdArticulo,
                        principalTable: "Articulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrazabilidadEventos_IdArticulo_Fecha",
                table: "TrazabilidadEventos",
                columns: new[] { "IdArticulo", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_TrazabilidadEventos_Tipo_Fecha",
                table: "TrazabilidadEventos",
                columns: new[] { "Tipo", "Fecha" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrazabilidadEventos");
        }
    }
}
