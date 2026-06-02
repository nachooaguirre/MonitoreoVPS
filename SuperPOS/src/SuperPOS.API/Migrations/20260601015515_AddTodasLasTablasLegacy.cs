using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SuperPOS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddTodasLasTablasLegacy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Auditorias",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Hora = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    TipoCbte = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    NroCbte = table.Column<int>(type: "integer", nullable: true),
                    Tipo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Cantidad = table.Column<decimal>(type: "numeric(18,3)", nullable: true),
                    Importe = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Acumulador = table.Column<int>(type: "integer", nullable: true),
                    EsEnvase = table.Column<bool>(type: "boolean", nullable: false),
                    Zeta = table.Column<int>(type: "integer", nullable: true),
                    CodigoInterno = table.Column<int>(type: "integer", nullable: true),
                    ProcesoStock = table.Column<int>(type: "integer", nullable: true),
                    Cliente = table.Column<int>(type: "integer", nullable: true),
                    Cajero = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Auditorias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CajerosLegacy",
                columns: table => new
                {
                    Codigo = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Clave = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Nivel = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CajerosLegacy", x => x.Codigo);
                });

            migrationBuilder.CreateTable(
                name: "Cupones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NroLinea = table.Column<int>(type: "integer", nullable: false),
                    Texto = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cupones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Encabezados",
                columns: table => new
                {
                    Linea = table.Column<int>(type: "integer", nullable: false),
                    Texto = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Doble = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Encabezados", x => x.Linea);
                });

            migrationBuilder.CreateTable(
                name: "Fantasias",
                columns: table => new
                {
                    Linea = table.Column<int>(type: "integer", nullable: false),
                    Texto = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Doble = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fantasias", x => x.Linea);
                });

            migrationBuilder.CreateTable(
                name: "MonedasLegacy",
                columns: table => new
                {
                    Codigo = table.Column<short>(type: "smallint", nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Cotizacion = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    Acumulador = table.Column<int>(type: "integer", nullable: true),
                    Tipo = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    Cuenta = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    EsDivisa = table.Column<bool>(type: "boolean", nullable: false),
                    Transmitido = table.Column<bool>(type: "boolean", nullable: false),
                    Comision = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    DiasCobro = table.Column<int>(type: "integer", nullable: true),
                    DescripcionImpresion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ImporteRetiro = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    MonedaCompletaRecargo = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonedasLegacy", x => x.Codigo);
                });

            migrationBuilder.CreateTable(
                name: "MonedasPPH",
                columns: table => new
                {
                    Codigo = table.Column<short>(type: "smallint", nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Cotizacion = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    Acumulador = table.Column<int>(type: "integer", nullable: true),
                    Tipo = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    Cuenta = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    EsDivisa = table.Column<bool>(type: "boolean", nullable: false),
                    Transmitido = table.Column<bool>(type: "boolean", nullable: false),
                    Comision = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    DiasCobro = table.Column<int>(type: "integer", nullable: true),
                    DescripcionImpresion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ImporteRetiro = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    MonedaCompletaRecargo = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonedasPPH", x => x.Codigo);
                });

            migrationBuilder.CreateTable(
                name: "Pies",
                columns: table => new
                {
                    Linea = table.Column<int>(type: "integer", nullable: false),
                    Texto = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Doble = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pies", x => x.Linea);
                });

            migrationBuilder.CreateTable(
                name: "POS_Busquedas",
                columns: table => new
                {
                    Busqueda = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Panel = table.Column<int>(type: "integer", nullable: true),
                    PosX = table.Column<int>(type: "integer", nullable: true),
                    PosY = table.Column<int>(type: "integer", nullable: true),
                    Ancho = table.Column<int>(type: "integer", nullable: true),
                    Alto = table.Column<int>(type: "integer", nullable: true),
                    FontSize = table.Column<int>(type: "integer", nullable: true),
                    Tabla = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CampoFoco = table.Column<int>(type: "integer", nullable: true),
                    TipoCargaDatos = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    FiltroBusqueda = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TipoFiltroDatos = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    BusquedaRemota = table.Column<bool>(type: "boolean", nullable: false),
                    Servidor = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Base = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_POS_Busquedas", x => x.Busqueda);
                });

            migrationBuilder.CreateTable(
                name: "POS_BusquedasCampos",
                columns: table => new
                {
                    Busqueda = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Posicion = table.Column<int>(type: "integer", nullable: false),
                    Campo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    AnchoColumna = table.Column<int>(type: "integer", nullable: true),
                    PosX = table.Column<int>(type: "integer", nullable: true),
                    PosY = table.Column<int>(type: "integer", nullable: true),
                    Ancho = table.Column<int>(type: "integer", nullable: true),
                    Alto = table.Column<int>(type: "integer", nullable: true),
                    FontSize = table.Column<int>(type: "integer", nullable: true),
                    PosXEnLista = table.Column<int>(type: "integer", nullable: true),
                    PosYEnLista = table.Column<int>(type: "integer", nullable: true),
                    NroIngreso = table.Column<int>(type: "integer", nullable: true),
                    CaracterComienzoBusqueda = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_POS_BusquedasCampos", x => new { x.Busqueda, x.Posicion });
                });

            migrationBuilder.CreateTable(
                name: "POS_Config",
                columns: table => new
                {
                    NroCaja = table.Column<int>(type: "integer", nullable: false),
                    Animacion = table.Column<bool>(type: "boolean", nullable: false),
                    PanelLogin = table.Column<int>(type: "integer", nullable: true),
                    VentaCantidadMaxima = table.Column<decimal>(type: "numeric(18,3)", nullable: true),
                    VentaImporteMaximo = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    VentaImporteMinimo = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    VentaCantidadDefecto = table.Column<decimal>(type: "numeric(18,3)", nullable: true),
                    VentaCantidadMaximaPagos = table.Column<decimal>(type: "numeric(18,3)", nullable: true),
                    VentaPedirCantidad = table.Column<bool>(type: "boolean", nullable: false),
                    VerVideo = table.Column<bool>(type: "boolean", nullable: false),
                    PanelPrincipal = table.Column<int>(type: "integer", nullable: true),
                    PuertoScanner = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    PuertoDisplay = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    PuertoBalanza = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PuertoFiscal = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    PathImagenesArticulos = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    PathImagenesCajeros = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    FiscalMarca = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    FiscalModelo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ModoCobro = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    NetoMinimoPercepcionIIBB = table.Column<int>(type: "integer", nullable: true),
                    MuestraCodigoEnPantalla = table.Column<bool>(type: "boolean", nullable: false),
                    RendicionGeneraRetiro = table.Column<bool>(type: "boolean", nullable: false),
                    SubtotalObligatorio = table.Column<bool>(type: "boolean", nullable: false),
                    ModoItem = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    ClienteObligatorio = table.Column<bool>(type: "boolean", nullable: false),
                    SucursalFacturacion = table.Column<int>(type: "integer", nullable: true),
                    SucursalFacturacion2 = table.Column<int>(type: "integer", nullable: true),
                    ConfirmaFacturacion = table.Column<bool>(type: "boolean", nullable: false),
                    StockOnLine = table.Column<bool>(type: "boolean", nullable: false),
                    SumaPuntos = table.Column<bool>(type: "boolean", nullable: false),
                    UsaDescripcionLarga = table.Column<bool>(type: "boolean", nullable: false),
                    ClienteFacturaCtaCte = table.Column<bool>(type: "boolean", nullable: false),
                    PuntosXPeso = table.Column<int>(type: "integer", nullable: true),
                    ImprimeEAN = table.Column<bool>(type: "boolean", nullable: false),
                    ZetaObligatoria = table.Column<bool>(type: "boolean", nullable: false),
                    ConfirmaZeta = table.Column<bool>(type: "boolean", nullable: false),
                    ZetaEnviaVenta = table.Column<bool>(type: "boolean", nullable: false),
                    ControlarCajon = table.Column<bool>(type: "boolean", nullable: false),
                    PathImagenes = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PathImagenesServidor = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    VentaImporteMinimoCbte = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    VentaImporteMaximoCbte = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    TruncaPuntos = table.Column<bool>(type: "boolean", nullable: false),
                    PesosXPunto = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    ClienteFacturaPuntos = table.Column<bool>(type: "boolean", nullable: false),
                    ObligarCierreCajero = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_POS_Config", x => x.NroCaja);
                });

            migrationBuilder.CreateTable(
                name: "POS_Cupones",
                columns: table => new
                {
                    NroCupon = table.Column<int>(type: "integer", nullable: false),
                    Linea = table.Column<int>(type: "integer", nullable: false),
                    Texto = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_POS_Cupones", x => new { x.NroCupon, x.Linea });
                });

            migrationBuilder.CreateTable(
                name: "POS_Eventos",
                columns: table => new
                {
                    Evento = table.Column<int>(type: "integer", nullable: false),
                    Detalle = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Tipo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    EjecutarFuncionNro = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_POS_Eventos", x => x.Evento);
                });

            migrationBuilder.CreateTable(
                name: "POS_Formularios",
                columns: table => new
                {
                    NroFormulario = table.Column<int>(type: "integer", nullable: false),
                    PosX = table.Column<int>(type: "integer", nullable: true),
                    PosY = table.Column<int>(type: "integer", nullable: true),
                    Ancho = table.Column<int>(type: "integer", nullable: true),
                    Alto = table.Column<int>(type: "integer", nullable: true),
                    EstadoVentana = table.Column<int>(type: "integer", nullable: true),
                    TipoBorde = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_POS_Formularios", x => x.NroFormulario);
                });

            migrationBuilder.CreateTable(
                name: "POS_Funciones",
                columns: table => new
                {
                    NroFuncion = table.Column<int>(type: "integer", nullable: false),
                    Funcion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Acumulador = table.Column<int>(type: "integer", nullable: true),
                    Descripcion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PosX = table.Column<int>(type: "integer", nullable: true),
                    PosY = table.Column<int>(type: "integer", nullable: true),
                    Ancho = table.Column<int>(type: "integer", nullable: true),
                    Alto = table.Column<int>(type: "integer", nullable: true),
                    Panel = table.Column<int>(type: "integer", nullable: true),
                    MoverPanel = table.Column<int>(type: "integer", nullable: true),
                    MoverPanelPos = table.Column<int>(type: "integer", nullable: true),
                    LlamarFuncion = table.Column<int>(type: "integer", nullable: true),
                    Codigo = table.Column<int>(type: "integer", nullable: true),
                    FontSize = table.Column<int>(type: "integer", nullable: true),
                    FontColor = table.Column<int>(type: "integer", nullable: true),
                    Alineacion = table.Column<int>(type: "integer", nullable: true),
                    Busqueda = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ImporteObligatorio = table.Column<bool>(type: "boolean", nullable: false),
                    Imagen = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    FocoEnIngreso = table.Column<int>(type: "integer", nullable: true),
                    EsEnvase = table.Column<bool>(type: "boolean", nullable: false),
                    PorcentajeMaximo = table.Column<decimal>(type: "numeric(8,4)", nullable: true),
                    Nivel = table.Column<int>(type: "integer", nullable: true),
                    Tiempo = table.Column<int>(type: "integer", nullable: true),
                    EsCtaCte = table.Column<bool>(type: "boolean", nullable: false),
                    AcumuladorVuelto = table.Column<int>(type: "integer", nullable: true),
                    MonedaAjusteCotizacion = table.Column<int>(type: "integer", nullable: true),
                    CantidadCupones = table.Column<int>(type: "integer", nullable: true),
                    Formulario = table.Column<int>(type: "integer", nullable: true),
                    NroEditVariable = table.Column<int>(type: "integer", nullable: true),
                    AbreCajon = table.Column<bool>(type: "boolean", nullable: false),
                    NroCupon = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_POS_Funciones", x => x.NroFuncion);
                });

            migrationBuilder.CreateTable(
                name: "POS_GrillaColumnas",
                columns: table => new
                {
                    Nombre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Titulo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Ancho = table.Column<int>(type: "integer", nullable: true),
                    Alineamiento = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Orden = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_POS_GrillaColumnas", x => x.Nombre);
                });

            migrationBuilder.CreateTable(
                name: "POS_GrillasVenta",
                columns: table => new
                {
                    NroGrilla = table.Column<int>(type: "integer", nullable: false),
                    Panel = table.Column<int>(type: "integer", nullable: true),
                    Descripcion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PosX = table.Column<int>(type: "integer", nullable: true),
                    PosY = table.Column<int>(type: "integer", nullable: true),
                    Ancho = table.Column<int>(type: "integer", nullable: true),
                    Alto = table.Column<int>(type: "integer", nullable: true),
                    FontSize = table.Column<int>(type: "integer", nullable: true),
                    MaximoArticulos = table.Column<int>(type: "integer", nullable: true),
                    ExtraTipo = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    ExtraPanel = table.Column<int>(type: "integer", nullable: true),
                    ExtraPosX = table.Column<int>(type: "integer", nullable: true),
                    ExtraPosY = table.Column<int>(type: "integer", nullable: true),
                    ExtraAncho = table.Column<int>(type: "integer", nullable: true),
                    ExtraAlto = table.Column<int>(type: "integer", nullable: true),
                    DobleClickLlamaFuncion = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_POS_GrillasVenta", x => x.NroGrilla);
                });

            migrationBuilder.CreateTable(
                name: "POS_Imagenes",
                columns: table => new
                {
                    NroImagen = table.Column<int>(type: "integer", nullable: false),
                    CampoContenido = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Panel = table.Column<int>(type: "integer", nullable: true),
                    PosX = table.Column<int>(type: "integer", nullable: true),
                    PosY = table.Column<int>(type: "integer", nullable: true),
                    Ancho = table.Column<int>(type: "integer", nullable: true),
                    Alto = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_POS_Imagenes", x => x.NroImagen);
                });

            migrationBuilder.CreateTable(
                name: "POS_Ingresos",
                columns: table => new
                {
                    NroIngreso = table.Column<int>(type: "integer", nullable: false),
                    Panel = table.Column<int>(type: "integer", nullable: true),
                    Descripcion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PosX = table.Column<int>(type: "integer", nullable: true),
                    PosY = table.Column<int>(type: "integer", nullable: true),
                    Ancho = table.Column<int>(type: "integer", nullable: true),
                    Alto = table.Column<int>(type: "integer", nullable: true),
                    LargoMaximo = table.Column<int>(type: "integer", nullable: true),
                    FontSize = table.Column<int>(type: "integer", nullable: true),
                    PasswordChar = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_POS_Ingresos", x => x.NroIngreso);
                });

            migrationBuilder.CreateTable(
                name: "POS_NumerosComprobantes",
                columns: table => new
                {
                    TipoCbte = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    NroSiguienteCbte = table.Column<int>(type: "integer", nullable: true),
                    FormatoImpresion = table.Column<int>(type: "integer", nullable: true),
                    SumaPuntos = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_POS_NumerosComprobantes", x => x.TipoCbte);
                });

            migrationBuilder.CreateTable(
                name: "POS_Paneles",
                columns: table => new
                {
                    Panel = table.Column<int>(type: "integer", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Ancho = table.Column<int>(type: "integer", nullable: true),
                    Alto = table.Column<int>(type: "integer", nullable: true),
                    GrosorBorde = table.Column<int>(type: "integer", nullable: true),
                    Color = table.Column<int>(type: "integer", nullable: true),
                    Animacion = table.Column<bool>(type: "boolean", nullable: false),
                    Imagen = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Formulario = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_POS_Paneles", x => x.Panel);
                });

            migrationBuilder.CreateTable(
                name: "POS_PanelesPosiciones",
                columns: table => new
                {
                    Panel = table.Column<int>(type: "integer", nullable: false),
                    Posicion = table.Column<int>(type: "integer", nullable: false),
                    PosX = table.Column<int>(type: "integer", nullable: true),
                    PosY = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_POS_PanelesPosiciones", x => new { x.Panel, x.Posicion });
                });

            migrationBuilder.CreateTable(
                name: "POS_Rendiciones",
                columns: table => new
                {
                    Rendicion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Panel = table.Column<int>(type: "integer", nullable: true),
                    PosX = table.Column<int>(type: "integer", nullable: true),
                    PosY = table.Column<int>(type: "integer", nullable: true),
                    Ancho = table.Column<int>(type: "integer", nullable: true),
                    Alto = table.Column<int>(type: "integer", nullable: true),
                    FontSize = table.Column<int>(type: "integer", nullable: true),
                    MuestraImportesCaja = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_POS_Rendiciones", x => x.Rendicion);
                });

            migrationBuilder.CreateTable(
                name: "POS_Teclas",
                columns: table => new
                {
                    IngresoNro = table.Column<int>(type: "integer", nullable: false),
                    Tecla = table.Column<int>(type: "integer", nullable: false),
                    FuncionNro = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_POS_Teclas", x => new { x.IngresoNro, x.Tecla });
                });

            migrationBuilder.CreateTable(
                name: "POS_Videos",
                columns: table => new
                {
                    NroVideo = table.Column<int>(type: "integer", nullable: false),
                    Panel = table.Column<int>(type: "integer", nullable: true),
                    PathContenido = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    PosX = table.Column<int>(type: "integer", nullable: true),
                    PosY = table.Column<int>(type: "integer", nullable: true),
                    Ancho = table.Column<int>(type: "integer", nullable: true),
                    Alto = table.Column<int>(type: "integer", nullable: true),
                    PosicionPanelPlay = table.Column<int>(type: "integer", nullable: true),
                    PathVideosServidor = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_POS_Videos", x => x.NroVideo);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Auditorias_Codigo",
                table: "Auditorias",
                column: "Codigo");

            migrationBuilder.CreateIndex(
                name: "IX_Auditorias_Fecha_Cajero",
                table: "Auditorias",
                columns: new[] { "Fecha", "Cajero" });

            migrationBuilder.CreateIndex(
                name: "IX_POS_Funciones_Panel",
                table: "POS_Funciones",
                column: "Panel");

            migrationBuilder.CreateIndex(
                name: "IX_POS_GrillasVenta_Panel",
                table: "POS_GrillasVenta",
                column: "Panel");

            migrationBuilder.CreateIndex(
                name: "IX_POS_Imagenes_Panel",
                table: "POS_Imagenes",
                column: "Panel");

            migrationBuilder.CreateIndex(
                name: "IX_POS_Ingresos_Panel",
                table: "POS_Ingresos",
                column: "Panel");

            migrationBuilder.CreateIndex(
                name: "IX_POS_Videos_Panel",
                table: "POS_Videos",
                column: "Panel");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Auditorias");

            migrationBuilder.DropTable(
                name: "CajerosLegacy");

            migrationBuilder.DropTable(
                name: "Cupones");

            migrationBuilder.DropTable(
                name: "Encabezados");

            migrationBuilder.DropTable(
                name: "Fantasias");

            migrationBuilder.DropTable(
                name: "MonedasLegacy");

            migrationBuilder.DropTable(
                name: "MonedasPPH");

            migrationBuilder.DropTable(
                name: "Pies");

            migrationBuilder.DropTable(
                name: "POS_Busquedas");

            migrationBuilder.DropTable(
                name: "POS_BusquedasCampos");

            migrationBuilder.DropTable(
                name: "POS_Config");

            migrationBuilder.DropTable(
                name: "POS_Cupones");

            migrationBuilder.DropTable(
                name: "POS_Eventos");

            migrationBuilder.DropTable(
                name: "POS_Formularios");

            migrationBuilder.DropTable(
                name: "POS_Funciones");

            migrationBuilder.DropTable(
                name: "POS_GrillaColumnas");

            migrationBuilder.DropTable(
                name: "POS_GrillasVenta");

            migrationBuilder.DropTable(
                name: "POS_Imagenes");

            migrationBuilder.DropTable(
                name: "POS_Ingresos");

            migrationBuilder.DropTable(
                name: "POS_NumerosComprobantes");

            migrationBuilder.DropTable(
                name: "POS_Paneles");

            migrationBuilder.DropTable(
                name: "POS_PanelesPosiciones");

            migrationBuilder.DropTable(
                name: "POS_Rendiciones");

            migrationBuilder.DropTable(
                name: "POS_Teclas");

            migrationBuilder.DropTable(
                name: "POS_Videos");
        }
    }
}
