using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SuperPOS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPresupuestosYCotizaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cotizaciones",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Numero = table.Column<long>(type: "bigint", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IdProveedor = table.Column<int>(type: "integer", nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Observacion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PlazoEntrega = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cotizaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cotizaciones_Proveedores_IdProveedor",
                        column: x => x.IdProveedor,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Presupuestos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Numero = table.Column<long>(type: "bigint", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IdCliente = table.Column<int>(type: "integer", nullable: false),
                    IdUsuario = table.Column<int>(type: "integer", nullable: false),
                    IdSucursal = table.Column<int>(type: "integer", nullable: false),
                    PlazoValidezDias = table.Column<int>(type: "integer", nullable: false),
                    Contacto = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Detalle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Observacion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    FormaPago = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SubTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Presupuestos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Presupuestos_Clientes_IdCliente",
                        column: x => x.IdCliente,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Presupuestos_Sucursales_IdSucursal",
                        column: x => x.IdSucursal,
                        principalTable: "Sucursales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Presupuestos_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CotizacionesDetalle",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdCotizacion = table.Column<long>(type: "bigint", nullable: false),
                    IdArticulo = table.Column<int>(type: "integer", nullable: false),
                    Cantidad = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    Precio = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ItemNro = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CotizacionesDetalle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CotizacionesDetalle_Articulos_IdArticulo",
                        column: x => x.IdArticulo,
                        principalTable: "Articulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CotizacionesDetalle_Cotizaciones_IdCotizacion",
                        column: x => x.IdCotizacion,
                        principalTable: "Cotizaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PresupuestosDetalle",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdPresupuesto = table.Column<long>(type: "bigint", nullable: false),
                    IdArticulo = table.Column<int>(type: "integer", nullable: false),
                    ItemNro = table.Column<int>(type: "integer", nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Costo = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Cantidad = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    Precio = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Margen = table.Column<decimal>(type: "numeric(8,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PresupuestosDetalle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PresupuestosDetalle_Articulos_IdArticulo",
                        column: x => x.IdArticulo,
                        principalTable: "Articulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PresupuestosDetalle_Presupuestos_IdPresupuesto",
                        column: x => x.IdPresupuesto,
                        principalTable: "Presupuestos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cotizaciones_IdProveedor",
                table: "Cotizaciones",
                column: "IdProveedor");

            migrationBuilder.CreateIndex(
                name: "IX_CotizacionesDetalle_IdArticulo",
                table: "CotizacionesDetalle",
                column: "IdArticulo");

            migrationBuilder.CreateIndex(
                name: "IX_CotizacionesDetalle_IdCotizacion",
                table: "CotizacionesDetalle",
                column: "IdCotizacion");

            migrationBuilder.CreateIndex(
                name: "IX_Presupuestos_IdCliente",
                table: "Presupuestos",
                column: "IdCliente");

            migrationBuilder.CreateIndex(
                name: "IX_Presupuestos_IdSucursal_Numero",
                table: "Presupuestos",
                columns: new[] { "IdSucursal", "Numero" });

            migrationBuilder.CreateIndex(
                name: "IX_Presupuestos_IdUsuario",
                table: "Presupuestos",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_PresupuestosDetalle_IdArticulo",
                table: "PresupuestosDetalle",
                column: "IdArticulo");

            migrationBuilder.CreateIndex(
                name: "IX_PresupuestosDetalle_IdPresupuesto",
                table: "PresupuestosDetalle",
                column: "IdPresupuesto");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CotizacionesDetalle");

            migrationBuilder.DropTable(
                name: "PresupuestosDetalle");

            migrationBuilder.DropTable(
                name: "Cotizaciones");

            migrationBuilder.DropTable(
                name: "Presupuestos");
        }
    }
}
