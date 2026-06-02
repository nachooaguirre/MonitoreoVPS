using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SuperPOS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddRemitos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Remitos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NroRemito = table.Column<int>(type: "integer", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IdProveedor = table.Column<int>(type: "integer", nullable: true),
                    IdCliente = table.Column<int>(type: "integer", nullable: true),
                    IdOrdenCompra = table.Column<int>(type: "integer", nullable: true),
                    IdCompra = table.Column<int>(type: "integer", nullable: true),
                    IdUsuario = table.Column<int>(type: "integer", nullable: false),
                    NroRemitoExterno = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Transportista = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Observaciones = table.Column<string>(type: "text", nullable: true),
                    Estado = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Remitos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Remitos_Clientes_IdCliente",
                        column: x => x.IdCliente,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Remitos_Proveedores_IdProveedor",
                        column: x => x.IdProveedor,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RemitosDetalle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdRemito = table.Column<int>(type: "integer", nullable: false),
                    IdArticulo = table.Column<int>(type: "integer", nullable: false),
                    CantidadRemitida = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    CantidadRecibida = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    PrecioCosto = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    LoteNro = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    FechaVencimiento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NroSerie = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Observaciones = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemitosDetalle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RemitosDetalle_Articulos_IdArticulo",
                        column: x => x.IdArticulo,
                        principalTable: "Articulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RemitosDetalle_Remitos_IdRemito",
                        column: x => x.IdRemito,
                        principalTable: "Remitos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Remitos_IdCliente",
                table: "Remitos",
                column: "IdCliente");

            migrationBuilder.CreateIndex(
                name: "IX_Remitos_IdProveedor",
                table: "Remitos",
                column: "IdProveedor");

            migrationBuilder.CreateIndex(
                name: "IX_Remitos_Tipo_NroRemito",
                table: "Remitos",
                columns: new[] { "Tipo", "NroRemito" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RemitosDetalle_IdArticulo",
                table: "RemitosDetalle",
                column: "IdArticulo");

            migrationBuilder.CreateIndex(
                name: "IX_RemitosDetalle_IdRemito",
                table: "RemitosDetalle",
                column: "IdRemito");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RemitosDetalle");

            migrationBuilder.DropTable(
                name: "Remitos");
        }
    }
}
