using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SuperPOS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddMovimientoCtaCteProveedor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MovimientosCtaCteProveedor",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdProveedor = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Concepto = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    IdCompra = table.Column<long>(type: "bigint", nullable: true),
                    IdUsuario = table.Column<int>(type: "integer", nullable: true),
                    Debe = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Haber = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SaldoAcumulado = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosCtaCteProveedor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimientosCtaCteProveedor_Proveedores_IdProveedor",
                        column: x => x.IdProveedor,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCtaCteProveedor_IdProveedor_Fecha",
                table: "MovimientosCtaCteProveedor",
                columns: new[] { "IdProveedor", "Fecha" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MovimientosCtaCteProveedor");
        }
    }
}
