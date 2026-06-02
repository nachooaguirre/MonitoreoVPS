using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SuperPOS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddZetasListasPreciosOCInventario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ListaPrecioId",
                table: "Clientes",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BonificacionesRango",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdArticulo = table.Column<int>(type: "integer", nullable: false),
                    CantidadDesde = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    CantidadHasta = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    PorcentajeDescuento = table.Column<decimal>(type: "numeric(8,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BonificacionesRango", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BonificacionesRango_Articulos_IdArticulo",
                        column: x => x.IdArticulo,
                        principalTable: "Articulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Inventarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IdUsuario = table.Column<int>(type: "integer", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaCierre = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    TotalArticulos = table.Column<int>(type: "integer", nullable: false),
                    ArticulosContados = table.Column<int>(type: "integer", nullable: false),
                    DiferenciaValorizada = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Observaciones = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inventarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ListasPrecios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(8,4)", nullable: false),
                    EsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListasPrecios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrdenesCompra",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdProveedor = table.Column<int>(type: "integer", nullable: false),
                    IdUsuario = table.Column<int>(type: "integer", nullable: false),
                    NroOrden = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaEntregaEsperada = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    TotalSinIva = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalIva = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Observaciones = table.Column<string>(type: "text", nullable: true),
                    FechaRecepcion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IdUsuarioRecepcion = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdenesCompra", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrdenesCompra_Proveedores_IdProveedor",
                        column: x => x.IdProveedor,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Zetas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdCaja = table.Column<int>(type: "integer", nullable: false),
                    IdSucursal = table.Column<int>(type: "integer", nullable: false),
                    IdUsuario = table.Column<int>(type: "integer", nullable: false),
                    NroZeta = table.Column<int>(type: "integer", nullable: false),
                    FechaApertura = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaCierre = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalVentas = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalDescuentos = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalIva21 = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalIva105 = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalIva0 = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CantidadVentas = table.Column<int>(type: "integer", nullable: false),
                    CantidadAnulaciones = table.Column<int>(type: "integer", nullable: false),
                    TotalAnulaciones = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalEfectivo = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalTarjetaDebito = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalTarjetaCredito = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalTransferencia = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalCtaCte = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalMercadoPago = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalOtros = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    EfectivoDeclarado = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DiferenciaArqueo = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Observaciones = table.Column<string>(type: "text", nullable: true),
                    Anulada = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Zetas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventariosDetalle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdInventario = table.Column<int>(type: "integer", nullable: false),
                    IdArticulo = table.Column<int>(type: "integer", nullable: false),
                    StockSistema = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    StockContado = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    PrecioCosto = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    FechaConteo = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Observaciones = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventariosDetalle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventariosDetalle_Articulos_IdArticulo",
                        column: x => x.IdArticulo,
                        principalTable: "Articulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventariosDetalle_Inventarios_IdInventario",
                        column: x => x.IdInventario,
                        principalTable: "Inventarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArticulosPreciosListas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdLista = table.Column<int>(type: "integer", nullable: false),
                    IdArticulo = table.Column<int>(type: "integer", nullable: false),
                    Precio = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    PorcentajeAjuste = table.Column<decimal>(type: "numeric(8,4)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticulosPreciosListas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArticulosPreciosListas_Articulos_IdArticulo",
                        column: x => x.IdArticulo,
                        principalTable: "Articulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArticulosPreciosListas_ListasPrecios_IdLista",
                        column: x => x.IdLista,
                        principalTable: "ListasPrecios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrdenesCompraDetalle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdOrdenCompra = table.Column<int>(type: "integer", nullable: false),
                    IdArticulo = table.Column<int>(type: "integer", nullable: false),
                    CantidadPedida = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    CantidadRecibida = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    PrecioCosto = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    AlicuotaIva = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdenesCompraDetalle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrdenesCompraDetalle_Articulos_IdArticulo",
                        column: x => x.IdArticulo,
                        principalTable: "Articulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdenesCompraDetalle_OrdenesCompra_IdOrdenCompra",
                        column: x => x.IdOrdenCompra,
                        principalTable: "OrdenesCompra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ZetasDetalleMedio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdZeta = table.Column<int>(type: "integer", nullable: false),
                    IdMedioPago = table.Column<int>(type: "integer", nullable: false),
                    NombreMedio = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CantOperaciones = table.Column<int>(type: "integer", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZetasDetalleMedio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ZetasDetalleMedio_Zetas_IdZeta",
                        column: x => x.IdZeta,
                        principalTable: "Zetas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Clientes",
                keyColumn: "Id",
                keyValue: 1,
                column: "ListaPrecioId",
                value: null);

            migrationBuilder.UpdateData(
                table: "ConfiguracionEmpresa",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "MensajePiePagina", "NombreEmpresa", "NombreFantasia" },
                values: new object[] { "¡Gracias por su compra! Vuelva pronto.", "Los Angeles Supermercados", "Supermercados Los Angeles" });

            migrationBuilder.InsertData(
                table: "ListasPrecios",
                columns: new[] { "Id", "Activo", "Descripcion", "EsDefault", "Nombre", "Tipo", "Valor" },
                values: new object[,]
                {
                    { 1, true, "Precio público general", true, "Minorista", 0, 0m },
                    { 2, true, "Precio especial por volumen", false, "Mayorista", 0, -10m },
                    { 3, true, "Precio para empleados", false, "Empleados", 0, -15m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_ListaPrecioId",
                table: "Clientes",
                column: "ListaPrecioId");

            migrationBuilder.CreateIndex(
                name: "IX_ArticulosPreciosListas_IdArticulo",
                table: "ArticulosPreciosListas",
                column: "IdArticulo");

            migrationBuilder.CreateIndex(
                name: "IX_ArticulosPreciosListas_IdLista_IdArticulo",
                table: "ArticulosPreciosListas",
                columns: new[] { "IdLista", "IdArticulo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BonificacionesRango_IdArticulo",
                table: "BonificacionesRango",
                column: "IdArticulo");

            migrationBuilder.CreateIndex(
                name: "IX_InventariosDetalle_IdArticulo",
                table: "InventariosDetalle",
                column: "IdArticulo");

            migrationBuilder.CreateIndex(
                name: "IX_InventariosDetalle_IdInventario",
                table: "InventariosDetalle",
                column: "IdInventario");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesCompra_IdProveedor",
                table: "OrdenesCompra",
                column: "IdProveedor");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesCompraDetalle_IdArticulo",
                table: "OrdenesCompraDetalle",
                column: "IdArticulo");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesCompraDetalle_IdOrdenCompra",
                table: "OrdenesCompraDetalle",
                column: "IdOrdenCompra");

            migrationBuilder.CreateIndex(
                name: "IX_Zetas_IdCaja_NroZeta",
                table: "Zetas",
                columns: new[] { "IdCaja", "NroZeta" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ZetasDetalleMedio_IdZeta",
                table: "ZetasDetalleMedio",
                column: "IdZeta");

            migrationBuilder.AddForeignKey(
                name: "FK_Clientes_ListasPrecios_ListaPrecioId",
                table: "Clientes",
                column: "ListaPrecioId",
                principalTable: "ListasPrecios",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clientes_ListasPrecios_ListaPrecioId",
                table: "Clientes");

            migrationBuilder.DropTable(
                name: "ArticulosPreciosListas");

            migrationBuilder.DropTable(
                name: "BonificacionesRango");

            migrationBuilder.DropTable(
                name: "InventariosDetalle");

            migrationBuilder.DropTable(
                name: "OrdenesCompraDetalle");

            migrationBuilder.DropTable(
                name: "ZetasDetalleMedio");

            migrationBuilder.DropTable(
                name: "ListasPrecios");

            migrationBuilder.DropTable(
                name: "Inventarios");

            migrationBuilder.DropTable(
                name: "OrdenesCompra");

            migrationBuilder.DropTable(
                name: "Zetas");

            migrationBuilder.DropIndex(
                name: "IX_Clientes_ListaPrecioId",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "ListaPrecioId",
                table: "Clientes");

            migrationBuilder.UpdateData(
                table: "ConfiguracionEmpresa",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "MensajePiePagina", "NombreEmpresa", "NombreFantasia" },
                values: new object[] { null, "Mi Supermercado", null });
        }
    }
}
