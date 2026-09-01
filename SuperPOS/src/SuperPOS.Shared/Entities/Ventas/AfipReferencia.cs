namespace SuperPOS.Shared.Entities.Ventas;

/// <summary>Grupo de especie/corte del Remito Electronico Carnico (AFIP RG 4256). Catalogo oficial, no editable por el usuario.</summary>
public class AfipRemitoCarneGrupo
{
    public int Id { get; set; }
    public string Descripcion { get; set; } = string.Empty;
}

/// <summary>Tipo de corte dentro de un grupo (codigo AFIP formato "grupo.item", ej. "2.14"). Ver <see cref="AfipRemitoCarneGrupo"/>.</summary>
public class AfipRemitoCarneTipo
{
    public string Codigo { get; set; } = string.Empty;
    public int IdGrupo { get; set; }
    public string Descripcion { get; set; } = string.Empty;

    public AfipRemitoCarneGrupo? Grupo { get; set; }
}

/// <summary>Tipo de harina/subproducto de molienda para el Remito Electronico Harinero (AFIP RG 4514).</summary>
public class AfipRemitoHarinaTipo
{
    public int Id { get; set; }
    public string Descripcion { get; set; } = string.Empty;
}

/// <summary>Tipo de embalaje/envase del despacho de harina (Remito Electronico Harinero).</summary>
public class AfipRemitoHarinaEmbalaje
{
    public int Id { get; set; }
    public string Descripcion { get; set; } = string.Empty;
}

/// <summary>Catalogo de entidades financieras (codigo BCRA) para medios de pago con banco (cheque, transferencia).</summary>
public class BancoArgentino
{
    public int CodigoBcra { get; set; }
    public string RazonSocial { get; set; } = string.Empty;
    public string AliasCorto { get; set; } = string.Empty;
}

/// <summary>
/// Combinacion valida (regimen, concepto, subcodigo) del regimen de retenciones SICORE de AFIP.
/// Es una matriz de validacion pura: la fuente relevada no trae descripciones de cada codigo (las
/// tablas oficiales de AFIP las tienen, pero no estaban disponibles en este relevamiento). Sirve para
/// validar que una retencion configurada a un proveedor corresponda a una combinacion real antes de
/// generar el certificado — no reemplaza el calculo de la retencion en si, que depende del regimen que
/// AFIP le haya asignado al comercio (ver preguntas al cliente en Analisis_Gecom_AFIP_Remitos.pdf).
/// </summary>
public class SicoreRegimenConcepto
{
    public int Id { get; set; }
    public int CodigoRegimen { get; set; }
    public int CodigoConcepto { get; set; }
    public int Subcodigo { get; set; }
}
