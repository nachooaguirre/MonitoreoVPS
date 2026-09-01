namespace SuperPOS.Shared.Entities.Ventas;

/// <summary>
/// Remito de entrega (equivalente a Remitos en Tecnolar)
/// Documenta la entrega de mercadería sin transacción económica.
/// Puede estar asociado a una OC recibida o a una venta con entrega diferida.
/// </summary>
public class Remito
{
    public int Id { get; set; }
    public int NroRemito { get; set; }
    public TipoRemito Tipo { get; set; } = TipoRemito.Entrada;
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public int? IdProveedor { get; set; }
    public int? IdCliente { get; set; }
    public int? IdOrdenCompra { get; set; }
    public int? IdCompra { get; set; }
    public int IdUsuario { get; set; }

    public string? NroRemitoExterno { get; set; }   // Nro del remito del proveedor
    public string? Transportista { get; set; }
    public string? Observaciones { get; set; }
    public EstadoRemito Estado { get; set; } = EstadoRemito.Pendiente;
    /// <summary>True si se generó desde el escaneo de un Zebra (Android) — pendiente de revisión en caja antes de confirmar.</summary>
    public bool GeneradoPorZebra { get; set; }

    /// <summary>Normal, Carne (RG 4256) o Harina (RG 4514) — remitos electrónicos con circuito AFIP/ARBA propio.</summary>
    public SubTipoRemito SubTipo { get; set; } = SubTipoRemito.Normal;

    /// <summary>
    /// Campos del Remito Electrónico Carnico/Harinero (AFIP + ARBA). La solicitud real contra los web
    /// services wsremcarne/wsremharina y el COT de ARBA NO está implementada — requiere WSDL y
    /// credenciales de homologación que todavía no tenemos (ver AfipRemitoCarneGrupo/Tipo y
    /// AfipRemitoHarinaTipo/Embalaje para los catálogos oficiales ya cargados). Estos campos dejan
    /// preparado el modelo para cuando se pueda conectar el circuito real.
    /// </summary>
    public string? CodigoRemitoElectronico { get; set; } // CRE, análogo al CAE
    public DateTime? VencimientoCRE { get; set; }
    public string? CotArba { get; set; } // Código de Operación de Transporte (ARBA)
    public string? TransporteCuitChofer { get; set; }
    public string? TransporteCuitTransportista { get; set; }
    public string? TransporteVehiculoPatente { get; set; }
    public string? TransporteDomicilioDestino { get; set; }

    public Proveedor? Proveedor { get; set; }
    public Cliente? Cliente { get; set; }
    public ICollection<RemitoDetalle> Detalles { get; set; } = [];
}

public class RemitoDetalle
{
    public int Id { get; set; }
    public int IdRemito { get; set; }
    public int IdArticulo { get; set; }
    public decimal CantidadRemitida { get; set; }
    public decimal CantidadRecibida { get; set; }
    public decimal PrecioCosto { get; set; }
    public string? LoteNro { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public string? NroSerie { get; set; }
    public string? Observaciones { get; set; }

    /// <summary>Solo si Remito.SubTipo == Carne: código de corte AFIP (ver AfipRemitoCarneTipo.Codigo) y peso real.</summary>
    public string? AfipCarneCodigoTipo { get; set; }
    public decimal? PesoKg { get; set; }

    /// <summary>Solo si Remito.SubTipo == Harina: tipo de harina y embalaje declarados (ver AfipRemitoHarinaTipo/Embalaje).</summary>
    public int? AfipHarinaIdTipo { get; set; }
    public int? AfipHarinaIdEmbalaje { get; set; }

    public Articulo? Articulo { get; set; }
    public Remito? Remito { get; set; }
}

public enum TipoRemito
{
    Entrada = 0,    // Mercadería que entra (de proveedor)
    Salida = 1      // Mercadería que sale (a cliente, transferencia)
}

public enum SubTipoRemito
{
    Normal = 0,
    Carne = 1,   // Remito Electrónico Cárnico — AFIP RG 4256
    Harina = 2   // Remito Electrónico Harinero — AFIP RG 4514
}

public enum EstadoRemito
{
    Pendiente = 0,
    Confirmado = 1,
    Anulado = 2
}
