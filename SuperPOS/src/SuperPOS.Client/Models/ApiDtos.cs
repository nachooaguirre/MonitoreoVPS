namespace SuperPOS.Client.Models;

// CtaCte
public class ClienteCtaCteDto
{
    public int Id { get; set; }
    public string RazonSocial { get; set; } = "";
    public string? NombreFantasia { get; set; }
    public string? Cuit { get; set; }
    public string? Telefono { get; set; }
    public decimal SaldoCtaCte { get; set; }
    public decimal LimiteCredito { get; set; }
    public bool EsMoroso { get; set; }
}

public class MovimientoDto
{
    public long Id { get; set; }
    public DateTime Fecha { get; set; }
    public string Tipo { get; set; } = "";
    public string Concepto { get; set; } = "";
    public decimal Debe { get; set; }
    public decimal Haber { get; set; }
    public decimal SaldoAcumulado { get; set; }
}

public class MovimientosResult
{
    public int Total { get; set; }
    public List<MovimientoDto> Items { get; set; } = [];
}

public class TarjetaInfoDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public bool EsCredito { get; set; }
    public decimal PorcentajeRecargo { get; set; }
    public bool Activo { get; set; }
}

// Sucursales / Puntos de Venta
public class SucursalAdminDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public bool EsCentral { get; set; }
    public string? Direccion { get; set; }
    public bool Activo { get; set; }
    public int CajasActivas { get; set; }
    public int CajasInactivas { get; set; }
}

public class RemitoListItemDto
{
    public int Id { get; set; }
    public int NroRemito { get; set; }
    public DateTime Fecha { get; set; }
    public int Tipo { get; set; }
    public int Estado { get; set; }
    public string? NroRemitoExterno { get; set; }
    public string? Transportista { get; set; }
    public int? IdOrdenCompra { get; set; }
    public int? IdCompra { get; set; }
    public string? ProveedorNombre { get; set; }
    public string? ClienteNombre { get; set; }
    public int CantArticulos { get; set; }
}

public class SucursalSimpleDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
}

public class CajaDisponibleDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public int IdSucursal { get; set; }
    public string SucursalNombre { get; set; } = "";
}

public class CajaEstadoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public bool Activo { get; set; }
    public int IdSucursal { get; set; }
    public string? SucursalNombre { get; set; }
    public DateTime? UltimaVenta { get; set; }
    public bool EnLinea { get; set; }
}

// Calendario de pagos a proveedores
public class CalendarioPagoDto
{
    public long Id { get; set; }
    public string? NumeroFactura { get; set; }
    public string? LetraFactura { get; set; }
    public DateTime Fecha { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public decimal Total { get; set; }
    public int IdProveedor { get; set; }
    public string Proveedor { get; set; } = "";
    public int? DiasParaVencer { get; set; }
    public bool Vencida { get; set; }
}

// Reportes
public class VentasDiaDto
{
    public DateTime Fecha { get; set; }
    public int CantVentas { get; set; }
    public decimal Total { get; set; }
    public decimal TicketPromedio { get; set; }
    public decimal Iva { get; set; }
    public List<MedioPagoResumenDto> PagosPorMedio { get; set; } = [];
}

public class MedioPagoResumenDto
{
    public string MedioPago { get; set; } = "";
    public decimal Total { get; set; }
}

public class VentasPeriodoResult
{
    public decimal TotalPeriodo { get; set; }
    public int CantTotal { get; set; }
    public List<PeriodoDto> Detalle { get; set; } = [];
}

public class PeriodoDto
{
    public string Periodo { get; set; } = "";
    public int CantVentas { get; set; }
    public decimal Total { get; set; }
    public decimal Iva { get; set; }
}

public class RankingProductoDto
{
    public int IdArticulo { get; set; }
    public string Descripcion { get; set; } = "";
    public decimal CantVendida { get; set; }
    public decimal TotalVendido { get; set; }
}

public class RentabilidadProveedoresResult
{
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }
    public List<RentabilidadProveedorDto> Proveedores { get; set; } = [];
}

public class RentabilidadProveedorDto
{
    public int IdProveedor { get; set; }
    public string Proveedor { get; set; } = "";
    public decimal TotalComprado { get; set; }
    public decimal TotalVendido { get; set; }
    public decimal CostoVendido { get; set; }
    public decimal MargenReal { get; set; }
}

public class StockBajoMinimoResult
{
    public int Total { get; set; }
    public List<ArticuloStockDto> Articulos { get; set; } = [];
}

public class ArticuloStockDto
{
    public int Id { get; set; }
    public string? CodigoBarras { get; set; }
    public string Descripcion { get; set; } = "";
    public decimal PrecioCosto { get; set; }
    public decimal PrecioVenta { get; set; }
    public decimal StockActual { get; set; }
    public decimal StockMinimo { get; set; }
    public decimal StockMaximo { get; set; }
    public decimal UnidadesAReponer { get; set; }
}

// Zetas (cierre de caja)
public class ArqueoDto
{
    public DateTime FechaDesde { get; set; }
    public DateTime FechaHasta { get; set; }
    public int NroZetaSiguiente { get; set; }
    public int CantidadVentas { get; set; }
    public decimal TotalVentas { get; set; }
    public decimal TotalIva21 { get; set; }
    public decimal TotalIva105 { get; set; }
    public decimal TotalDescuentos { get; set; }
    public List<DetalleMedioArqueoDto> DetallesMedios { get; set; } = [];
}

public class DetalleMedioArqueoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public int CantOperaciones { get; set; }
    public decimal Total { get; set; }
}

public class ZetaDto
{
    public int Id { get; set; }
    public int NroZeta { get; set; }
    public DateTime FechaApertura { get; set; }
    public DateTime FechaCierre { get; set; }
    public int CantidadVentas { get; set; }
    public decimal TotalVentas { get; set; }
    public decimal TotalIva21 { get; set; }
    public decimal TotalIva105 { get; set; }
    public decimal TotalDescuentos { get; set; }
    public decimal TotalEfectivo { get; set; }
    public decimal EfectivoDeclarado { get; set; }
    public decimal DiferenciaArqueo { get; set; }
}

// Órdenes de Compra
public class OrdenCompraResumenDto
{
    public int Id { get; set; }
    public int NroOrden { get; set; }
    public DateTime Fecha { get; set; }
    public string? ProveedorNombre { get; set; }
    public decimal Total { get; set; }
    public int Estado { get; set; }
    public string EstadoNombre => Estado switch
    {
        0 => "Pendiente",
        1 => "Enviada",
        2 => "Recep. Parcial",
        3 => "Recibida",
        4 => "Anulada",
        5 => "Borrador",
        6 => "Devuelta",
        _ => "?"
    };
}

public class SugerenciaOCDto
{
    public int IdProveedor { get; set; }
    public int CantidadArticulos { get; set; }
    public decimal TotalEstimado { get; set; }
    public List<ItemSugerenciaDto> Items { get; set; } = [];
}

public class ItemSugerenciaDto
{
    public int Id { get; set; }
    public string? CodigoBarras { get; set; }
    public string? CodigoProveedor { get; set; }
    public string Descripcion { get; set; } = "";
    public decimal StockActual { get; set; }
    public decimal StockMinimo { get; set; }
    public decimal StockMaximo { get; set; }
    public decimal CantidadSugerida { get; set; }
    public decimal PrecioCosto { get; set; }
    public decimal SubtotalEstimado { get; set; }
}

// Inventario
public class InventarioResumenDto
{
    public int Id { get; set; }
    public string Descripcion { get; set; } = "";
    public int IdSucursal { get; set; }
    public string? SucursalNombre { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaCierre { get; set; }
    public int Estado { get; set; }
    public string EstadoNombre => Estado switch { 0 => "En Proceso", 1 => "Cerrado", _ => "Aplicado" };
    public int TotalArticulos { get; set; }
    public int ArticulosContados { get; set; }
    public decimal DiferenciaValorizada { get; set; }
}

public class InventarioDetalleDto
{
    public int Id { get; set; }
    public int IdInventario { get; set; }
    public int IdArticulo { get; set; }
    public string? CodigoBarras { get; set; }
    public string Descripcion { get; set; } = "";
    public decimal StockSistema { get; set; }
    public decimal StockContado { get; set; }
    public bool FueConteado { get; set; }
    public decimal Diferencia => StockContado - StockSistema;
    public decimal PrecioCosto { get; set; }
    public decimal DiferenciaValorizada => Diferencia * PrecioCosto;
}

public class InventarioDiferenciasResultDto
{
    public int TotalDiferencias { get; set; }
    public decimal ValorDiferencia { get; set; }
    public List<InventarioDiferenciaFilaDto>? Detalle { get; set; }
}

public class InventarioDiferenciaFilaDto
{
    public int IdArticulo { get; set; }
    public string? Descripcion { get; set; }
    public decimal StockSistema { get; set; }
    public decimal StockContado { get; set; }
    public decimal Diferencia { get; set; }
    public decimal PrecioCosto { get; set; }
    public decimal DiferenciaValorizada { get; set; }
}
