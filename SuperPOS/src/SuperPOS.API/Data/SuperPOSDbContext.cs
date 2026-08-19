using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SuperPOS.Shared.Entities.Ventas;
using SuperPOS.Shared.Entities.Ventas.Legacy;

namespace SuperPOS.API.Data;

public class SuperPOSDbContext(DbContextOptions<SuperPOSDbContext> options, IHttpContextAccessor? httpContextAccessor = null) : DbContext(options)
{
    public DbSet<Articulo> Articulos => Set<Articulo>();
    public DbSet<ArticuloCodigoBarras> ArticulosCodigoBarras => Set<ArticuloCodigoBarras>();
    public DbSet<Departamento> Departamentos => Set<Departamento>();
    public DbSet<Familia> Familias => Set<Familia>();
    public DbSet<Marca> Marcas => Set<Marca>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<Comprobante> Comprobantes => Set<Comprobante>();
    public DbSet<ComprobanteDetalle> ComprobantesDetalle => Set<ComprobanteDetalle>();
    public DbSet<ComprobantePago> ComprobantesPago => Set<ComprobantePago>();
    public DbSet<TipoComprobante> TiposComprobante => Set<TipoComprobante>();
    public DbSet<MedioPago> MediosPago => Set<MedioPago>();
    public DbSet<TarjetaMarca> TarjetasMarca => Set<TarjetaMarca>();
    public DbSet<Caja> Cajas => Set<Caja>();
    public DbSet<Sucursal> Sucursales => Set<Sucursal>();
    public DbSet<TurnoCaja> TurnosCaja => Set<TurnoCaja>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Perfil> Perfiles => Set<Perfil>();
    public DbSet<Compra> Compras => Set<Compra>();
    public DbSet<CompraDetalle> ComprasDetalle => Set<CompraDetalle>();
    public DbSet<MovimientoCtaCte> MovimientosCtaCte => Set<MovimientoCtaCte>();
    public DbSet<MovimientoCtaCteProveedor> MovimientosCtaCteProveedor => Set<MovimientoCtaCteProveedor>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ComprobanteAfipLog> ComprobantesAfipLog => Set<ComprobanteAfipLog>();
    public DbSet<ConfiguracionEmpresa> ConfiguracionEmpresa => Set<ConfiguracionEmpresa>();
    public DbSet<Zeta> Zetas => Set<Zeta>();
    public DbSet<ZetaDetalleMedio> ZetasDetalleMedio => Set<ZetaDetalleMedio>();
    public DbSet<ListaPrecio> ListasPrecios => Set<ListaPrecio>();
    public DbSet<ArticuloPrecioLista> ArticulosPreciosListas => Set<ArticuloPrecioLista>();
    public DbSet<BonificacionRango> BonificacionesRango => Set<BonificacionRango>();
    public DbSet<ListaPrecioProveedor> ListasPrecioProveedor => Set<ListaPrecioProveedor>();
    public DbSet<ListaPrecioProveedorLinea> ListasPrecioProveedorLineas => Set<ListaPrecioProveedorLinea>();
    public DbSet<OrdenCompra> OrdenesCompra => Set<OrdenCompra>();
    public DbSet<OrdenCompraDetalle> OrdenesCompraDetalle => Set<OrdenCompraDetalle>();
    public DbSet<Inventario> Inventarios => Set<Inventario>();
    public DbSet<InventarioDetalle> InventariosDetalle => Set<InventarioDetalle>();
    public DbSet<Remito> Remitos => Set<Remito>();
    public DbSet<RemitoDetalle> RemitosDetalle => Set<RemitoDetalle>();
    public DbSet<ArticuloStockSucursal> ArticulosStockPorSucursal => Set<ArticuloStockSucursal>();
    public DbSet<TransferenciaInterna> TransferenciasInternas => Set<TransferenciaInterna>();
    public DbSet<TransferenciaInternaDetalle> TransferenciasInternasDetalle => Set<TransferenciaInternaDetalle>();
    public DbSet<CuentaTesoreria> CuentasTesoreria => Set<CuentaTesoreria>();
    public DbSet<Chequera> Chequeras => Set<Chequera>();
    public DbSet<Banco> Bancos => Set<Banco>();
    public DbSet<MovimientoTesoreria> MovimientosTesoreria => Set<MovimientoTesoreria>();
    public DbSet<Cheque> Cheques => Set<Cheque>();
    public DbSet<GastoCaja> GastosCaja => Set<GastoCaja>();
    public DbSet<TrazabilidadEvento> TrazabilidadEventos => Set<TrazabilidadEvento>();
    public DbSet<Alerta> Alertas => Set<Alerta>();
    public DbSet<AlertaUsuario> AlertasUsuarios => Set<AlertaUsuario>();
    public DbSet<AlertaEnviada> AlertasEnviadas => Set<AlertaEnviada>();
    public DbSet<Mailing> Mailings => Set<Mailing>();
    public DbSet<HistorialPrecio> HistorialPrecios => Set<HistorialPrecio>();
    public DbSet<BonificacionFecha> BonificacionesFecha => Set<BonificacionFecha>();
    public DbSet<Presupuesto> Presupuestos => Set<Presupuesto>();
    public DbSet<PresupuestoDetalle> PresupuestosDetalle => Set<PresupuestoDetalle>();
    public DbSet<Cotizacion> Cotizaciones => Set<Cotizacion>();
    public DbSet<CotizacionDetalle> CotizacionesDetalle => Set<CotizacionDetalle>();
    public DbSet<Promocion> Promociones => Set<Promocion>();
    public DbSet<PromocionCondicion> PromocionesCondiciones => Set<PromocionCondicion>();
    public DbSet<PromocionParametroAccion> PromocionesParametrosAccion => Set<PromocionParametroAccion>();
    public DbSet<MonedaPlan> MonedasPlanes => Set<MonedaPlan>();
    public DbSet<ArticuloDatoAdicional> ArticulosDatosAdicionales => Set<ArticuloDatoAdicional>();
    public DbSet<EtiquetaCola> EtiquetasCola => Set<EtiquetaCola>();
    public DbSet<Oferta> Ofertas => Set<Oferta>();

    // Tablas Legacy
    public DbSet<Auditoria> Auditorias => Set<Auditoria>();
    public DbSet<CajeroLegacy> CajerosLegacy => Set<CajeroLegacy>();
    public DbSet<Cupon> Cupones => Set<Cupon>();
    public DbSet<Encabezado> Encabezados => Set<Encabezado>();
    public DbSet<Fantasia> Fantasias => Set<Fantasia>();
    public DbSet<Moneda> MonedasLegacy => Set<Moneda>();
    public DbSet<MonedaPPH> MonedasPPH => Set<MonedaPPH>();
    public DbSet<Pie> Pies => Set<Pie>();
    public DbSet<POS_Busqueda> POS_Busquedas => Set<POS_Busqueda>();
    public DbSet<POS_BusquedaCampo> POS_BusquedasCampos => Set<POS_BusquedaCampo>();
    public DbSet<POS_Config> POS_Config => Set<POS_Config>();
    public DbSet<POS_Cupon> POS_Cupones => Set<POS_Cupon>();
    public DbSet<POS_Evento> POS_Eventos => Set<POS_Evento>();
    public DbSet<POS_Formulario> POS_Formularios => Set<POS_Formulario>();
    public DbSet<POS_Funcion> POS_Funciones => Set<POS_Funcion>();
    public DbSet<POS_GrillaColumna> POS_GrillaColumnas => Set<POS_GrillaColumna>();
    public DbSet<POS_GrillaVenta> POS_GrillasVenta => Set<POS_GrillaVenta>();
    public DbSet<POS_Imagen> POS_Imagenes => Set<POS_Imagen>();
    public DbSet<POS_Ingreso> POS_Ingresos => Set<POS_Ingreso>();
    public DbSet<POS_NumeroComprobante> POS_NumerosComprobantes => Set<POS_NumeroComprobante>();
    public DbSet<POS_Panel> POS_Paneles => Set<POS_Panel>();
    public DbSet<POS_PanelPosicion> POS_PanelesPosiciones => Set<POS_PanelPosicion>();
    public DbSet<POS_Rendicion> POS_Rendiciones => Set<POS_Rendicion>();
    public DbSet<POS_Tecla> POS_Teclas => Set<POS_Tecla>();
    public DbSet<POS_Video> POS_Videos => Set<POS_Video>();

    /// <summary>
    /// Namespaces/entidades que no se auditan: el propio AuditLog (evita recursión) y las tablas
    /// Legacy de Tecnolar (el importador masivo generaría miles de filas de ruido sin valor).
    /// </summary>
    private static bool EsAuditable(EntityEntry entry) =>
        entry.Entity is not AuditLog
        && entry.Entity.GetType().Namespace != typeof(CajeroLegacy).Namespace;

    public override int SaveChanges() => SaveChangesAsync().GetAwaiter().GetResult();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var pendientes = ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(EsAuditable)
            .ToList();

        // Para "Added" el PK todavía no existe (lo asigna la DB); armamos el AuditLog después del SaveChanges real.
        var pendientesInfo = pendientes.Select(e => new
        {
            Entry = e,
            e.State,
            Cambios = e.State == EntityState.Modified
                ? e.Properties.Where(p => !Equals(p.OriginalValue, p.CurrentValue))
                    .ToDictionary(p => p.Metadata.Name, p => new { anterior = p.OriginalValue, nuevo = p.CurrentValue })
                : null
        }).ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        if (pendientesInfo.Count > 0)
        {
            var (idUsuario, nombreUsuario) = ObtenerUsuarioActual();
            var ahora = DateTime.UtcNow;

            foreach (var p in pendientesInfo)
            {
                if (p.State == EntityState.Modified && (p.Cambios is null || p.Cambios.Count == 0))
                    continue; // Nada realmente cambió (ej. SaveChanges llamado sin modificaciones reales)

                var tipo = p.State switch
                {
                    EntityState.Added => TipoAccionAuditoria.Creacion,
                    EntityState.Deleted => TipoAccionAuditoria.Eliminacion,
                    _ => TipoAccionAuditoria.Modificacion
                };

                var pk = string.Join(",", p.Entry.Properties.Where(x => x.Metadata.IsPrimaryKey()).Select(x => x.CurrentValue));
                var entidad = p.Entry.Entity.GetType().Name;

                string? cambiosJson = null;
                string? descripcion = null;
                if (p.State == EntityState.Modified && p.Cambios != null)
                {
                    cambiosJson = JsonSerializer.Serialize(p.Cambios);
                    descripcion = string.Join("; ", p.Cambios.Select(c => $"{c.Key}: {c.Value.anterior} -> {c.Value.nuevo}"));
                    if (descripcion.Length > 950) descripcion = descripcion[..950] + "…";
                }

                AuditLogs.Add(new AuditLog
                {
                    Fecha = ahora,
                    IdUsuario = idUsuario,
                    NombreUsuario = nombreUsuario,
                    Entidad = entidad,
                    EntidadId = pk,
                    Accion = tipo,
                    Descripcion = descripcion,
                    CambiosJson = cambiosJson
                });
            }

            await base.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    private (int? idUsuario, string? nombreUsuario) ObtenerUsuarioActual()
    {
        var user = httpContextAccessor?.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true) return (null, null);

        var idStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var nombre = user.FindFirstValue(ClaimTypes.Name);
        return (int.TryParse(idStr, out var id) ? id : null, nombre);
    }

    protected override void OnModelCreating(ModelBuilder m)
    {
        base.OnModelCreating(m);

        m.Entity<Articulo>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.CodigoBarras).HasMaxLength(50);
            e.Property(x => x.CodigoInterno).HasMaxLength(50);
            e.Property(x => x.CodigoProveedor).HasMaxLength(50);
            e.Property(x => x.Descripcion).HasMaxLength(200).IsRequired();
            e.Property(x => x.DescripcionCorta).HasMaxLength(40);
            e.Property(x => x.PrecioVenta).HasColumnType("decimal(18,4)");
            e.Property(x => x.PrecioCosto).HasColumnType("decimal(18,4)");
            e.Property(x => x.PrecioOferta).HasColumnType("decimal(18,4)");
            e.Property(x => x.MargenGanancia).HasColumnType("decimal(8,4)");
            e.Property(x => x.AlicuotaIva).HasColumnType("decimal(5,2)");
            e.Property(x => x.ImpuestoInterno).HasColumnType("decimal(8,4)");
            e.Property(x => x.Bonificacion1).HasColumnType("decimal(8,4)");
            e.Property(x => x.Bonificacion2).HasColumnType("decimal(8,4)");
            e.Property(x => x.Bonificacion3).HasColumnType("decimal(8,4)");
            e.Property(x => x.Bonificacion4).HasColumnType("decimal(8,4)");
            e.Property(x => x.Bonificacion5).HasColumnType("decimal(8,4)");
            e.Property(x => x.Recargo1).HasColumnType("decimal(8,4)");
            e.Property(x => x.UnidadesPorBulto).HasColumnType("decimal(10,3)");
            e.Property(x => x.CajasPorBulto).HasColumnType("decimal(10,3)");
            e.Property(x => x.StockActual).HasColumnType("decimal(18,3)");
            e.Property(x => x.StockMinimo).HasColumnType("decimal(18,3)");
            e.Property(x => x.StockMaximo).HasColumnType("decimal(18,3)");
            e.Property(x => x.StockDeposito).HasColumnType("decimal(18,3)");
            e.Property(x => x.CantidadVendida).HasColumnType("decimal(18,3)");
            e.Property(x => x.VencimientoReferencia).HasColumnType("timestamp with time zone");
            e.HasIndex(x => x.CodigoBarras);
            e.HasIndex(x => x.CodigoInterno);
            e.HasIndex(x => x.Descripcion);
            e.HasIndex(x => new { x.Activo, x.IdDepartamento });
            e.HasOne(x => x.Departamento).WithMany().HasForeignKey(x => x.IdDepartamento).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Familia).WithMany().HasForeignKey(x => x.IdFamilia).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Marca).WithMany().HasForeignKey(x => x.IdMarca).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Proveedor).WithMany().HasForeignKey(x => x.IdProveedor).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.MultiplicadorStock).HasColumnType("decimal(18,3)").HasDefaultValue(1m);
            e.HasOne(x => x.ArticuloPadre).WithMany().HasForeignKey(x => x.IdArticuloPadre).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.ContenidoValor).HasColumnType("decimal(18,4)").HasDefaultValue(1m);
            e.Property(x => x.ContenidoUnidad).HasMaxLength(10).HasDefaultValue("UN");
        });

        m.Entity<ArticuloCodigoBarras>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.CodigoBarras).HasMaxLength(50).IsRequired();
            e.HasOne(x => x.Articulo).WithMany(a => a.CodigosBarras).HasForeignKey(x => x.IdArticulo);
            e.HasIndex(x => x.CodigoBarras);
        });

        m.Entity<EtiquetaCola>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Articulo).WithMany().HasForeignKey(x => x.IdArticulo).OnDelete(DeleteBehavior.Cascade);
        });

        m.Entity<Oferta>(e =>
        {
            e.ToTable("Ofertas");
            e.HasKey(x => x.Id);
            e.Property(x => x.PrecioOferta).HasColumnType("decimal(18,4)");
            e.Property(x => x.LimiteStock).HasColumnType("decimal(18,3)");
            e.Property(x => x.CantidadVendida).HasColumnType("decimal(18,3)");
            e.HasOne(x => x.Articulo).WithMany().HasForeignKey(x => x.IdArticulo).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.IdArticulo, x.FechaDesde, x.FechaHasta, x.Activa });
        });

        m.Entity<TrazabilidadEvento>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Cantidad).HasColumnType("decimal(18,3)");
            e.Property(x => x.Ubicacion).HasMaxLength(120);
            e.Property(x => x.LoteNro).HasMaxLength(80);
            e.Property(x => x.NroSerie).HasMaxLength(80);
            e.Property(x => x.Observaciones).HasMaxLength(500);
            e.HasOne(x => x.Articulo).WithMany().HasForeignKey(x => x.IdArticulo).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.IdArticulo, x.Fecha });
            e.HasIndex(x => new { x.Tipo, x.Fecha });
        });

        m.Entity<Cliente>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.RazonSocial).HasMaxLength(150).IsRequired();
            e.Property(x => x.Cuit).HasMaxLength(20);
            e.Property(x => x.LimiteCredito).HasColumnType("decimal(18,2)");
            e.Property(x => x.SaldoCtaCte).HasColumnType("decimal(18,2)");
            e.Property(x => x.PorcentajeDescuento).HasColumnType("decimal(8,2)");
            e.Property(x => x.TipoSaldo).HasMaxLength(1);
            e.HasIndex(x => x.Cuit);
            e.HasIndex(x => x.RazonSocial);
        });

        m.Entity<Proveedor>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.RazonSocial).HasMaxLength(150).IsRequired();
            e.Property(x => x.Cuit).HasMaxLength(20);
            e.Property(x => x.SaldoCtaCte).HasColumnType("decimal(18,2)");
            e.HasIndex(x => x.Cuit);
            e.HasIndex(x => x.RazonSocial);
        });

        m.Entity<Comprobante>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.SubTotal).HasColumnType("decimal(18,2)");
            e.Property(x => x.TotalDescuento).HasColumnType("decimal(18,2)");
            e.Property(x => x.TotalIva21).HasColumnType("decimal(18,2)");
            e.Property(x => x.TotalIva105).HasColumnType("decimal(18,2)");
            e.Property(x => x.TotalIva0).HasColumnType("decimal(18,2)");
            e.Property(x => x.Total).HasColumnType("decimal(18,2)");
            e.Property(x => x.Comision).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.Cliente).WithMany().HasForeignKey(x => x.IdCliente).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Proveedor).WithMany().HasForeignKey(x => x.IdProveedor).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.TipoComprobante).WithMany().HasForeignKey(x => x.IdTipoComprobante).OnDelete(DeleteBehavior.Restrict);
            // La numeración AFIP es independiente POR TIPO de comprobante (CbteTipo), aunque compartan
            // letra: Factura B (6) y Nota de Crédito B (8) llevan cada una su propia secuencia 1,2,3...
            // No incluir IdTipoComprobante acá permitía que se pisaran entre sí.
            e.HasIndex(x => new { x.IdSucursal, x.PuntoVenta, x.IdTipoComprobante, x.Numero, x.Letra }).IsUnique();
            e.HasIndex(x => x.Fecha);
            e.HasIndex(x => x.IdCliente);
        });

        m.Entity<ComprobanteAfipLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Detalle).HasMaxLength(2000);
            e.HasOne(x => x.Comprobante).WithMany().HasForeignKey(x => x.IdComprobante).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.IdComprobante);
        });

        m.Entity<ComprobanteDetalle>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Cantidad).HasColumnType("decimal(18,3)");
            e.Property(x => x.PrecioUnitario).HasColumnType("decimal(18,4)");
            e.Property(x => x.PrecioUnitarioSinIva).HasColumnType("decimal(18,4)");
            e.Property(x => x.AlicuotaIva).HasColumnType("decimal(5,2)");
            e.Property(x => x.MontoIva).HasColumnType("decimal(18,2)");
            e.Property(x => x.PorcentajeDescuento).HasColumnType("decimal(5,2)");
            e.Property(x => x.MontoDescuento).HasColumnType("decimal(18,2)");
            e.Property(x => x.SubTotal).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.Comprobante).WithMany(c => c.Detalles).HasForeignKey(x => x.IdComprobante).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Articulo).WithMany().HasForeignKey(x => x.IdArticulo).OnDelete(DeleteBehavior.Restrict);
        });

        m.Entity<ComprobantePago>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Importe).HasColumnType("decimal(18,2)");
            e.Property(x => x.Vuelto).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.Comprobante).WithMany(c => c.Pagos).HasForeignKey(x => x.IdComprobante).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.MedioPago).WithMany().HasForeignKey(x => x.IdMedioPago).OnDelete(DeleteBehavior.Restrict);
        });

        m.Entity<TurnoCaja>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.SaldoInicial).HasColumnType("decimal(18,2)");
            e.Property(x => x.SaldoFinal).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.Caja).WithMany().HasForeignKey(x => x.IdCaja).OnDelete(DeleteBehavior.Restrict);
        });

        m.Entity<Caja>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Sucursal).WithMany().HasForeignKey(x => x.IdSucursal).OnDelete(DeleteBehavior.Restrict);
        });

        m.Entity<Departamento>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasMany(x => x.Familias).WithOne(f => f.Departamento).HasForeignKey(f => f.IdDepartamento);
        });

        m.Entity<Usuario>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.NombreUsuario).HasMaxLength(50).IsRequired();
            e.Property(x => x.NombreCompleto).HasMaxLength(150).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(128).IsRequired();
            e.HasOne(x => x.Perfil).WithMany().HasForeignKey(x => x.IdPerfil).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.NombreUsuario).IsUnique();
        });

        m.Entity<Perfil>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Nombre).HasMaxLength(80).IsRequired();
            e.Property(x => x.MaximoDescuento).HasColumnType("decimal(5,2)");
        });

        m.Entity<Compra>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.SubTotal).HasColumnType("decimal(18,2)");
            e.Property(x => x.TotalIva).HasColumnType("decimal(18,2)");
            e.Property(x => x.PercepcionIva).HasColumnType("decimal(18,2)");
            e.Property(x => x.PercepcionNacional).HasColumnType("decimal(18,2)");
            e.Property(x => x.PercepcionIIBB).HasColumnType("decimal(18,2)");
            e.Property(x => x.PercepcionMunicipal).HasColumnType("decimal(18,2)");
            e.Property(x => x.ImpuestosInternos).HasColumnType("decimal(18,2)");
            e.Property(x => x.ImporteNoGravado).HasColumnType("decimal(18,2)");
            e.Property(x => x.ImporteExento).HasColumnType("decimal(18,2)");
            e.Property(x => x.IvaComision).HasColumnType("decimal(18,2)");
            e.Property(x => x.CuitCorredor).HasMaxLength(20);
            e.Property(x => x.NombreCorredor).HasMaxLength(150);
            e.Property(x => x.DespachoImportacion).HasMaxLength(50);
            e.Property(x => x.Total).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.Proveedor).WithMany().HasForeignKey(x => x.IdProveedor).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.TipoComprobante).WithMany().HasForeignKey(x => x.IdTipoComprobante).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Detalles).WithOne(d => d.Compra).HasForeignKey(d => d.IdCompra).OnDelete(DeleteBehavior.Cascade);
        });

        m.Entity<CompraDetalle>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Cantidad).HasColumnType("decimal(18,3)");
            e.Property(x => x.PrecioCosto).HasColumnType("decimal(18,4)");
            e.Property(x => x.Bonificacion).HasColumnType("decimal(8,4)");
            e.Property(x => x.PrecioCostoNeto).HasColumnType("decimal(18,4)");
            e.Property(x => x.AlicuotaIva).HasColumnType("decimal(5,2)");
            e.Property(x => x.SubTotal).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.Articulo).WithMany().HasForeignKey(x => x.IdArticulo).OnDelete(DeleteBehavior.Restrict);
        });

        m.Entity<MovimientoCtaCte>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Concepto).HasMaxLength(250);
            e.Property(x => x.Debe).HasColumnType("decimal(18,2)");
            e.Property(x => x.Haber).HasColumnType("decimal(18,2)");
            e.Property(x => x.SaldoAcumulado).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.Cliente).WithMany().HasForeignKey(x => x.IdCliente).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.IdCliente, x.Fecha });
        });

        m.Entity<MovimientoCtaCteProveedor>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Concepto).HasMaxLength(250);
            e.Property(x => x.Debe).HasColumnType("decimal(18,2)");
            e.Property(x => x.Haber).HasColumnType("decimal(18,2)");
            e.Property(x => x.SaldoAcumulado).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.Proveedor).WithMany().HasForeignKey(x => x.IdProveedor).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.IdProveedor, x.Fecha });
        });

        m.Entity<AuditLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.NombreUsuario).HasMaxLength(80);
            e.Property(x => x.Entidad).HasMaxLength(100);
            e.Property(x => x.EntidadId).HasMaxLength(50);
            e.Property(x => x.Descripcion).HasMaxLength(1000);
            e.HasIndex(x => new { x.Entidad, x.EntidadId });
            e.HasIndex(x => x.Fecha);
            e.HasIndex(x => x.IdUsuario);
        });

        m.Entity<ConfiguracionEmpresa>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.NombreEmpresa).HasMaxLength(150);
            e.Property(x => x.Cuit).HasMaxLength(20);
        });

        m.Entity<Zeta>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.TotalVentas).HasColumnType("decimal(18,2)");
            e.Property(x => x.TotalDescuentos).HasColumnType("decimal(18,2)");
            e.Property(x => x.TotalIva21).HasColumnType("decimal(18,2)");
            e.Property(x => x.TotalIva105).HasColumnType("decimal(18,2)");
            e.Property(x => x.TotalIva0).HasColumnType("decimal(18,2)");
            e.Property(x => x.TotalAnulaciones).HasColumnType("decimal(18,2)");
            e.Property(x => x.TotalEfectivo).HasColumnType("decimal(18,2)");
            e.Property(x => x.TotalTarjetaDebito).HasColumnType("decimal(18,2)");
            e.Property(x => x.TotalTarjetaCredito).HasColumnType("decimal(18,2)");
            e.Property(x => x.TotalTransferencia).HasColumnType("decimal(18,2)");
            e.Property(x => x.TotalCtaCte).HasColumnType("decimal(18,2)");
            e.Property(x => x.TotalMercadoPago).HasColumnType("decimal(18,2)");
            e.Property(x => x.TotalOtros).HasColumnType("decimal(18,2)");
            e.Property(x => x.EfectivoDeclarado).HasColumnType("decimal(18,2)");
            e.Property(x => x.DiferenciaArqueo).HasColumnType("decimal(18,2)");
            e.HasIndex(x => new { x.IdCaja, x.NroZeta }).IsUnique();
        });

        m.Entity<ZetaDetalleMedio>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Total).HasColumnType("decimal(18,2)");
            e.Property(x => x.NombreMedio).HasMaxLength(80);
            e.HasOne(x => x.Zeta).WithMany(z => z.DetallesMedios).HasForeignKey(x => x.IdZeta).OnDelete(DeleteBehavior.Cascade);
        });

        m.Entity<ListaPrecio>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Nombre).HasMaxLength(80).IsRequired();
            e.Property(x => x.Descripcion).HasMaxLength(200);
            e.Property(x => x.Valor).HasColumnType("decimal(8,4)");
        });

        m.Entity<ArticuloPrecioLista>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Precio).HasColumnType("decimal(18,4)");
            e.Property(x => x.PorcentajeAjuste).HasColumnType("decimal(8,4)");
            e.HasOne(x => x.Lista).WithMany(l => l.PreciosEspeciales).HasForeignKey(x => x.IdLista).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Articulo).WithMany().HasForeignKey(x => x.IdArticulo).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.IdLista, x.IdArticulo }).IsUnique();
        });

        m.Entity<BonificacionRango>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.CantidadDesde).HasColumnType("decimal(18,3)");
            e.Property(x => x.CantidadHasta).HasColumnType("decimal(18,3)");
            e.Property(x => x.PorcentajeDescuento).HasColumnType("decimal(8,4)");
            e.HasOne(x => x.Articulo).WithMany().HasForeignKey(x => x.IdArticulo).OnDelete(DeleteBehavior.Cascade);
        });

        m.Entity<ListaPrecioProveedor>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Nombre).HasMaxLength(200).IsRequired();
            e.Property(x => x.Notas).HasMaxLength(2000);
            e.Property(x => x.ArchivoOrigenNombre).HasMaxLength(260);
            e.Property(x => x.ArchivoOrigenRutaRelativa).HasMaxLength(400);
            e.HasIndex(x => new { x.IdProveedor, x.Nombre });
            e.HasOne(x => x.Proveedor).WithMany().HasForeignKey(x => x.IdProveedor).OnDelete(DeleteBehavior.Restrict);
        });

        m.Entity<ListaPrecioProveedorLinea>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.CodigoProveedor).HasMaxLength(80);
            e.Property(x => x.Descripcion).HasMaxLength(500);
            e.Property(x => x.PrecioUnitario).HasColumnType("decimal(18,4)");
            e.Property(x => x.IvaPorcentaje).HasColumnType("decimal(5,2)");
            e.Property(x => x.BonificacionesJson).HasMaxLength(4000);
            e.HasIndex(x => new { x.IdLista, x.CodigoProveedor });
            e.HasOne(x => x.Lista).WithMany(l => l.Lineas).HasForeignKey(x => x.IdLista).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Articulo).WithMany().HasForeignKey(x => x.IdArticulo).OnDelete(DeleteBehavior.SetNull);
        });

        m.Entity<OrdenCompra>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.TotalSinIva).HasColumnType("decimal(18,2)");
            e.Property(x => x.TotalIva).HasColumnType("decimal(18,2)");
            e.Property(x => x.Total).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.Proveedor).WithMany().HasForeignKey(x => x.IdProveedor).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Detalles).WithOne(d => d.OrdenCompra).HasForeignKey(d => d.IdOrdenCompra).OnDelete(DeleteBehavior.Cascade);
        });

        m.Entity<OrdenCompraDetalle>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.CantidadPedida).HasColumnType("decimal(18,3)");
            e.Property(x => x.CantidadRecibida).HasColumnType("decimal(18,3)");
            e.Property(x => x.PrecioCosto).HasColumnType("decimal(18,4)");
            e.Property(x => x.AlicuotaIva).HasColumnType("decimal(5,2)");
            e.Property(x => x.Subtotal).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.Articulo).WithMany().HasForeignKey(x => x.IdArticulo).OnDelete(DeleteBehavior.Restrict);
        });

        m.Entity<Inventario>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Descripcion).HasMaxLength(200);
            e.Property(x => x.DiferenciaValorizada).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.Sucursal).WithMany().HasForeignKey(x => x.IdSucursal).OnDelete(DeleteBehavior.Restrict);
        });

        m.Entity<ArticuloStockSucursal>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Cantidad).HasColumnType("decimal(18,3)");
            e.HasOne(x => x.Articulo).WithMany().HasForeignKey(x => x.IdArticulo).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Sucursal).WithMany().HasForeignKey(x => x.IdSucursal).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.IdArticulo, x.IdSucursal }).IsUnique();
        });

        m.Entity<TransferenciaInterna>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Observaciones).HasMaxLength(500);
            e.HasOne(x => x.SucursalOrigen).WithMany().HasForeignKey(x => x.IdSucursalOrigen).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.SucursalDestino).WithMany().HasForeignKey(x => x.IdSucursalDestino).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Detalles).WithOne(d => d.Transferencia).HasForeignKey(d => d.IdTransferencia).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.NroTransferencia).IsUnique();
        });

        m.Entity<TransferenciaInternaDetalle>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Cantidad).HasColumnType("decimal(18,3)");
            e.HasOne(x => x.Articulo).WithMany().HasForeignKey(x => x.IdArticulo).OnDelete(DeleteBehavior.Restrict);
        });

        m.Entity<InventarioDetalle>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.FueConteado).HasDefaultValue(false);
            e.Property(x => x.StockSistema).HasColumnType("decimal(18,3)");
            e.Property(x => x.StockContado).HasColumnType("decimal(18,3)");
            e.Property(x => x.PrecioCosto).HasColumnType("decimal(18,4)");
            e.Ignore(x => x.Diferencia);
            e.Ignore(x => x.DiferenciaValorizada);
            e.HasOne(x => x.Articulo).WithMany().HasForeignKey(x => x.IdArticulo).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Inventario).WithMany(i => i.Detalles).HasForeignKey(x => x.IdInventario).OnDelete(DeleteBehavior.Cascade);
        });

        m.Entity<Remito>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.NroRemitoExterno).HasMaxLength(50);
            e.Property(x => x.Transportista).HasMaxLength(150);
            e.HasOne(x => x.Proveedor).WithMany().HasForeignKey(x => x.IdProveedor).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Cliente).WithMany().HasForeignKey(x => x.IdCliente).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Detalles).WithOne(d => d.Remito).HasForeignKey(d => d.IdRemito).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.Tipo, x.NroRemito }).IsUnique();
        });

        m.Entity<RemitoDetalle>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.CantidadRemitida).HasColumnType("decimal(18,3)");
            e.Property(x => x.CantidadRecibida).HasColumnType("decimal(18,3)");
            e.Property(x => x.PrecioCosto).HasColumnType("decimal(18,4)");
            e.Property(x => x.LoteNro).HasMaxLength(50);
            e.Property(x => x.NroSerie).HasMaxLength(100);
            e.HasOne(x => x.Articulo).WithMany().HasForeignKey(x => x.IdArticulo).OnDelete(DeleteBehavior.Restrict);
        });

        m.Entity<CuentaTesoreria>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.SaldoInicial).HasColumnType("decimal(18,2)");
            e.Property(x => x.SaldoActual).HasColumnType("decimal(18,2)");
            e.HasMany(x => x.Movimientos).WithOne(m => m.Cuenta).HasForeignKey(m => m.IdCuenta).OnDelete(DeleteBehavior.Restrict);
        });

        m.Entity<MovimientoTesoreria>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Monto).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.CuentaDestino).WithMany().HasForeignKey(x => x.IdCuentaDestino).OnDelete(DeleteBehavior.Restrict);
        });

        m.Entity<Cheque>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Monto).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.Cliente).WithMany().HasForeignKey(x => x.IdCliente).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Proveedor).WithMany().HasForeignKey(x => x.IdProveedor).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Cuenta).WithMany().HasForeignKey(x => x.IdCuenta).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Chequera).WithMany().HasForeignKey(x => x.IdChequera).OnDelete(DeleteBehavior.Restrict);
        });

        m.Entity<Chequera>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
            e.Property(x => x.Desde).HasMaxLength(50).IsRequired();
            e.Property(x => x.Hasta).HasMaxLength(50).IsRequired();
            e.Property(x => x.SiguienteNumero).HasMaxLength(50).IsRequired();
            e.HasOne(x => x.Cuenta).WithMany().HasForeignKey(x => x.IdCuentaTesoreria).OnDelete(DeleteBehavior.Restrict);
        });

        m.Entity<Banco>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Codigo).HasMaxLength(10).IsRequired();
            e.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
        });

        m.Entity<GastoCaja>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Monto).HasColumnType("decimal(18,2)");
        });

        m.Entity<Alerta>(e =>
        {
            e.ToTable("Alertas");
            e.HasKey(x => x.Id);
            e.Property(x => x.Descripcion).HasMaxLength(200).IsRequired();
            e.Property(x => x.Detalle).HasMaxLength(1000);
            e.Property(x => x.ConsultaSQL).IsRequired();
            e.Property(x => x.DiasSemanaAlerta).HasMaxLength(50);
        });

        m.Entity<AlertaUsuario>(e =>
        {
            e.ToTable("AlertasUsuarios");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Alerta).WithMany(a => a.AlertasUsuarios).HasForeignKey(x => x.IdAlerta).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.IdUsuario).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.IdAlerta, x.IdUsuario }).IsUnique();
        });

        m.Entity<AlertaEnviada>(e =>
        {
            e.ToTable("AlertasEnviadas");
            e.HasKey(x => x.Id);
            e.Property(x => x.Md5Registros).HasMaxLength(50).IsRequired();
            e.Property(x => x.Log).HasColumnType("text").IsRequired();
            e.HasOne(x => x.Alerta).WithMany().HasForeignKey(x => x.IdAlerta).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.IdUsuario).OnDelete(DeleteBehavior.Cascade);
        });

        m.Entity<Mailing>(e =>
        {
            e.ToTable("Mailing");
            e.HasKey(x => x.Id);
            e.Property(x => x.Destino).HasMaxLength(200).IsRequired();
            e.Property(x => x.Asunto).HasMaxLength(200).IsRequired();
            e.Property(x => x.Cuerpo).HasColumnType("text").IsRequired();
            e.Property(x => x.Estado).HasMaxLength(1);
        });

        m.Entity<HistorialPrecio>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.PrecioAnterior).HasColumnType("decimal(18,4)");
            e.Property(x => x.PrecioNuevo).HasColumnType("decimal(18,4)");
            e.Property(x => x.Campo).HasMaxLength(2).IsRequired();
            e.Property(x => x.Fecha).HasColumnType("timestamp with time zone");

            e.HasOne(x => x.Articulo)
                .WithMany()
                .HasForeignKey(x => x.IdArticulo)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Usuario)
                .WithMany()
                .HasForeignKey(x => x.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Sucursal)
                .WithMany()
                .HasForeignKey(x => x.IdSucursal)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => x.IdArticulo);
            e.HasIndex(x => new { x.IdArticulo, x.Fecha });
        });

        m.Entity<BonificacionFecha>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Detalle).HasMaxLength(150).IsRequired();
            e.Property(x => x.Porcentaje).HasColumnType("decimal(8,4)");
            e.Property(x => x.FechaDesde).HasColumnType("timestamp with time zone");
            e.Property(x => x.FechaHasta).HasColumnType("timestamp with time zone");

            e.HasOne(x => x.Articulo)
                .WithMany()
                .HasForeignKey(x => x.IdArticulo)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => x.IdArticulo);
            e.HasIndex(x => new { x.IdArticulo, x.FechaDesde, x.FechaHasta });
        });

        m.Entity<Presupuesto>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.SubTotal).HasColumnType("decimal(18,2)");
            e.Property(x => x.Total).HasColumnType("decimal(18,2)");
            e.Property(x => x.Contacto).HasMaxLength(150);
            e.Property(x => x.Detalle).HasMaxLength(200).IsRequired();
            e.Property(x => x.Observacion).HasMaxLength(1000);
            e.Property(x => x.FormaPago).HasMaxLength(100);

            e.HasOne(x => x.Cliente)
                .WithMany()
                .HasForeignKey(x => x.IdCliente)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Usuario)
                .WithMany()
                .HasForeignKey(x => x.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Sucursal)
                .WithMany()
                .HasForeignKey(x => x.IdSucursal)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(x => x.Detalles)
                .WithOne(d => d.Presupuesto)
                .HasForeignKey(d => d.IdPresupuesto)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => x.IdCliente);
            e.HasIndex(x => new { x.IdSucursal, x.Numero });
        });

        m.Entity<PresupuestoDetalle>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Descripcion).HasMaxLength(200).IsRequired();
            e.Property(x => x.Costo).HasColumnType("decimal(18,4)");
            e.Property(x => x.Cantidad).HasColumnType("decimal(18,3)");
            e.Property(x => x.Precio).HasColumnType("decimal(18,4)");
            e.Property(x => x.Margen).HasColumnType("decimal(8,4)");

            e.HasOne(x => x.Articulo)
                .WithMany()
                .HasForeignKey(x => x.IdArticulo)
                .OnDelete(DeleteBehavior.Restrict);
        });

        m.Entity<Cotizacion>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Descripcion).HasMaxLength(200).IsRequired();
            e.Property(x => x.Observacion).HasMaxLength(1000);
            e.Property(x => x.PlazoEntrega).HasMaxLength(100);

            e.HasOne(x => x.Proveedor)
                .WithMany()
                .HasForeignKey(x => x.IdProveedor)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(x => x.Detalles)
                .WithOne(d => d.Cotizacion)
                .HasForeignKey(d => d.IdCotizacion)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => x.IdProveedor);
        });

        m.Entity<CotizacionDetalle>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Cantidad).HasColumnType("decimal(18,3)");
            e.Property(x => x.Precio).HasColumnType("decimal(18,4)");

            e.HasOne(x => x.Articulo)
                .WithMany()
                .HasForeignKey(x => x.IdArticulo)
                .OnDelete(DeleteBehavior.Restrict);
        });

        m.Entity<Promocion>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Descripcion).HasMaxLength(255).IsRequired();
            e.Property(x => x.HoraInicio).HasMaxLength(50);
            e.Property(x => x.HoraFin).HasMaxLength(50);
            e.Property(x => x.DiasSemana).HasMaxLength(100);
            e.Property(x => x.Sucursales).HasMaxLength(255);
            e.HasIndex(x => new { x.FechaDesde, x.FechaHasta, x.Activa });
        });

        m.Entity<PromocionCondicion>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Tipo).HasMaxLength(50).IsRequired();
            e.Property(x => x.Cantidad).HasColumnType("decimal(18,3)");
            e.Property(x => x.Importe).HasColumnType("decimal(18,2)");
            e.Property(x => x.ValorDesde).HasColumnType("decimal(18,3)");
            e.Property(x => x.ValorHasta).HasColumnType("decimal(18,3)");
            e.Property(x => x.TipoValor).HasMaxLength(50);
            e.Property(x => x.Excluye);
            e.HasOne(x => x.Promocion).WithMany(p => p.Condiciones).HasForeignKey(x => x.IdPromocion).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Articulo).WithMany().HasForeignKey(x => x.IdArticulo).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.IdPromocion);
            e.HasIndex(x => x.IdArticulo);
        });

        m.Entity<PromocionParametroAccion>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Tipo).HasMaxLength(50).IsRequired();
            e.Property(x => x.Cantidad).HasColumnType("decimal(18,3)");
            e.Property(x => x.Importe).HasColumnType("decimal(18,2)");
            e.Property(x => x.Porcentaje).HasColumnType("decimal(8,4)");
            e.Property(x => x.Valor).HasColumnType("decimal(18,4)");
            e.Property(x => x.TipoValor).HasMaxLength(50);
            e.Property(x => x.AplicaSobre).HasMaxLength(50);
            e.Property(x => x.Repeticiones);
            e.Property(x => x.PrefiereMenorValor);
            e.HasOne(x => x.Promocion).WithMany(p => p.ParametrosAccion).HasForeignKey(x => x.IdPromocion).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Articulo).WithMany().HasForeignKey(x => x.IdArticulo).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.IdPromocion);
            e.HasIndex(x => x.IdArticulo);
        });

        m.Entity<MonedaPlan>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Detalle).HasMaxLength(100).IsRequired();
            e.Property(x => x.Recargo).HasColumnType("decimal(5,2)");
            e.HasOne(x => x.MedioPago).WithMany().HasForeignKey(x => x.IdMedioPago).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.IdMedioPago);
        });

        m.Entity<ArticuloDatoAdicional>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Campo).HasMaxLength(50).IsRequired();
            e.Property(x => x.Dato).HasMaxLength(200).IsRequired();
            e.HasOne(x => x.Articulo).WithMany().HasForeignKey(x => x.IdArticulo).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.IdArticulo, x.Campo }).IsUnique();
        });

        // Configuración de Tablas Legacy
        m.Entity<Auditoria>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Hora).HasMaxLength(8);
            e.Property(x => x.TipoCbte).HasMaxLength(4);
            e.Property(x => x.Tipo).HasMaxLength(10);
            e.Property(x => x.Codigo).HasMaxLength(50);
            e.Property(x => x.Cantidad).HasColumnType("decimal(18,3)");
            e.Property(x => x.Importe).HasColumnType("decimal(18,2)");
            e.HasIndex(x => new { x.Fecha, x.Cajero });
            e.HasIndex(x => x.Codigo);
        });

        m.Entity<CajeroLegacy>(e =>
        {
            e.HasKey(x => x.Codigo);
            e.Property(x => x.Codigo).ValueGeneratedNever();
            e.Property(x => x.Nombre).HasMaxLength(50);
            e.Property(x => x.Clave).HasMaxLength(50);
        });

        m.Entity<Cupon>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Texto).HasMaxLength(50);
        });

        m.Entity<Encabezado>(e =>
        {
            e.HasKey(x => x.Linea);
            e.Property(x => x.Linea).ValueGeneratedNever();
            e.Property(x => x.Texto).HasMaxLength(50);
        });

        m.Entity<Fantasia>(e =>
        {
            e.HasKey(x => x.Linea);
            e.Property(x => x.Linea).ValueGeneratedNever();
            e.Property(x => x.Texto).HasMaxLength(255);
        });

        m.Entity<Moneda>(e =>
        {
            e.HasKey(x => x.Codigo);
            e.Property(x => x.Codigo).ValueGeneratedNever();
            e.Property(x => x.Descripcion).HasMaxLength(50);
            e.Property(x => x.Cotizacion).HasColumnType("decimal(18,4)");
            e.Property(x => x.Tipo).HasMaxLength(2);
            e.Property(x => x.Cuenta).HasColumnType("decimal(18,2)");
            e.Property(x => x.Comision).HasColumnType("decimal(18,2)");
            e.Property(x => x.DescripcionImpresion).HasMaxLength(50);
            e.Property(x => x.ImporteRetiro).HasColumnType("decimal(18,2)");
        });

        m.Entity<MonedaPPH>(e =>
        {
            e.HasKey(x => x.Codigo);
            e.Property(x => x.Codigo).ValueGeneratedNever();
            e.Property(x => x.Descripcion).HasMaxLength(50);
            e.Property(x => x.Cotizacion).HasColumnType("decimal(18,4)");
            e.Property(x => x.Tipo).HasMaxLength(2);
            e.Property(x => x.Cuenta).HasColumnType("decimal(18,2)");
            e.Property(x => x.Comision).HasColumnType("decimal(18,2)");
            e.Property(x => x.DescripcionImpresion).HasMaxLength(50);
            e.Property(x => x.ImporteRetiro).HasColumnType("decimal(18,2)");
        });

        m.Entity<Pie>(e =>
        {
            e.HasKey(x => x.Linea);
            e.Property(x => x.Linea).ValueGeneratedNever();
            e.Property(x => x.Texto).HasMaxLength(50);
        });

        m.Entity<POS_Busqueda>(e =>
        {
            e.HasKey(x => x.Busqueda);
            e.Property(x => x.Busqueda).HasMaxLength(20);
            e.Property(x => x.Tabla).HasMaxLength(20);
            e.Property(x => x.TipoCargaDatos).HasMaxLength(1);
            e.Property(x => x.FiltroBusqueda).HasMaxLength(50);
            e.Property(x => x.TipoFiltroDatos).HasMaxLength(255);
            e.Property(x => x.Servidor).HasMaxLength(255);
            e.Property(x => x.Base).HasMaxLength(255);
        });

        m.Entity<POS_BusquedaCampo>(e =>
        {
            e.HasKey(x => new { x.Busqueda, x.Posicion });
            e.Property(x => x.Busqueda).HasMaxLength(20);
            e.Property(x => x.Campo).HasMaxLength(20);
        });

        m.Entity<POS_Config>(e =>
        {
            e.HasKey(x => x.NroCaja);
            e.Property(x => x.NroCaja).ValueGeneratedNever();
            e.Property(x => x.PuertoScanner).HasMaxLength(255);
            e.Property(x => x.PuertoDisplay).HasMaxLength(255);
            e.Property(x => x.PuertoBalanza).HasMaxLength(50);
            e.Property(x => x.PuertoFiscal).HasMaxLength(255);
            e.Property(x => x.PathImagenesArticulos).HasMaxLength(255);
            e.Property(x => x.PathImagenesCajeros).HasMaxLength(255);
            e.Property(x => x.FiscalMarca).HasMaxLength(50);
            e.Property(x => x.FiscalModelo).HasMaxLength(50);
            e.Property(x => x.ModoCobro).HasMaxLength(50);
            e.Property(x => x.ModoItem).HasMaxLength(1);
            e.Property(x => x.PathImagenes).HasMaxLength(50);
            e.Property(x => x.PathImagenesServidor).HasMaxLength(255);
            e.Property(x => x.VentaCantidadMaxima).HasColumnType("decimal(18,3)");
            e.Property(x => x.VentaImporteMaximo).HasColumnType("decimal(18,2)");
            e.Property(x => x.VentaImporteMinimo).HasColumnType("decimal(18,2)");
            e.Property(x => x.VentaCantidadDefecto).HasColumnType("decimal(18,3)");
            e.Property(x => x.VentaCantidadMaximaPagos).HasColumnType("decimal(18,3)");
            e.Property(x => x.VentaImporteMinimoCbte).HasColumnType("decimal(18,2)");
            e.Property(x => x.VentaImporteMaximoCbte).HasColumnType("decimal(18,2)");
            e.Property(x => x.PesosXPunto).HasColumnType("decimal(18,2)");
        });

        m.Entity<POS_Cupon>(e =>
        {
            e.HasKey(x => new { x.NroCupon, x.Linea });
            e.Property(x => x.Texto).HasMaxLength(50);
        });

        m.Entity<POS_Evento>(e =>
        {
            e.HasKey(x => x.Evento);
            e.Property(x => x.Evento).ValueGeneratedNever();
            e.Property(x => x.Detalle).HasMaxLength(255);
            e.Property(x => x.Tipo).HasMaxLength(255);
        });

        m.Entity<POS_Formulario>(e =>
        {
            e.HasKey(x => x.NroFormulario);
            e.Property(x => x.NroFormulario).ValueGeneratedNever();
        });

        m.Entity<POS_Funcion>(e =>
        {
            e.HasKey(x => x.NroFuncion);
            e.Property(x => x.NroFuncion).ValueGeneratedNever();
            e.Property(x => x.Funcion).HasMaxLength(50);
            e.Property(x => x.Descripcion).HasMaxLength(50);
            e.Property(x => x.Busqueda).HasMaxLength(50);
            e.Property(x => x.Imagen).HasMaxLength(255);
            e.Property(x => x.PorcentajeMaximo).HasColumnType("decimal(8,4)");
            e.HasIndex(x => x.Panel);
        });

        m.Entity<POS_GrillaColumna>(e =>
        {
            e.HasKey(x => x.Nombre);
            e.Property(x => x.Nombre).HasMaxLength(50);
            e.Property(x => x.Titulo).HasMaxLength(50);
            e.Property(x => x.Alineamiento).HasMaxLength(50);
        });

        m.Entity<POS_GrillaVenta>(e =>
        {
            e.HasKey(x => x.NroGrilla);
            e.Property(x => x.NroGrilla).ValueGeneratedNever();
            e.Property(x => x.Descripcion).HasMaxLength(50);
            e.Property(x => x.ExtraTipo).HasMaxLength(1);
            e.HasIndex(x => x.Panel);
        });

        m.Entity<POS_Imagen>(e =>
        {
            e.HasKey(x => x.NroImagen);
            e.Property(x => x.NroImagen).ValueGeneratedNever();
            e.Property(x => x.CampoContenido).HasMaxLength(255);
            e.HasIndex(x => x.Panel);
        });

        m.Entity<POS_Ingreso>(e =>
        {
            e.HasKey(x => x.NroIngreso);
            e.Property(x => x.NroIngreso).ValueGeneratedNever();
            e.Property(x => x.Descripcion).HasMaxLength(50);
            e.HasIndex(x => x.Panel);
        });

        m.Entity<POS_NumeroComprobante>(e =>
        {
            e.HasKey(x => x.TipoCbte);
            e.Property(x => x.TipoCbte).HasMaxLength(255);
        });

        m.Entity<POS_Panel>(e =>
        {
            e.HasKey(x => x.Panel);
            e.Property(x => x.Panel).ValueGeneratedNever();
            e.Property(x => x.Titulo).HasMaxLength(50);
            e.Property(x => x.Imagen).HasMaxLength(255);
        });

        m.Entity<POS_PanelPosicion>(e =>
        {
            e.HasKey(x => new { x.Panel, x.Posicion });
        });

        m.Entity<POS_Rendicion>(e =>
        {
            e.HasKey(x => x.Rendicion);
            e.Property(x => x.Rendicion).HasMaxLength(20);
        });

        m.Entity<POS_Tecla>(e =>
        {
            e.HasKey(x => new { x.IngresoNro, x.Tecla });
        });

        m.Entity<POS_Video>(e =>
        {
            e.HasKey(x => x.NroVideo);
            e.Property(x => x.NroVideo).ValueGeneratedNever();
            e.Property(x => x.PathContenido).HasMaxLength(255);
            e.Property(x => x.PathVideosServidor).HasMaxLength(50);
            e.HasIndex(x => x.Panel);
        });

        SeedData(m);
    }

    private static void SeedData(ModelBuilder m)
    {
        var fecha = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        m.Entity<Sucursal>().HasData(
            new Sucursal { Id = 1, Nombre = "Casa Central", EsCentral = true, Activo = true },
            new Sucursal { Id = 2, Nombre = "Local comercial (ejemplo)", EsCentral = false, Activo = true });

        m.Entity<Banco>().HasData(
            new Banco { Id = 1, Codigo = "011", Nombre = "Banco de la Nación Argentina" },
            new Banco { Id = 2, Codigo = "014", Nombre = "Banco de la Provincia de Buenos Aires" },
            new Banco { Id = 3, Codigo = "007", Nombre = "Banco Galicia" },
            new Banco { Id = 4, Codigo = "017", Nombre = "BBVA Argentina" },
            new Banco { Id = 5, Codigo = "072", Nombre = "Banco Santander Argentina" },
            new Banco { Id = 6, Codigo = "285", Nombre = "Banco Macro" },
            new Banco { Id = 7, Codigo = "191", Nombre = "Banco Credicoop" },
            new Banco { Id = 8, Codigo = "034", Nombre = "Banco Patagonia" },
            new Banco { Id = 9, Codigo = "029", Nombre = "Banco Ciudad de Buenos Aires" },
            new Banco { Id = 10, Codigo = "027", Nombre = "Banco Supervielle" }
        );
        m.Entity<Caja>().HasData(new Caja { Id = 1, Nombre = "Caja 1", IdSucursal = 1, Activo = true });

        m.Entity<Perfil>().HasData(
            new Perfil
            {
                Id = 1, Nombre = "Administrador", EsAdministrador = true,
                AccesoCaja = true, AccesoArticulos = true, AccesoClientes = true,
                AccesoProveedores = true, AccesoCompras = true, AccesoStock = true,
                AccesoCtaCte = true, AccesoReportes = true, AccesoConfiguracion = true, AccesoUsuarios = true,
                PuedeVender = true, PuedeAnularVentas = true, PuedeHacerDescuentos = true,
                MaximoDescuento = 100, PuedeCambiarPrecios = true, PuedeVerCostos = true,
                PuedeModificarStock = true, PuedeAbrirCaja = true, PuedeCerrarCaja = true
            },
            new Perfil
            {
                Id = 2, Nombre = "Cajero",
                AccesoCaja = true, AccesoClientes = true,
                PuedeVender = true, PuedeHacerDescuentos = true, MaximoDescuento = 10,
                PuedeAbrirCaja = true, PuedeCerrarCaja = false
            },
            new Perfil
            {
                Id = 3, Nombre = "Repositor",
                AccesoArticulos = true, AccesoStock = true, AccesoCompras = true,
                PuedeModificarStock = true, PuedeVerCostos = true
            },
            new Perfil
            {
                Id = 4, Nombre = "Supervisor",
                AccesoCaja = true, AccesoArticulos = true, AccesoClientes = true,
                AccesoProveedores = true, AccesoCompras = true, AccesoStock = true,
                AccesoCtaCte = true, AccesoReportes = true,
                PuedeVender = true, PuedeAnularVentas = true, PuedeHacerDescuentos = true,
                MaximoDescuento = 30, PuedeCambiarPrecios = false, PuedeVerCostos = true,
                PuedeModificarStock = true, PuedeAbrirCaja = true, PuedeCerrarCaja = true
            }
        );

        m.Entity<TipoComprobante>().HasData(
            new TipoComprobante { Id = 1, Nombre = "Factura A", Abreviatura = "FA", EsVenta = true, RequiereCAE = true, CodigoAfip = 1, Activo = true },
            new TipoComprobante { Id = 2, Nombre = "Factura B", Abreviatura = "FB", EsVenta = true, RequiereCAE = true, CodigoAfip = 6, Activo = true },
            new TipoComprobante { Id = 3, Nombre = "Factura C", Abreviatura = "FC", EsVenta = true, RequiereCAE = true, CodigoAfip = 11, Activo = true },
            new TipoComprobante { Id = 4, Nombre = "Nota de Crédito A", Abreviatura = "NCA", EsVenta = true, RequiereCAE = true, CodigoAfip = 3, Activo = true },
            new TipoComprobante { Id = 5, Nombre = "Nota de Crédito B", Abreviatura = "NCB", EsVenta = true, RequiereCAE = true, CodigoAfip = 8, Activo = true },
            new TipoComprobante { Id = 6, Nombre = "Nota de Crédito C", Abreviatura = "NCC", EsVenta = true, RequiereCAE = true, CodigoAfip = 13, Activo = true },
            new TipoComprobante { Id = 7, Nombre = "Ticket", Abreviatura = "TK", EsVenta = true, RequiereCAE = false, Activo = true },
            new TipoComprobante { Id = 8, Nombre = "Remito", Abreviatura = "REM", EsVenta = false, RequiereCAE = false, Activo = true },
            new TipoComprobante { Id = 9, Nombre = "Presupuesto", Abreviatura = "PRE", EsVenta = false, RequiereCAE = false, Activo = true }
        );

        m.Entity<MedioPago>().HasData(
            new MedioPago { Id = 1, Nombre = "Efectivo", Tipo = TipoMedioPago.Efectivo, Activo = true },
            new MedioPago { Id = 2, Nombre = "Tarjeta de Débito", Tipo = TipoMedioPago.TarjetaDebito, RequiereReferencia = true, Activo = true },
            new MedioPago { Id = 3, Nombre = "Tarjeta de Crédito", Tipo = TipoMedioPago.TarjetaCredito, RequiereReferencia = true, Activo = true },
            new MedioPago { Id = 4, Nombre = "MercadoPago", Tipo = TipoMedioPago.MercadoPago, RequiereReferencia = true, Activo = true },
            new MedioPago { Id = 5, Nombre = "Transferencia", Tipo = TipoMedioPago.Transferencia, RequiereReferencia = true, Activo = true },
            new MedioPago { Id = 6, Nombre = "Cuenta Corriente", Tipo = TipoMedioPago.CtaCte, Activo = true },
            new MedioPago { Id = 7, Nombre = "Cheque", Tipo = TipoMedioPago.Cheque, RequiereReferencia = true, Activo = true }
        );

        m.Entity<TarjetaMarca>(e =>
        {
            e.Property(x => x.PorcentajeRecargo).HasColumnType("decimal(6,3)");
        });
        m.Entity<TarjetaMarca>().HasData(
            new TarjetaMarca { Id = 1, Codigo = "visa-cred", Nombre = "Visa Crédito", EsCredito = true, Activo = true },
            new TarjetaMarca { Id = 2, Codigo = "visa-deb", Nombre = "Visa Débito", EsCredito = false, Activo = true },
            new TarjetaMarca { Id = 3, Codigo = "master-cred", Nombre = "Mastercard Crédito", EsCredito = true, Activo = true },
            new TarjetaMarca { Id = 4, Codigo = "master-deb", Nombre = "Mastercard Débito", EsCredito = false, Activo = true },
            new TarjetaMarca { Id = 5, Codigo = "cabal-cred", Nombre = "Cabal Crédito", EsCredito = true, Activo = true },
            new TarjetaMarca { Id = 6, Codigo = "cabal-deb", Nombre = "Cabal Débito", EsCredito = false, Activo = true },
            new TarjetaMarca { Id = 7, Codigo = "amex", Nombre = "American Express", EsCredito = true, Activo = true },
            new TarjetaMarca { Id = 8, Codigo = "naranja", Nombre = "Naranja", EsCredito = true, Activo = true },
            new TarjetaMarca { Id = 9, Codigo = "nativa", Nombre = "Nativa", EsCredito = true, Activo = true }
        );

        m.Entity<Departamento>().HasData(new Departamento { Id = 1, Nombre = "General", Activo = true });
        m.Entity<Familia>().HasData(new Familia { Id = 1, Nombre = "General", IdDepartamento = 1, Activo = true });
        m.Entity<Marca>().HasData(new Marca { Id = 1, Nombre = "Sin marca", Activo = true });

        m.Entity<ListaPrecio>().HasData(
            new ListaPrecio { Id = 1, Nombre = "Minorista", Descripcion = "Precio público general", Tipo = TipoListaPrecio.PorcentajeSobreVenta, Valor = 0, EsDefault = true, Activo = true },
            new ListaPrecio { Id = 2, Nombre = "Mayorista", Descripcion = "Precio especial por volumen", Tipo = TipoListaPrecio.PorcentajeSobreVenta, Valor = -10, EsDefault = false, Activo = true },
            new ListaPrecio { Id = 3, Nombre = "Empleados", Descripcion = "Precio para empleados", Tipo = TipoListaPrecio.PorcentajeSobreVenta, Valor = -15, EsDefault = false, Activo = true }
        );

        m.Entity<ConfiguracionEmpresa>().HasData(new ConfiguracionEmpresa
        {
            Id = 1,
            NombreEmpresa = "Los Angeles Supermercados",
            NombreFantasia = "Supermercados Los Angeles",
            Cuit = "00-00000000-0",
            Provincia = "Buenos Aires",
            PuntoVenta = 1,
            AfipHomologacion = true,
            ControlaStock = true,
            PrecioConIva = true,
            MensajePiePagina = "¡Gracias por su compra! Vuelva pronto."
        });

        m.Entity<Cliente>().HasData(new Cliente
        {
            Id = 1, RazonSocial = "Consumidor Final", Cuit = "00000000000",
            CondicionIva = 5, Activo = true, IdListaPrecio = 1,
            FechaAlta = fecha, TipoSaldo = 'H'
        });

        m.Entity<Usuario>().HasData(new Usuario
        {
            Id = 1, NombreUsuario = "admin", NombreCompleto = "Administrador",
            PasswordHash = "8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918", // SHA256("admin")
            IdPerfil = 1, Activo = true, FechaAlta = fecha
        });
    }
}
