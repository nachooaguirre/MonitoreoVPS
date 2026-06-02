using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SuperPOS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddComprasCtaCteCfg : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Compras",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdProveedor = table.Column<int>(type: "integer", nullable: false),
                    IdUsuario = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaVencimiento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NumeroFactura = table.Column<string>(type: "text", nullable: true),
                    LetraFactura = table.Column<string>(type: "text", nullable: true),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    SubTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalIva = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Observaciones = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Compras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Compras_Proveedores_IdProveedor",
                        column: x => x.IdProveedor,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConfiguracionEmpresa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NombreEmpresa = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    NombreFantasia = table.Column<string>(type: "text", nullable: true),
                    Cuit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IngresosBrutos = table.Column<string>(type: "text", nullable: true),
                    Direccion = table.Column<string>(type: "text", nullable: true),
                    Localidad = table.Column<string>(type: "text", nullable: true),
                    Provincia = table.Column<string>(type: "text", nullable: true),
                    Telefono = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    SitioWeb = table.Column<string>(type: "text", nullable: true),
                    PuntoVenta = table.Column<int>(type: "integer", nullable: false),
                    AfipHomologacion = table.Column<bool>(type: "boolean", nullable: false),
                    AfipCertificadoPath = table.Column<string>(type: "text", nullable: true),
                    AfipCertificadoPassword = table.Column<string>(type: "text", nullable: true),
                    ImpresoraFiscalModelo = table.Column<string>(type: "text", nullable: true),
                    ImpresoraFiscalPuerto = table.Column<string>(type: "text", nullable: true),
                    ImpresoraTicketNombre = table.Column<string>(type: "text", nullable: true),
                    MensajePiePagina = table.Column<string>(type: "text", nullable: true),
                    ControlaStock = table.Column<bool>(type: "boolean", nullable: false),
                    PrecioConIva = table.Column<bool>(type: "boolean", nullable: false),
                    BackupRuta = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionEmpresa", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MovimientosCtaCte",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdCliente = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Concepto = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    IdComprobante = table.Column<long>(type: "bigint", nullable: true),
                    IdUsuario = table.Column<int>(type: "integer", nullable: true),
                    Debe = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Haber = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SaldoAcumulado = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosCtaCte", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimientosCtaCte_Clientes_IdCliente",
                        column: x => x.IdCliente,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ComprasDetalle",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdCompra = table.Column<long>(type: "bigint", nullable: false),
                    IdArticulo = table.Column<int>(type: "integer", nullable: false),
                    Cantidad = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    PrecioCosto = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Bonificacion = table.Column<decimal>(type: "numeric(8,4)", nullable: false),
                    PrecioCostoNeto = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    AlicuotaIva = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    SubTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ActualizaPrecio = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComprasDetalle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComprasDetalle_Articulos_IdArticulo",
                        column: x => x.IdArticulo,
                        principalTable: "Articulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComprasDetalle_Compras_IdCompra",
                        column: x => x.IdCompra,
                        principalTable: "Compras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ConfiguracionEmpresa",
                columns: new[] { "Id", "AfipCertificadoPassword", "AfipCertificadoPath", "AfipHomologacion", "BackupRuta", "ControlaStock", "Cuit", "Direccion", "Email", "ImpresoraFiscalModelo", "ImpresoraFiscalPuerto", "ImpresoraTicketNombre", "IngresosBrutos", "Localidad", "MensajePiePagina", "NombreEmpresa", "NombreFantasia", "PrecioConIva", "Provincia", "PuntoVenta", "SitioWeb", "Telefono" },
                values: new object[] { 1, null, null, true, null, true, "00-00000000-0", null, null, null, null, null, null, null, null, "Mi Supermercado", null, true, "Buenos Aires", 1, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_Compras_IdProveedor",
                table: "Compras",
                column: "IdProveedor");

            migrationBuilder.CreateIndex(
                name: "IX_ComprasDetalle_IdArticulo",
                table: "ComprasDetalle",
                column: "IdArticulo");

            migrationBuilder.CreateIndex(
                name: "IX_ComprasDetalle_IdCompra",
                table: "ComprasDetalle",
                column: "IdCompra");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCtaCte_IdCliente_Fecha",
                table: "MovimientosCtaCte",
                columns: new[] { "IdCliente", "Fecha" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComprasDetalle");

            migrationBuilder.DropTable(
                name: "ConfiguracionEmpresa");

            migrationBuilder.DropTable(
                name: "MovimientosCtaCte");

            migrationBuilder.DropTable(
                name: "Compras");
        }
    }
}
