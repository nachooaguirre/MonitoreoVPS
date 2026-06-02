namespace SuperPOS.Shared.Entities.Ventas;

public class Articulo
{
    public int Id { get; set; }
    public string CodigoBarras { get; set; } = string.Empty;      // EAN
    public string CodigoInterno { get; set; } = string.Empty;     // Código interno
    public string CodigoProveedor { get; set; } = string.Empty;   // CodProve
    public string Descripcion { get; set; } = string.Empty;
    public string DescripcionCorta { get; set; } = string.Empty;
    public int IdDepartamento { get; set; }
    public int IdFamilia { get; set; }
    public int IdMarca { get; set; }
    public int IdProveedor { get; set; }

    // Precios
    public decimal PrecioCosto { get; set; }
    public decimal PrecioVenta { get; set; }
    public decimal PrecioOferta { get; set; }
    public decimal MargenGanancia { get; set; }

    // Bonificaciones de compra (hasta 5)
    public decimal Bonificacion1 { get; set; }
    public decimal Bonificacion2 { get; set; }
    public decimal Bonificacion3 { get; set; }
    public decimal Bonificacion4 { get; set; }
    public decimal Bonificacion5 { get; set; }
    public decimal Recargo1 { get; set; }

    // Impuestos
    public decimal AlicuotaIva { get; set; } = 21m;
    public bool AplicaIva { get; set; } = true;
    public decimal ImpuestoInterno { get; set; }

    // Unidades / Presentación
    public decimal UnidadesPorBulto { get; set; } = 1;
    public decimal CajasPorBulto { get; set; } = 1;
    public bool EsPesable { get; set; }
    public int BanderaEAN { get; set; }   // 0=normal, 20=peso, 21=precio

    // Stock
    public decimal StockActual { get; set; }
    public decimal StockMinimo { get; set; }
    public decimal StockMaximo { get; set; }
    public decimal StockDeposito { get; set; }

    // Control
    public bool Activo { get; set; } = true;
    public bool RequiereNroSerie { get; set; }
    public bool RequiereNroLote { get; set; }
    public bool RequiereFechaVencimiento { get; set; }
    /// <summary>Próxima fecha de vencimiento de referencia (lote o alerta) para control en listados.</summary>
    public DateTime? VencimientoReferencia { get; set; }
    public DateTime FechaAlta { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public DateTime? UltimaVenta { get; set; }
    public decimal CantidadVendida { get; set; }

    public Departamento? Departamento { get; set; }
    public Familia? Familia { get; set; }
    public Marca? Marca { get; set; }
    public Proveedor? Proveedor { get; set; }
    public ICollection<ArticuloCodigoBarras> CodigosBarras { get; set; } = [];
}
