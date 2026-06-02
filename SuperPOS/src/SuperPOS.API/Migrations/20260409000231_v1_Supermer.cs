using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SuperPOS.API.Migrations
{
    /// <inheritdoc />
    public partial class v1_Supermer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RazonSocial = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    NombreFantasia = table.Column<string>(type: "text", nullable: true),
                    Cuit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CondicionIva = table.Column<int>(type: "integer", nullable: false),
                    Telefono = table.Column<string>(type: "text", nullable: true),
                    Celular = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Direccion = table.Column<string>(type: "text", nullable: true),
                    Localidad = table.Column<string>(type: "text", nullable: true),
                    CodigoPostal = table.Column<string>(type: "text", nullable: true),
                    Provincia = table.Column<string>(type: "text", nullable: true),
                    IdListaPrecio = table.Column<int>(type: "integer", nullable: false),
                    IdVendedor = table.Column<int>(type: "integer", nullable: true),
                    TieneCtaCte = table.Column<bool>(type: "boolean", nullable: false),
                    LimiteCredito = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SaldoCtaCte = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TipoSaldo = table.Column<char>(type: "character(1)", maxLength: 1, nullable: false),
                    EsMoroso = table.Column<bool>(type: "boolean", nullable: false),
                    DiasVencimientoCtaCte = table.Column<int>(type: "integer", nullable: false),
                    PorcentajeDescuento = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaAlta = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaVtoCtaCte = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Observaciones = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Departamentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departamentos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Marcas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Marcas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MediosPago",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    RequiereReferencia = table.Column<bool>(type: "boolean", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediosPago", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Perfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    EsAdministrador = table.Column<bool>(type: "boolean", nullable: false),
                    PuedeVender = table.Column<bool>(type: "boolean", nullable: false),
                    PuedeAnular = table.Column<bool>(type: "boolean", nullable: false),
                    PuedeHacerDescuentos = table.Column<bool>(type: "boolean", nullable: false),
                    MaximoDescuento = table.Column<decimal>(type: "numeric", nullable: false),
                    PuedeVerPrecios = table.Column<bool>(type: "boolean", nullable: false),
                    PuedeAbrirCaja = table.Column<bool>(type: "boolean", nullable: false),
                    PuedeVerReportes = table.Column<bool>(type: "boolean", nullable: false),
                    PuedeGestionarStock = table.Column<bool>(type: "boolean", nullable: false),
                    PuedeGestionarCompras = table.Column<bool>(type: "boolean", nullable: false),
                    PuedeGestionarClientes = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Perfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Proveedores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RazonSocial = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    NombreFantasia = table.Column<string>(type: "text", nullable: true),
                    Cuit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CondicionIva = table.Column<int>(type: "integer", nullable: false),
                    CodigoProveedor = table.Column<string>(type: "text", nullable: true),
                    Telefono = table.Column<string>(type: "text", nullable: true),
                    Celular = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Direccion = table.Column<string>(type: "text", nullable: true),
                    Localidad = table.Column<string>(type: "text", nullable: true),
                    Provincia = table.Column<string>(type: "text", nullable: true),
                    CodigoPostal = table.Column<string>(type: "text", nullable: true),
                    DiasEntrega = table.Column<int>(type: "integer", nullable: false),
                    DiasVencimientoPago = table.Column<int>(type: "integer", nullable: false),
                    SaldoCtaCte = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Observaciones = table.Column<string>(type: "text", nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaAlta = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proveedores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sucursales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Direccion = table.Column<string>(type: "text", nullable: true),
                    EsCentral = table.Column<bool>(type: "boolean", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sucursales", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TiposComprobante",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Abreviatura = table.Column<string>(type: "text", nullable: true),
                    EsVenta = table.Column<bool>(type: "boolean", nullable: false),
                    EsCompra = table.Column<bool>(type: "boolean", nullable: false),
                    RequiereCAE = table.Column<bool>(type: "boolean", nullable: false),
                    CodigoAfip = table.Column<int>(type: "integer", nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposComprobante", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Familias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    IdDepartamento = table.Column<int>(type: "integer", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Familias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Familias_Departamentos_IdDepartamento",
                        column: x => x.IdDepartamento,
                        principalTable: "Departamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NombreUsuario = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NombreCompleto = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    IdPerfil = table.Column<int>(type: "integer", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaAlta = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UltimoAcceso = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Usuarios_Perfiles_IdPerfil",
                        column: x => x.IdPerfil,
                        principalTable: "Perfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Cajas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    IdSucursal = table.Column<int>(type: "integer", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cajas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cajas_Sucursales_IdSucursal",
                        column: x => x.IdSucursal,
                        principalTable: "Sucursales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Comprobantes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdTipoComprobante = table.Column<int>(type: "integer", nullable: false),
                    Letra = table.Column<char>(type: "character(1)", nullable: false),
                    PuntoVenta = table.Column<int>(type: "integer", nullable: false),
                    Numero = table.Column<long>(type: "bigint", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IdCliente = table.Column<int>(type: "integer", nullable: false),
                    IdCaja = table.Column<int>(type: "integer", nullable: false),
                    IdSucursal = table.Column<int>(type: "integer", nullable: false),
                    IdUsuario = table.Column<int>(type: "integer", nullable: false),
                    SubTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalDescuento = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalIva21 = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalIva105 = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalIva0 = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    EsFacturaElectronica = table.Column<bool>(type: "boolean", nullable: false),
                    CAE = table.Column<long>(type: "bigint", nullable: true),
                    CAEVencimiento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    QrAfip = table.Column<string>(type: "text", nullable: true),
                    Observaciones = table.Column<string>(type: "text", nullable: true),
                    FechaAnulacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IdUsuarioAnulacion = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comprobantes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comprobantes_Clientes_IdCliente",
                        column: x => x.IdCliente,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Comprobantes_TiposComprobante_IdTipoComprobante",
                        column: x => x.IdTipoComprobante,
                        principalTable: "TiposComprobante",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Articulos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CodigoBarras = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CodigoInterno = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CodigoProveedor = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DescripcionCorta = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    IdDepartamento = table.Column<int>(type: "integer", nullable: false),
                    IdFamilia = table.Column<int>(type: "integer", nullable: false),
                    IdMarca = table.Column<int>(type: "integer", nullable: false),
                    IdProveedor = table.Column<int>(type: "integer", nullable: false),
                    PrecioCosto = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    PrecioVenta = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    PrecioOferta = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    MargenGanancia = table.Column<decimal>(type: "numeric(8,4)", nullable: false),
                    Bonificacion1 = table.Column<decimal>(type: "numeric(8,4)", nullable: false),
                    Bonificacion2 = table.Column<decimal>(type: "numeric(8,4)", nullable: false),
                    Bonificacion3 = table.Column<decimal>(type: "numeric(8,4)", nullable: false),
                    Bonificacion4 = table.Column<decimal>(type: "numeric(8,4)", nullable: false),
                    Bonificacion5 = table.Column<decimal>(type: "numeric(8,4)", nullable: false),
                    Recargo1 = table.Column<decimal>(type: "numeric(8,4)", nullable: false),
                    AlicuotaIva = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    AplicaIva = table.Column<bool>(type: "boolean", nullable: false),
                    ImpuestoInterno = table.Column<decimal>(type: "numeric(8,4)", nullable: false),
                    UnidadesPorBulto = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    CajasPorBulto = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    EsPesable = table.Column<bool>(type: "boolean", nullable: false),
                    BanderaEAN = table.Column<int>(type: "integer", nullable: false),
                    StockActual = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    StockMinimo = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    StockMaximo = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    StockDeposito = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    RequiereNroSerie = table.Column<bool>(type: "boolean", nullable: false),
                    RequiereNroLote = table.Column<bool>(type: "boolean", nullable: false),
                    RequiereFechaVencimiento = table.Column<bool>(type: "boolean", nullable: false),
                    FechaAlta = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UltimaVenta = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CantidadVendida = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    DepartamentoId = table.Column<int>(type: "integer", nullable: true),
                    FamiliaId = table.Column<int>(type: "integer", nullable: true),
                    MarcaId = table.Column<int>(type: "integer", nullable: true),
                    ProveedorId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Articulos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Articulos_Departamentos_DepartamentoId",
                        column: x => x.DepartamentoId,
                        principalTable: "Departamentos",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Articulos_Familias_FamiliaId",
                        column: x => x.FamiliaId,
                        principalTable: "Familias",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Articulos_Marcas_MarcaId",
                        column: x => x.MarcaId,
                        principalTable: "Marcas",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Articulos_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedores",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TurnosCaja",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdCaja = table.Column<int>(type: "integer", nullable: false),
                    IdUsuario = table.Column<int>(type: "integer", nullable: false),
                    Apertura = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Cierre = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SaldoInicial = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SaldoFinal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TurnosCaja", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TurnosCaja_Cajas_IdCaja",
                        column: x => x.IdCaja,
                        principalTable: "Cajas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ComprobantesPago",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdComprobante = table.Column<long>(type: "bigint", nullable: false),
                    IdMedioPago = table.Column<int>(type: "integer", nullable: false),
                    Importe = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Referencia = table.Column<string>(type: "text", nullable: true),
                    Vuelto = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComprobantesPago", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComprobantesPago_Comprobantes_IdComprobante",
                        column: x => x.IdComprobante,
                        principalTable: "Comprobantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComprobantesPago_MediosPago_IdMedioPago",
                        column: x => x.IdMedioPago,
                        principalTable: "MediosPago",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ArticulosCodigoBarras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdArticulo = table.Column<int>(type: "integer", nullable: false),
                    CodigoBarras = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EsPrincipal = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticulosCodigoBarras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArticulosCodigoBarras_Articulos_IdArticulo",
                        column: x => x.IdArticulo,
                        principalTable: "Articulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComprobantesDetalle",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdComprobante = table.Column<long>(type: "bigint", nullable: false),
                    IdArticulo = table.Column<int>(type: "integer", nullable: false),
                    Descripcion = table.Column<string>(type: "text", nullable: false),
                    Cantidad = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    PrecioUnitarioSinIva = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    AlicuotaIva = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    MontoIva = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PorcentajeDescuento = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    MontoDescuento = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SubTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    NroSerie = table.Column<string>(type: "text", nullable: true),
                    NroLote = table.Column<string>(type: "text", nullable: true),
                    FechaVencimiento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComprobantesDetalle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComprobantesDetalle_Articulos_IdArticulo",
                        column: x => x.IdArticulo,
                        principalTable: "Articulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComprobantesDetalle_Comprobantes_IdComprobante",
                        column: x => x.IdComprobante,
                        principalTable: "Comprobantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Clientes",
                columns: new[] { "Id", "Activo", "Celular", "CodigoPostal", "CondicionIva", "Cuit", "DiasVencimientoCtaCte", "Direccion", "Email", "EsMoroso", "FechaAlta", "FechaVtoCtaCte", "IdListaPrecio", "IdVendedor", "LimiteCredito", "Localidad", "NombreFantasia", "Observaciones", "PorcentajeDescuento", "Provincia", "RazonSocial", "SaldoCtaCte", "Telefono", "TieneCtaCte", "TipoSaldo" },
                values: new object[] { 1, true, null, null, 5, "00000000000", 30, null, null, false, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, null, 0m, null, null, null, 0m, null, "Consumidor Final", 0m, null, false, 'H' });

            migrationBuilder.InsertData(
                table: "Departamentos",
                columns: new[] { "Id", "Activo", "Nombre" },
                values: new object[] { 1, true, "General" });

            migrationBuilder.InsertData(
                table: "Marcas",
                columns: new[] { "Id", "Activo", "Nombre" },
                values: new object[] { 1, true, "Sin marca" });

            migrationBuilder.InsertData(
                table: "MediosPago",
                columns: new[] { "Id", "Activo", "Nombre", "RequiereReferencia", "Tipo" },
                values: new object[,]
                {
                    { 1, true, "Efectivo", false, 1 },
                    { 2, true, "Tarjeta de Débito", true, 2 },
                    { 3, true, "Tarjeta de Crédito", true, 3 },
                    { 4, true, "MercadoPago", true, 5 },
                    { 5, true, "Transferencia", true, 6 },
                    { 6, true, "Cuenta Corriente", false, 7 },
                    { 7, true, "Cheque", true, 4 }
                });

            migrationBuilder.InsertData(
                table: "Perfiles",
                columns: new[] { "Id", "EsAdministrador", "MaximoDescuento", "Nombre", "PuedeAbrirCaja", "PuedeAnular", "PuedeGestionarClientes", "PuedeGestionarCompras", "PuedeGestionarStock", "PuedeHacerDescuentos", "PuedeVender", "PuedeVerPrecios", "PuedeVerReportes" },
                values: new object[,]
                {
                    { 1, true, 100m, "Administrador", true, true, true, true, true, true, true, true, true },
                    { 2, false, 10m, "Cajero", true, false, false, false, false, true, true, true, false }
                });

            migrationBuilder.InsertData(
                table: "Sucursales",
                columns: new[] { "Id", "Activo", "Direccion", "EsCentral", "Nombre" },
                values: new object[] { 1, true, null, true, "Casa Central" });

            migrationBuilder.InsertData(
                table: "TiposComprobante",
                columns: new[] { "Id", "Abreviatura", "Activo", "CodigoAfip", "EsCompra", "EsVenta", "Nombre", "RequiereCAE" },
                values: new object[,]
                {
                    { 1, "FA", true, 1, false, true, "Factura A", true },
                    { 2, "FB", true, 6, false, true, "Factura B", true },
                    { 3, "FC", true, 11, false, true, "Factura C", true },
                    { 4, "NCA", true, 3, false, true, "Nota de Crédito A", true },
                    { 5, "NCB", true, 8, false, true, "Nota de Crédito B", true },
                    { 6, "NCC", true, 13, false, true, "Nota de Crédito C", true },
                    { 7, "TK", true, null, false, true, "Ticket", false },
                    { 8, "REM", true, null, false, false, "Remito", false },
                    { 9, "PRE", true, null, false, false, "Presupuesto", false }
                });

            migrationBuilder.InsertData(
                table: "Cajas",
                columns: new[] { "Id", "Activo", "IdSucursal", "Nombre" },
                values: new object[] { 1, true, 1, "Caja 1" });

            migrationBuilder.InsertData(
                table: "Familias",
                columns: new[] { "Id", "Activo", "IdDepartamento", "Nombre" },
                values: new object[] { 1, true, 1, "General" });

            migrationBuilder.InsertData(
                table: "Usuarios",
                columns: new[] { "Id", "Activo", "FechaAlta", "IdPerfil", "NombreCompleto", "NombreUsuario", "PasswordHash", "UltimoAcceso" },
                values: new object[] { 1, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, "Administrador", "admin", "admin", null });

            migrationBuilder.CreateIndex(
                name: "IX_Articulos_Activo_IdDepartamento",
                table: "Articulos",
                columns: new[] { "Activo", "IdDepartamento" });

            migrationBuilder.CreateIndex(
                name: "IX_Articulos_CodigoBarras",
                table: "Articulos",
                column: "CodigoBarras");

            migrationBuilder.CreateIndex(
                name: "IX_Articulos_CodigoInterno",
                table: "Articulos",
                column: "CodigoInterno");

            migrationBuilder.CreateIndex(
                name: "IX_Articulos_DepartamentoId",
                table: "Articulos",
                column: "DepartamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Articulos_Descripcion",
                table: "Articulos",
                column: "Descripcion");

            migrationBuilder.CreateIndex(
                name: "IX_Articulos_FamiliaId",
                table: "Articulos",
                column: "FamiliaId");

            migrationBuilder.CreateIndex(
                name: "IX_Articulos_MarcaId",
                table: "Articulos",
                column: "MarcaId");

            migrationBuilder.CreateIndex(
                name: "IX_Articulos_ProveedorId",
                table: "Articulos",
                column: "ProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_ArticulosCodigoBarras_CodigoBarras",
                table: "ArticulosCodigoBarras",
                column: "CodigoBarras");

            migrationBuilder.CreateIndex(
                name: "IX_ArticulosCodigoBarras_IdArticulo",
                table: "ArticulosCodigoBarras",
                column: "IdArticulo");

            migrationBuilder.CreateIndex(
                name: "IX_Cajas_IdSucursal",
                table: "Cajas",
                column: "IdSucursal");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Cuit",
                table: "Clientes",
                column: "Cuit");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_RazonSocial",
                table: "Clientes",
                column: "RazonSocial");

            migrationBuilder.CreateIndex(
                name: "IX_Comprobantes_Fecha",
                table: "Comprobantes",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_Comprobantes_IdCliente",
                table: "Comprobantes",
                column: "IdCliente");

            migrationBuilder.CreateIndex(
                name: "IX_Comprobantes_IdSucursal_PuntoVenta_Numero_Letra",
                table: "Comprobantes",
                columns: new[] { "IdSucursal", "PuntoVenta", "Numero", "Letra" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Comprobantes_IdTipoComprobante",
                table: "Comprobantes",
                column: "IdTipoComprobante");

            migrationBuilder.CreateIndex(
                name: "IX_ComprobantesDetalle_IdArticulo",
                table: "ComprobantesDetalle",
                column: "IdArticulo");

            migrationBuilder.CreateIndex(
                name: "IX_ComprobantesDetalle_IdComprobante",
                table: "ComprobantesDetalle",
                column: "IdComprobante");

            migrationBuilder.CreateIndex(
                name: "IX_ComprobantesPago_IdComprobante",
                table: "ComprobantesPago",
                column: "IdComprobante");

            migrationBuilder.CreateIndex(
                name: "IX_ComprobantesPago_IdMedioPago",
                table: "ComprobantesPago",
                column: "IdMedioPago");

            migrationBuilder.CreateIndex(
                name: "IX_Familias_IdDepartamento",
                table: "Familias",
                column: "IdDepartamento");

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_Cuit",
                table: "Proveedores",
                column: "Cuit");

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_RazonSocial",
                table: "Proveedores",
                column: "RazonSocial");

            migrationBuilder.CreateIndex(
                name: "IX_TurnosCaja_IdCaja",
                table: "TurnosCaja",
                column: "IdCaja");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_IdPerfil",
                table: "Usuarios",
                column: "IdPerfil");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_NombreUsuario",
                table: "Usuarios",
                column: "NombreUsuario",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArticulosCodigoBarras");

            migrationBuilder.DropTable(
                name: "ComprobantesDetalle");

            migrationBuilder.DropTable(
                name: "ComprobantesPago");

            migrationBuilder.DropTable(
                name: "TurnosCaja");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Articulos");

            migrationBuilder.DropTable(
                name: "Comprobantes");

            migrationBuilder.DropTable(
                name: "MediosPago");

            migrationBuilder.DropTable(
                name: "Cajas");

            migrationBuilder.DropTable(
                name: "Perfiles");

            migrationBuilder.DropTable(
                name: "Familias");

            migrationBuilder.DropTable(
                name: "Marcas");

            migrationBuilder.DropTable(
                name: "Proveedores");

            migrationBuilder.DropTable(
                name: "Clientes");

            migrationBuilder.DropTable(
                name: "TiposComprobante");

            migrationBuilder.DropTable(
                name: "Sucursales");

            migrationBuilder.DropTable(
                name: "Departamentos");
        }
    }
}
