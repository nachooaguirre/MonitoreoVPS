using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SuperPOS.API.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Corrige columnas/tabla que quedaron declaradas en el modelo (y en el snapshot)
    /// pero nunca se generaron en una migracion real, dejando la base desincronizada.
    /// </summary>
    public partial class FixColumnasFaltantesYOfertas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AccesoZebra",
                table: "Usuarios",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "IdArticuloPadre",
                table: "Articulos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MultiplicadorStock",
                table: "Articulos",
                type: "decimal(18,3)",
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<bool>(
                name: "PosnetHabilitado",
                table: "ConfiguracionEmpresa",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PostnetPuertoCom",
                table: "ConfiguracionEmpresa",
                type: "text",
                nullable: false,
                defaultValue: "SIMULADOR");

            migrationBuilder.AddColumn<bool>(
                name: "MpQrHabilitado",
                table: "ConfiguracionEmpresa",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MpAccessToken",
                table: "ConfiguracionEmpresa",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MpCollectorId",
                table: "ConfiguracionEmpresa",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MpStoreId",
                table: "ConfiguracionEmpresa",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MpExternalPosId",
                table: "ConfiguracionEmpresa",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdOrdenCompraOriginal",
                table: "OrdenesCompra",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoDiferencia",
                table: "OrdenesCompra",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObservacionDiferencia",
                table: "OrdenesCompraDetalle",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Ofertas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdArticulo = table.Column<int>(type: "integer", nullable: false),
                    Detalle = table.Column<string>(type: "text", nullable: false),
                    FechaDesde = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaHasta = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PrecioOferta = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    LimiteStock = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    CantidadVendida = table.Column<decimal>(type: "decimal(18,3)", nullable: false, defaultValue: 0m),
                    Activa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ofertas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ofertas_Articulos_IdArticulo",
                        column: x => x.IdArticulo,
                        principalTable: "Articulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Articulos_IdArticuloPadre",
                table: "Articulos",
                column: "IdArticuloPadre");

            migrationBuilder.CreateIndex(
                name: "IX_Ofertas_IdArticulo_FechaDesde_FechaHasta_Activa",
                table: "Ofertas",
                columns: new[] { "IdArticulo", "FechaDesde", "FechaHasta", "Activa" });

            migrationBuilder.AddForeignKey(
                name: "FK_Articulos_Articulos_IdArticuloPadre",
                table: "Articulos",
                column: "IdArticuloPadre",
                principalTable: "Articulos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Articulos_Articulos_IdArticuloPadre",
                table: "Articulos");

            migrationBuilder.DropTable(
                name: "Ofertas");

            migrationBuilder.DropIndex(
                name: "IX_Articulos_IdArticuloPadre",
                table: "Articulos");

            migrationBuilder.DropColumn(name: "AccesoZebra", table: "Usuarios");
            migrationBuilder.DropColumn(name: "IdArticuloPadre", table: "Articulos");
            migrationBuilder.DropColumn(name: "MultiplicadorStock", table: "Articulos");
            migrationBuilder.DropColumn(name: "PosnetHabilitado", table: "ConfiguracionEmpresa");
            migrationBuilder.DropColumn(name: "PostnetPuertoCom", table: "ConfiguracionEmpresa");
            migrationBuilder.DropColumn(name: "MpQrHabilitado", table: "ConfiguracionEmpresa");
            migrationBuilder.DropColumn(name: "MpAccessToken", table: "ConfiguracionEmpresa");
            migrationBuilder.DropColumn(name: "MpCollectorId", table: "ConfiguracionEmpresa");
            migrationBuilder.DropColumn(name: "MpStoreId", table: "ConfiguracionEmpresa");
            migrationBuilder.DropColumn(name: "MpExternalPosId", table: "ConfiguracionEmpresa");
            migrationBuilder.DropColumn(name: "IdOrdenCompraOriginal", table: "OrdenesCompra");
            migrationBuilder.DropColumn(name: "MotivoDiferencia", table: "OrdenesCompra");
            migrationBuilder.DropColumn(name: "ObservacionDiferencia", table: "OrdenesCompraDetalle");
        }
    }
}
