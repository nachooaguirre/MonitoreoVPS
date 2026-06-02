using System;
using System.Data;
using System.Data.Odbc;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperPOS.API.Data;
using SuperPOS.Shared.Entities.Ventas;
using SuperPOS.Shared.Entities.Ventas.Legacy;

namespace SuperPOS.API.Services;

public class ImportadorLegacyService
{
    private readonly SuperPOSDbContext _db;
    private readonly ILogger<ImportadorLegacyService> _logger;

    public ImportadorLegacyService(SuperPOSDbContext db, ILogger<ImportadorLegacyService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ImportResult> ImportarDeMdbAsync(string mdbPath)
    {
        var result = new ImportResult();
        string connStr = $"Driver={{Microsoft Access Driver (*.mdb, *.accdb)}};Dbq={mdbPath};";

        using var conn = new OdbcConnection(connStr);
        try
        {
            conn.Open();
            _logger.LogInformation("Conexión abierta con éxito a la base de datos MDB: {MdbPath}", mdbPath);

            using var transaction = await _db.Database.BeginTransactionAsync();

            // 1. Departamentos
            _logger.LogInformation("Importando Departamentos...");
            var deptosCmd = new OdbcCommand("SELECT Codigo, Descripcion, EsEnvase FROM Departamentos", conn);
            using (var reader = deptosCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    int codigo = Convert.ToInt32(reader["Codigo"]);
                    string descripcion = reader["Descripcion"]?.ToString() ?? "";
                    
                    var depto = await _db.Departamentos.FirstOrDefaultAsync(d => d.Id == codigo);
                    if (depto == null)
                    {
                        depto = new Departamento { Id = codigo, Nombre = descripcion, Activo = true };
                        _db.Departamentos.Add(depto);
                        result.DepartamentosCreados++;
                    }
                    else
                    {
                        depto.Nombre = descripcion;
                        result.DepartamentosActualizados++;
                    }
                }
            }
            await _db.SaveChangesAsync();

            // Asegurarnos de que existan familias, marcas y proveedores genéricos
            var familiaDef = await _db.Familias.FirstOrDefaultAsync(f => f.Id == 1);
            if (familiaDef == null)
            {
                _db.Familias.Add(new Familia { Id = 1, Nombre = "General", IdDepartamento = 1, Activo = true });
            }

            var marcaDef = await _db.Marcas.FirstOrDefaultAsync(m => m.Id == 1);
            if (marcaDef == null)
            {
                _db.Marcas.Add(new Marca { Id = 1, Nombre = "Sin Marca", Activo = true });
            }

            var provDef = await _db.Proveedores.FirstOrDefaultAsync(p => p.Id == 1);
            if (provDef == null)
            {
                _db.Proveedores.Add(new Proveedor { Id = 1, RazonSocial = "Proveedor General", Cuit = "00000000000", Activo = true });
            }
            await _db.SaveChangesAsync();

            // 2. Clientes
            _logger.LogInformation("Importando Clientes...");
            var clientesCmd = new OdbcCommand("SELECT Codigo, Nombre, Direccion, Documento, Descuento, ListaPrecio, Estado FROM Clientes", conn);
            using (var reader = clientesCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    int codigo = Convert.ToInt32(reader["Codigo"]);
                    string nombre = reader["Nombre"]?.ToString() ?? "";
                    string cuit = reader["Documento"]?.ToString() ?? "";
                    string direccion = reader["Direccion"]?.ToString() ?? "";
                    decimal descuento = reader["Descuento"] != DBNull.Value ? Convert.ToDecimal(reader["Descuento"]) : 0m;
                    int listaPrecio = reader["ListaPrecio"] != DBNull.Value ? Convert.ToInt32(reader["ListaPrecio"]) : 1;
                    string estado = reader["Estado"]?.ToString() ?? "A";

                    var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Id == codigo);
                    bool esNuevo = false;
                    if (cliente == null)
                    {
                        cliente = new Cliente { Id = codigo, FechaAlta = DateTime.UtcNow };
                        esNuevo = true;
                    }

                    cliente.RazonSocial = nombre;
                    cliente.NombreFantasia = nombre;
                    cliente.Cuit = cuit;
                    cliente.Direccion = direccion;
                    cliente.PorcentajeDescuento = descuento;
                    cliente.IdListaPrecio = listaPrecio == 0 ? 1 : listaPrecio;
                    cliente.Activo = (estado == "A");

                    if (esNuevo)
                    {
                        _db.Clientes.Add(cliente);
                        result.ClientesCreados++;
                    }
                    else
                    {
                        result.ClientesActualizados++;
                    }
                }
            }
            await _db.SaveChangesAsync();

            // 3. Artículos
            _logger.LogInformation("Importando Artículos...");
            var articulosCmd = new OdbcCommand("SELECT EAN, Descripcion, DescripcionCorta, Tipo, Precio1, Precio2, EsEnvase, Acumulador, Iva, ImpInt, CodigoInterno, Familia, Marca, Proveedor FROM Articulos", conn);
            using (var reader = articulosCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    string ean = reader["EAN"] != DBNull.Value ? Convert.ToDecimal(reader["EAN"]).ToString("F0") : "";
                    string descripcion = reader["Descripcion"]?.ToString() ?? "";
                    string descCorta = reader["DescripcionCorta"]?.ToString() ?? "";
                    int codigoIntLegacy = reader["CodigoInterno"] != DBNull.Value ? Convert.ToInt32(reader["CodigoInterno"]) : 0;
                    string codigoInt = codigoIntLegacy > 0 ? codigoIntLegacy.ToString() : "";
                    decimal precioVenta = reader["Precio1"] != DBNull.Value ? Convert.ToDecimal(reader["Precio1"]) : 0m;
                    decimal precioCosto = reader["Precio2"] != DBNull.Value ? Convert.ToDecimal(reader["Precio2"]) : 0m;
                    decimal alicuotaIva = reader["Iva"] != DBNull.Value ? Convert.ToDecimal(reader["Iva"]) : 21m;
                    decimal impInt = reader["ImpInt"] != DBNull.Value ? Convert.ToDecimal(reader["ImpInt"]) : 0m;
                    
                    int familiaLegacy = reader["Familia"] != DBNull.Value ? Convert.ToInt32(reader["Familia"]) : 1;
                    int marcaLegacy = reader["Marca"] != DBNull.Value ? Convert.ToInt32(reader["Marca"]) : 1;
                    int provLegacy = reader["Proveedor"] != DBNull.Value ? Convert.ToInt32(reader["Proveedor"]) : 1;
                    int acumLegacy = reader["Acumulador"] != DBNull.Value ? Convert.ToInt32(reader["Acumulador"]) : 1;

                    // Si el código de barras está vacío, usar el código interno
                    if (string.IsNullOrWhiteSpace(ean)) ean = codigoInt;
                    if (string.IsNullOrWhiteSpace(ean)) continue; // No se puede agregar artículo sin código

                    // Validar FKs o crearlas al vuelo para referencialidad
                    if (familiaLegacy > 0 && !await _db.Familias.AnyAsync(f => f.Id == familiaLegacy))
                    {
                        _db.Familias.Add(new Familia { Id = familiaLegacy, Nombre = $"Familia {familiaLegacy}", IdDepartamento = 1, Activo = true });
                        await _db.SaveChangesAsync();
                    }
                    if (marcaLegacy > 0 && !await _db.Marcas.AnyAsync(m => m.Id == marcaLegacy))
                    {
                        _db.Marcas.Add(new Marca { Id = marcaLegacy, Nombre = $"Marca {marcaLegacy}", Activo = true });
                        await _db.SaveChangesAsync();
                    }
                    if (provLegacy > 0 && !await _db.Proveedores.AnyAsync(p => p.Id == provLegacy))
                    {
                        _db.Proveedores.Add(new Proveedor { Id = provLegacy, RazonSocial = $"Proveedor {provLegacy}", Cuit = "00000000000", Activo = true });
                        await _db.SaveChangesAsync();
                    }

                    var articulo = await _db.Articulos.FirstOrDefaultAsync(a => a.CodigoBarras == ean);
                    bool esNuevo = false;
                    if (articulo == null)
                    {
                        articulo = new Articulo { CodigoBarras = ean, FechaAlta = DateTime.UtcNow };
                        esNuevo = true;
                    }

                    articulo.Descripcion = descripcion;
                    articulo.DescripcionCorta = descCorta.Length > 40 ? descCorta.Substring(0, 40) : descCorta;
                    articulo.CodigoInterno = codigoInt;
                    articulo.PrecioVenta = precioVenta;
                    articulo.PrecioCosto = precioCosto;
                    articulo.AlicuotaIva = alicuotaIva;
                    articulo.ImpuestoInterno = impInt;
                    articulo.IdDepartamento = acumLegacy == 0 ? 1 : acumLegacy;
                    articulo.IdFamilia = familiaLegacy == 0 ? 1 : familiaLegacy;
                    articulo.IdMarca = marcaLegacy == 0 ? 1 : marcaLegacy;
                    articulo.IdProveedor = provLegacy == 0 ? 1 : provLegacy;
                    articulo.Activo = true;

                    if (esNuevo)
                    {
                        _db.Articulos.Add(articulo);
                        result.ArticulosCreados++;
                    }
                    else
                    {
                        result.ArticulosActualizados++;
                    }
                }
            }
            await _db.SaveChangesAsync();

            // 4. Promociones
            _logger.LogInformation("Importando Promociones...");
            var promoCmd = new OdbcCommand("SELECT Promocion, Tipo, Descripcion, FechaDesde, FechaHasta, HoraInicio, HoraFin, DiasSemana, Sucursales FROM Promociones", conn);
            using (var reader = promoCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    int codPromo = Convert.ToInt32(reader["Promocion"]);
                    int tipoAccion = Convert.ToInt32(reader["Tipo"]);
                    string desc = reader["Descripcion"]?.ToString() ?? "";
                    DateTime fechaD = Convert.ToDateTime(reader["FechaDesde"]);
                    DateTime fechaH = Convert.ToDateTime(reader["FechaHasta"]);
                    string horaI = reader["HoraInicio"]?.ToString() ?? "";
                    string horaF = reader["HoraFin"]?.ToString() ?? "";
                    string dias = reader["DiasSemana"]?.ToString() ?? "";
                    string sucursales = reader["Sucursales"]?.ToString() ?? "";

                    var promo = await _db.Promociones.FirstOrDefaultAsync(p => p.CodigoPromocion == codPromo);
                    bool esNuevo = false;
                    if (promo == null)
                    {
                        promo = new Promocion { CodigoPromocion = codPromo };
                        esNuevo = true;
                    }

                    promo.TipoAccion = tipoAccion;
                    promo.Descripcion = desc;
                    promo.FechaDesde = DateTime.SpecifyKind(fechaD, DateTimeKind.Utc);
                    promo.FechaHasta = DateTime.SpecifyKind(fechaH, DateTimeKind.Utc);
                    promo.HoraInicio = horaI;
                    promo.HoraFin = horaF;
                    promo.DiasSemana = dias;
                    promo.Sucursales = sucursales;
                    promo.Activa = true;

                    if (esNuevo)
                    {
                        _db.Promociones.Add(promo);
                        result.PromocionesCreadas++;
                    }
                    else
                    {
                        result.PromocionesActualizadas++;
                    }
                }
            }
            await _db.SaveChangesAsync();

            // 5. Promociones Condiciones
            _logger.LogInformation("Importando Condiciones de Promociones...");
            // Limpiamos existentes para evitar duplicados en la carga
            await _db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"PromocionesCondiciones\" CASCADE;");
            
            var condCmd = new OdbcCommand("SELECT Promocion, Item, Tipo, Codigo, ValorDesde, ValorHasta, TipoValor, Excluye FROM PromocionesCondiciones", conn);
            using (var reader = condCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    int codPromo = Convert.ToInt32(reader["Promocion"]);
                    string tipo = reader["Tipo"]?.ToString() ?? "";
                    int codigo = Convert.ToInt32(reader["Codigo"]);
                    decimal? valorDesde = reader["ValorDesde"] != DBNull.Value ? Convert.ToDecimal(reader["ValorDesde"]) : null;
                    decimal? valorHasta = reader["ValorHasta"] != DBNull.Value ? Convert.ToDecimal(reader["ValorHasta"]) : null;
                    string? tipoValor = reader["TipoValor"]?.ToString();
                    bool excluye = GetBool(reader["Excluye"]);
                    int item = Convert.ToInt32(reader["Item"]);

                    var promo = await _db.Promociones.FirstOrDefaultAsync(p => p.CodigoPromocion == codPromo);
                    if (promo == null) continue;

                    string ean = codigo.ToString();
                    var art = await _db.Articulos.FirstOrDefaultAsync(a => a.CodigoBarras == ean || a.CodigoInterno == ean);

                    _db.PromocionesCondiciones.Add(new PromocionCondicion
                    {
                        IdPromocion = promo.Id,
                        Tipo = tipo,
                        IdArticulo = art?.Id,
                        Cantidad = valorDesde ?? 0m,
                        Importe = valorDesde ?? 0m,
                        Item = item,
                        ValorDesde = valorDesde,
                        ValorHasta = valorHasta,
                        TipoValor = tipoValor,
                        Excluye = excluye
                    });
                    result.CondicionesCreadas++;
                }
            }
            await _db.SaveChangesAsync();

            // 6. Promociones Parametros Acción
            _logger.LogInformation("Importando Acciones de Promociones...");
            await _db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"PromocionesParametrosAccion\" CASCADE;");

            var accCmd = new OdbcCommand("SELECT Promocion, Item, Tipo, Codigo, Valor, TipoValor, AplicaSobre, Repeticiones, PrefiereMenorValor FROM PromocionesAcciones", conn);
            using (var reader = accCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    int codPromo = Convert.ToInt32(reader["Promocion"]);
                    string tipo = reader["Tipo"]?.ToString() ?? "";
                    int codigo = Convert.ToInt32(reader["Codigo"]);
                    decimal? valor = reader["Valor"] != DBNull.Value ? Convert.ToDecimal(reader["Valor"]) : null;
                    string? tipoValor = reader["TipoValor"]?.ToString();
                    string? aplicaSobre = reader["AplicaSobre"]?.ToString();
                    int? repeticiones = reader["Repeticiones"] != DBNull.Value ? Convert.ToInt32(reader["Repeticiones"]) : null;
                    bool prefiereMenorValor = GetBool(reader["PrefiereMenorValor"]);
                    int item = Convert.ToInt32(reader["Item"]);

                    var promo = await _db.Promociones.FirstOrDefaultAsync(p => p.CodigoPromocion == codPromo);
                    if (promo == null) continue;

                    string ean = codigo.ToString();
                    var art = await _db.Articulos.FirstOrDefaultAsync(a => a.CodigoBarras == ean || a.CodigoInterno == ean);

                    _db.PromocionesParametrosAccion.Add(new PromocionParametroAccion
                    {
                        IdPromocion = promo.Id,
                        Tipo = tipo,
                        IdArticulo = art?.Id,
                        Cantidad = valor ?? 0m,
                        Importe = valor ?? 0m,
                        Porcentaje = valor ?? 0m,
                        Item = item,
                        Valor = valor,
                        TipoValor = tipoValor,
                        AplicaSobre = aplicaSobre,
                        Repeticiones = repeticiones,
                        PrefiereMenorValor = prefiereMenorValor
                    });
                    result.AccionesCreadas++;
                }
            }
            await _db.SaveChangesAsync();

            // 7. Monedas Planes
            _logger.LogInformation("Importando Planes de Monedas/Tarjetas...");
            var planCmd = new OdbcCommand("SELECT Plan, Moneda, Detalle, Recargo, Acumulador FROM MonedasPlanes", conn);
            using (var reader = planCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    int planNro = Convert.ToInt32(reader["Plan"]);
                    int moneda = Convert.ToInt32(reader["Moneda"]);
                    string detalle = reader["Detalle"]?.ToString() ?? "";
                    decimal recargo = reader["Recargo"] != DBNull.Value ? Convert.ToDecimal(reader["Recargo"]) : 0m;
                    int acumulador = reader["Acumulador"] != DBNull.Value ? Convert.ToInt32(reader["Acumulador"]) : 0;

                    // Mapear Moneda a MedioPago en SuperPOS.
                    // Si el MedioPago (Id) no existe, mapear a Efectivo (1) o crearlo.
                    var medio = await _db.MediosPago.FirstOrDefaultAsync(m => m.Id == moneda);
                    if (medio == null)
                    {
                        // Crear medio pago genérico para no romper la FK
                        medio = new MedioPago { Id = moneda, Nombre = $"Medio Pago {moneda}", Tipo = TipoMedioPago.Vale, Activo = true };
                        _db.MediosPago.Add(medio);
                        await _db.SaveChangesAsync();
                    }

                    var plan = await _db.MonedasPlanes.FirstOrDefaultAsync(p => p.PlanNro == planNro && p.IdMedioPago == moneda);
                    bool esNuevo = false;
                    if (plan == null)
                    {
                        plan = new MonedaPlan { PlanNro = planNro, IdMedioPago = moneda };
                        esNuevo = true;
                    }

                    plan.Detalle = detalle;
                    plan.Recargo = recargo;
                    plan.Acumulador = acumulador;

                    if (esNuevo)
                    {
                        _db.MonedasPlanes.Add(plan);
                        result.PlanesCreados++;
                    }
                    else
                    {
                        result.PlanesActualizados++;
                    }
                }
            }
            await _db.SaveChangesAsync();

            // === IMPORTACIÓN DE TABLAS LEGACY ADICIONALES ===
            int? GetInt(object val) => val == DBNull.Value ? null : Convert.ToInt32(val);
            decimal? GetDecimal(object val) => val == DBNull.Value ? null : Convert.ToDecimal(val);
            bool GetBool(object val) => val != DBNull.Value && Convert.ToBoolean(val);
            string? GetStr(object val) => val == DBNull.Value ? null : val.ToString();
            short? GetShort(object val) => val == DBNull.Value ? null : Convert.ToInt16(val);

            // 8. Auditorias
            try
            {
                _logger.LogInformation("Importando Auditorias...");
                await _db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Auditorias\" CASCADE;");
                using (var cmd = new OdbcCommand("SELECT Id, Fecha, Hora, TipoCbte, NroCbte, Tipo, Codigo, Cantidad, Importe, Acumulador, EsEnvase, Zeta, CodigoInterno, ProcesoStock, Cliente, Cajero FROM Auditoria", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _db.Auditorias.Add(new Auditoria
                        {
                            Id = Convert.ToInt64(reader["Id"]),
                            Fecha = reader["Fecha"] != DBNull.Value ? DateTime.SpecifyKind(Convert.ToDateTime(reader["Fecha"]), DateTimeKind.Utc) : null,
                            Hora = GetStr(reader["Hora"]),
                            TipoCbte = GetStr(reader["TipoCbte"]),
                            NroCbte = GetInt(reader["NroCbte"]),
                            Tipo = GetStr(reader["Tipo"]),
                            Codigo = reader["Codigo"] != DBNull.Value ? Convert.ToDecimal(reader["Codigo"]).ToString("F0") : null,
                            Cantidad = GetDecimal(reader["Cantidad"]),
                            Importe = GetDecimal(reader["Importe"]),
                            Acumulador = GetInt(reader["Acumulador"]),
                            EsEnvase = GetBool(reader["EsEnvase"]),
                            Zeta = GetInt(reader["Zeta"]),
                            CodigoInterno = GetInt(reader["CodigoInterno"]),
                            ProcesoStock = GetInt(reader["ProcesoStock"]),
                            Cliente = GetInt(reader["Cliente"]),
                            Cajero = GetInt(reader["Cajero"])
                        });
                        result.AuditoriasCreadas++;
                    }
                }
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al importar Auditoria.");
            }

            // 9. CajerosLegacy
            try
            {
                _logger.LogInformation("Importando CajerosLegacy...");
                await _db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"CajerosLegacy\" CASCADE;");
                using (var cmd = new OdbcCommand("SELECT Codigo, Nombre, Clave, Nivel FROM Cajeros", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _db.CajerosLegacy.Add(new CajeroLegacy
                        {
                            Codigo = Convert.ToInt32(reader["Codigo"]),
                            Nombre = GetStr(reader["Nombre"]),
                            Clave = GetStr(reader["Clave"]),
                            Nivel = GetInt(reader["Nivel"])
                        });
                        result.CajerosCreados++;
                    }
                }
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al importar Cajeros.");
            }

            // 10. Cupones
            try
            {
                _logger.LogInformation("Importando Cupones...");
                await _db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Cupones\" CASCADE;");
                using (var cmd = new OdbcCommand("SELECT NroLinea, Texto FROM Cupones", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _db.Cupones.Add(new Cupon
                        {
                            NroLinea = Convert.ToInt32(reader["NroLinea"]),
                            Texto = GetStr(reader["Texto"])
                        });
                        result.CuponesCreados++;
                    }
                }
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al importar Cupones.");
            }

            // 11. Encabezados
            try
            {
                _logger.LogInformation("Importando Encabezados...");
                await _db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Encabezados\" CASCADE;");
                using (var cmd = new OdbcCommand("SELECT Linea, Texto, Doble FROM Encabezado", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _db.Encabezados.Add(new Encabezado
                        {
                            Linea = Convert.ToInt32(reader["Linea"]),
                            Texto = GetStr(reader["Texto"]),
                            Doble = GetBool(reader["Doble"])
                        });
                        result.EncabezadosCreados++;
                    }
                }
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al importar Encabezados.");
            }

            // 12. Fantasias
            try
            {
                _logger.LogInformation("Importando Fantasias...");
                await _db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Fantasias\" CASCADE;");
                using (var cmd = new OdbcCommand("SELECT Linea, Texto, Doble FROM Fantasia", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _db.Fantasias.Add(new Fantasia
                        {
                            Linea = Convert.ToInt32(reader["Linea"]),
                            Texto = GetStr(reader["Texto"]),
                            Doble = GetBool(reader["Doble"])
                        });
                        result.FantasiasCreados++;
                    }
                }
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al importar Fantasias.");
            }

            // 13. MonedasLegacy
            try
            {
                _logger.LogInformation("Importando MonedasLegacy...");
                await _db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"MonedasLegacy\" CASCADE;");
                using (var cmd = new OdbcCommand("SELECT Codigo, Descripcion, Cotizacion, Acumulador, Tipo, Cuenta, EsDivisa, Transmitido, Comision, DiasCobro, DescripcionImpresion, ImporteRetiro, MonedaCompletaRecargo FROM Monedas", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _db.MonedasLegacy.Add(new Moneda
                        {
                            Codigo = Convert.ToInt16(reader["Codigo"]),
                            Descripcion = GetStr(reader["Descripcion"]),
                            Cotizacion = GetDecimal(reader["Cotizacion"]),
                            Acumulador = GetInt(reader["Acumulador"]),
                            Tipo = GetStr(reader["Tipo"]),
                            Cuenta = GetDecimal(reader["Cuenta"]),
                            EsDivisa = GetBool(reader["EsDivisa"]),
                            Transmitido = GetBool(reader["Transmitido"]),
                            Comision = GetDecimal(reader["Comision"]),
                            DiasCobro = GetInt(reader["DiasCobro"]),
                            DescripcionImpresion = GetStr(reader["DescripcionImpresion"]),
                            ImporteRetiro = GetDecimal(reader["ImporteRetiro"]),
                            MonedaCompletaRecargo = GetInt(reader["MonedaCompletaRecargo"])
                        });
                        result.MonedasCreadas++;
                    }
                }
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al importar Monedas.");
            }

            // 14. MonedasPPH
            try
            {
                _logger.LogInformation("Importando MonedasPPH...");
                await _db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"MonedasPPH\" CASCADE;");
                using (var cmd = new OdbcCommand("SELECT Codigo, Descripcion, Cotizacion, Acumulador, Tipo, Cuenta, EsDivisa, Transmitido, Comision, DiasCobro, DescripcionImpresion, ImporteRetiro, MonedaCompletaRecargo FROM MonedasPPH", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _db.MonedasPPH.Add(new MonedaPPH
                        {
                            Codigo = Convert.ToInt16(reader["Codigo"]),
                            Descripcion = GetStr(reader["Descripcion"]),
                            Cotizacion = GetDecimal(reader["Cotizacion"]),
                            Acumulador = GetInt(reader["Acumulador"]),
                            Tipo = GetStr(reader["Tipo"]),
                            Cuenta = GetDecimal(reader["Cuenta"]),
                            EsDivisa = GetBool(reader["EsDivisa"]),
                            Transmitido = GetBool(reader["Transmitido"]),
                            Comision = GetDecimal(reader["Comision"]),
                            DiasCobro = GetInt(reader["DiasCobro"]),
                            DescripcionImpresion = GetStr(reader["DescripcionImpresion"]),
                            ImporteRetiro = GetDecimal(reader["ImporteRetiro"]),
                            MonedaCompletaRecargo = GetInt(reader["MonedaCompletaRecargo"])
                        });
                        result.MonedasPPHCreadas++;
                    }
                }
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al importar MonedasPPH.");
            }

            // 15. Pies
            try
            {
                _logger.LogInformation("Importando Pies...");
                await _db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Pies\" CASCADE;");
                using (var cmd = new OdbcCommand("SELECT Linea, Texto, Doble FROM Pie", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _db.Pies.Add(new Pie
                        {
                            Linea = Convert.ToInt32(reader["Linea"]),
                            Texto = GetStr(reader["Texto"]),
                            Doble = GetBool(reader["Doble"])
                        });
                        result.PiesCreados++;
                    }
                }
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al importar Pies.");
            }

            // 16. POS_Busquedas
            try
            {
                _logger.LogInformation("Importando POS_Busquedas...");
                await _db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"POS_Busquedas\" CASCADE;");
                using (var cmd = new OdbcCommand("SELECT Busqueda, Panel, PosX, PosY, Ancho, Alto, FontSize, Tabla, CampoFoco, TipoCargaDatos, FiltroBusqueda, TipoFiltroDatos, BusquedaRemota, Servidor, Base FROM POS_Busquedas", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _db.POS_Busquedas.Add(new POS_Busqueda
                        {
                            Busqueda = reader["Busqueda"].ToString()!,
                            Panel = GetInt(reader["Panel"]),
                            PosX = GetInt(reader["PosX"]),
                            PosY = GetInt(reader["PosY"]),
                            Ancho = GetInt(reader["Ancho"]),
                            Alto = GetInt(reader["Alto"]),
                            FontSize = GetInt(reader["FontSize"]),
                            Tabla = GetStr(reader["Tabla"]),
                            CampoFoco = GetInt(reader["CampoFoco"]),
                            TipoCargaDatos = GetStr(reader["TipoCargaDatos"]),
                            FiltroBusqueda = GetStr(reader["FiltroBusqueda"]),
                            TipoFiltroDatos = GetStr(reader["TipoFiltroDatos"]),
                            BusquedaRemota = GetBool(reader["BusquedaRemota"]),
                            Servidor = GetStr(reader["Servidor"]),
                            Base = GetStr(reader["Base"])
                        });
                        result.POS_BusquedasCreadas++;
                    }
                }
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al importar POS_Busquedas.");
            }

            // 17. POS_BusquedasCampos
            try
            {
                _logger.LogInformation("Importando POS_BusquedasCampos...");
                await _db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"POS_BusquedasCampos\" CASCADE;");
                using (var cmd = new OdbcCommand("SELECT Busqueda, Posicion, Campo, AnchoColumna, PosX, PosY, Ancho, Alto, FontSize, PosXEnLista, PosYEnLista, NroIngreso, CaracterComienzoBusqueda FROM POS_BusquedasCampos", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _db.POS_BusquedasCampos.Add(new POS_BusquedaCampo
                        {
                            Busqueda = reader["Busqueda"].ToString()!,
                            Posicion = Convert.ToInt32(reader["Posicion"]),
                            Campo = GetStr(reader["Campo"]),
                            AnchoColumna = GetInt(reader["AnchoColumna"]),
                            PosX = GetInt(reader["PosX"]),
                            PosY = GetInt(reader["PosY"]),
                            Ancho = GetInt(reader["Ancho"]),
                            Alto = GetInt(reader["Alto"]),
                            FontSize = GetInt(reader["FontSize"]),
                            PosXEnLista = GetInt(reader["PosXEnLista"]),
                            PosYEnLista = GetInt(reader["PosYEnLista"]),
                            NroIngreso = GetInt(reader["NroIngreso"]),
                            CaracterComienzoBusqueda = GetInt(reader["CaracterComienzoBusqueda"])
                        });
                        result.POS_BusquedasCamposCreados++;
                    }
                }
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al importar POS_BusquedasCampos.");
            }

            // 18. POS_Config
            try
            {
                _logger.LogInformation("Importando POS_Config...");
                await _db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"POS_Config\" CASCADE;");
                using (var cmd = new OdbcCommand("SELECT NroCaja, Animacion, PanelLogin, VentaCantidadMaxima, VentaImporteMaximo, VentaImporteMinimo, VentaCantidadDefecto, VentaCantidadMaximaPagos, VentaPedirCantidad, VerVideo, PanelPrincipal, PuertoScanner, PuertoDisplay, PuertoBalanza, PuertoFiscal, PathImagenesArticulos, PathImagenesCajeros, FiscalMarca, FiscalModelo, ModoCobro, NetoMinimoPercepcionIIBB, MuestraCodigoEnPantalla, RendicionGeneraRetiro, SubtotalObligatorio, ModoItem, ClienteObligatorio, SucursalFacturacion, SucursalFacturacion2, ConfirmaFacturacion, StockOnLine, SumaPuntos, UsaDescripcionLarga, ClienteFacturaCtaCte, PuntosXPeso, ImprimeEAN, ZetaObligatoria, ConfirmaZeta, ZetaEnviaVenta, ControlarCajon, PathImagenes, PathImagenesServidor, VentaImporteMinimoCbte, VentaImporteMaximoCbte, TruncaPuntos, PesosXPunto, ClienteFacturaPuntos, ObligarCierreCajero FROM POS_Config", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _db.POS_Config.Add(new POS_Config
                        {
                            NroCaja = Convert.ToInt32(reader["NroCaja"]),
                            Animacion = GetBool(reader["Animacion"]),
                            PanelLogin = GetInt(reader["PanelLogin"]),
                            VentaCantidadMaxima = GetDecimal(reader["VentaCantidadMaxima"]),
                            VentaImporteMaximo = GetDecimal(reader["VentaImporteMaximo"]),
                            VentaImporteMinimo = GetDecimal(reader["VentaImporteMinimo"]),
                            VentaCantidadDefecto = GetDecimal(reader["VentaCantidadDefecto"]),
                            VentaCantidadMaximaPagos = GetDecimal(reader["VentaCantidadMaximaPagos"]),
                            VentaPedirCantidad = GetBool(reader["VentaPedirCantidad"]),
                            VerVideo = GetBool(reader["VerVideo"]),
                            PanelPrincipal = GetInt(reader["PanelPrincipal"]),
                            PuertoScanner = GetStr(reader["PuertoScanner"]),
                            PuertoDisplay = GetStr(reader["PuertoDisplay"]),
                            PuertoBalanza = GetStr(reader["PuertoBalanza"]),
                            PuertoFiscal = GetStr(reader["PuertoFiscal"]),
                            PathImagenesArticulos = GetStr(reader["PathImagenesArticulos"]),
                            PathImagenesCajeros = GetStr(reader["PathImagenesCajeros"]),
                            FiscalMarca = GetStr(reader["FiscalMarca"]),
                            FiscalModelo = GetStr(reader["FiscalModelo"]),
                            ModoCobro = GetStr(reader["ModoCobro"]),
                            NetoMinimoPercepcionIIBB = GetInt(reader["NetoMinimoPercepcionIIBB"]),
                            MuestraCodigoEnPantalla = GetBool(reader["MuestraCodigoEnPantalla"]),
                            RendicionGeneraRetiro = GetBool(reader["RendicionGeneraRetiro"]),
                            SubtotalObligatorio = GetBool(reader["SubtotalObligatorio"]),
                            ModoItem = GetStr(reader["ModoItem"]),
                            ClienteObligatorio = GetBool(reader["ClienteObligatorio"]),
                            SucursalFacturacion = GetInt(reader["SucursalFacturacion"]),
                            SucursalFacturacion2 = GetInt(reader["SucursalFacturacion2"]),
                            ConfirmaFacturacion = GetBool(reader["ConfirmaFacturacion"]),
                            StockOnLine = GetBool(reader["StockOnLine"]),
                            SumaPuntos = GetBool(reader["SumaPuntos"]),
                            UsaDescripcionLarga = GetBool(reader["UsaDescripcionLarga"]),
                            ClienteFacturaCtaCte = GetBool(reader["ClienteFacturaCtaCte"]),
                            PuntosXPeso = GetInt(reader["PuntosXPeso"]),
                            ImprimeEAN = GetBool(reader["ImprimeEAN"]),
                            ZetaObligatoria = GetBool(reader["ZetaObligatoria"]),
                            ConfirmaZeta = GetBool(reader["ConfirmaZeta"]),
                            ZetaEnviaVenta = GetBool(reader["ZetaEnviaVenta"]),
                            ControlarCajon = GetBool(reader["ControlarCajon"]),
                            PathImagenes = GetStr(reader["PathImagenes"]),
                            PathImagenesServidor = GetStr(reader["PathImagenesServidor"]),
                            VentaImporteMinimoCbte = GetDecimal(reader["VentaImporteMinimoCbte"]),
                            VentaImporteMaximoCbte = GetDecimal(reader["VentaImporteMaximoCbte"]),
                            TruncaPuntos = GetBool(reader["TruncaPuntos"]),
                            PesosXPunto = GetDecimal(reader["PesosXPunto"]),
                            ClienteFacturaPuntos = GetBool(reader["ClienteFacturaPuntos"]),
                            ObligarCierreCajero = GetBool(reader["ObligarCierreCajero"])
                        });
                        result.POS_ConfigCreados++;
                    }
                }
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al importar POS_Config.");
            }

            // 19. POS_Cupones
            try
            {
                _logger.LogInformation("Importando POS_Cupones...");
                await _db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"POS_Cupones\" CASCADE;");
                using (var cmd = new OdbcCommand("SELECT NroCupon, Linea, Texto FROM POS_Cupones", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _db.POS_Cupones.Add(new POS_Cupon
                        {
                            NroCupon = Convert.ToInt32(reader["NroCupon"]),
                            Linea = Convert.ToInt32(reader["Linea"]),
                            Texto = GetStr(reader["Texto"])
                        });
                        result.POS_CuponesCreados++;
                    }
                }
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al importar POS_Cupones.");
            }

            // 20. POS_Eventos
            try
            {
                _logger.LogInformation("Importando POS_Eventos...");
                await _db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"POS_Eventos\" CASCADE;");
                using (var cmd = new OdbcCommand("SELECT Evento, Detalle, Tipo, EjecutarFuncionNro FROM POS_Eventos", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _db.POS_Eventos.Add(new POS_Evento
                        {
                            Evento = Convert.ToInt32(reader["Evento"]),
                            Detalle = GetStr(reader["Detalle"]),
                            Tipo = GetStr(reader["Tipo"]),
                            EjecutarFuncionNro = GetInt(reader["EjecutarFuncionNro"])
                        });
                        result.POS_EventosCreados++;
                    }
                }
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al importar POS_Eventos.");
            }

            // 21. POS_Formularios
            try
            {
                _logger.LogInformation("Importando POS_Formularios...");
                await _db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"POS_Formularios\" CASCADE;");
                using (var cmd = new OdbcCommand("SELECT NroFormulario, PosX, PosY, Ancho, Alto, EstadoVentana, TipoBorde FROM POS_Formularios", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _db.POS_Formularios.Add(new POS_Formulario
                        {
                            NroFormulario = Convert.ToInt32(reader["NroFormulario"]),
                            PosX = GetInt(reader["PosX"]),
                            PosY = GetInt(reader["PosY"]),
                            Ancho = GetInt(reader["Ancho"]),
                            Alto = GetInt(reader["Alto"]),
                            EstadoVentana = GetInt(reader["EstadoVentana"]),
                            TipoBorde = GetInt(reader["TipoBorde"])
                        });
                        result.POS_FormulariosCreados++;
                    }
                }
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al importar POS_Formularios.");
            }

            // 22. POS_Funciones
            try
            {
                _logger.LogInformation("Importando POS_Funciones...");
                await _db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"POS_Funciones\" CASCADE;");
                using (var cmd = new OdbcCommand("SELECT NroFuncion, Funcion, Acumulador, Descripcion, PosX, PosY, Ancho, Alto, Panel, MoverPanel, MoverPanelPos, LlamarFuncion, Codigo, FontSize, FontColor, Alineacion, Busqueda, ImporteObligatorio, Imagen, FocoEnIngreso, EsEnvase, PorcentajeMaximo, Nivel, Tiempo, EsCtaCte, AcumuladorVuelto, MonedaAjusteCotizacion, CantidadCupones, Formulario, NroEditVariable, AbreCajon, NroCupon FROM POS_Funciones", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _db.POS_Funciones.Add(new POS_Funcion
                        {
                            NroFuncion = Convert.ToInt32(reader["NroFuncion"]),
                            Funcion = GetStr(reader["Funcion"]),
                            Acumulador = GetInt(reader["Acumulador"]),
                            Descripcion = GetStr(reader["Descripcion"]),
                            PosX = GetInt(reader["PosX"]),
                            PosY = GetInt(reader["PosY"]),
                            Ancho = GetInt(reader["Ancho"]),
                            Alto = GetInt(reader["Alto"]),
                            Panel = GetInt(reader["Panel"]),
                            MoverPanel = GetInt(reader["MoverPanel"]),
                            MoverPanelPos = GetInt(reader["MoverPanelPos"]),
                            LlamarFuncion = GetInt(reader["LlamarFuncion"]),
                            Codigo = GetInt(reader["Codigo"]),
                            FontSize = GetInt(reader["FontSize"]),
                            FontColor = GetInt(reader["FontColor"]),
                            Alineacion = GetInt(reader["Alineacion"]),
                            Busqueda = GetStr(reader["Busqueda"]),
                            ImporteObligatorio = GetBool(reader["ImporteObligatorio"]),
                            Imagen = GetStr(reader["Imagen"]),
                            FocoEnIngreso = GetInt(reader["FocoEnIngreso"]),
                            EsEnvase = GetBool(reader["EsEnvase"]),
                            PorcentajeMaximo = GetDecimal(reader["PorcentajeMaximo"]),
                            Nivel = GetInt(reader["Nivel"]),
                            Tiempo = GetInt(reader["Tiempo"]),
                            EsCtaCte = GetBool(reader["EsCtaCte"]),
                            AcumuladorVuelto = GetInt(reader["AcumuladorVuelto"]),
                            MonedaAjusteCotizacion = GetInt(reader["MonedaAjusteCotizacion"]),
                            CantidadCupones = GetInt(reader["CantidadCupones"]),
                            Formulario = GetInt(reader["Formulario"]),
                            NroEditVariable = GetInt(reader["NroEditVariable"]),
                            AbreCajon = GetBool(reader["AbreCajon"]),
                            NroCupon = GetInt(reader["NroCupon"])
                        });
                        result.POS_FuncionesCreadas++;
                    }
                }
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al importar POS_Funciones.");
            }

            // 23. POS_GrillaColumnas
            try
            {
                _logger.LogInformation("Importando POS_GrillaColumnas...");
                await _db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"POS_GrillaColumnas\" CASCADE;");
                using (var cmd = new OdbcCommand("SELECT Titulo, Nombre, Ancho, Alineamiento, Orden FROM POS_Grilla_Columnas", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _db.POS_GrillaColumnas.Add(new POS_GrillaColumna
                        {
                            Nombre = reader["Nombre"].ToString()!,
                            Titulo = GetStr(reader["Titulo"]),
                            Ancho = GetInt(reader["Ancho"]),
                            Alineamiento = GetStr(reader["Alineamiento"]),
                            Orden = GetInt(reader["Orden"])
                        });
                        result.POS_GrillaColumnasCreadas++;
                    }
                }
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al importar POS_GrillaColumnas.");
            }

            // 24. POS_GrillasVenta
            try
            {
                _logger.LogInformation("Importando POS_GrillasVenta...");
                await _db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"POS_GrillasVenta\" CASCADE;");
                using (var cmd = new OdbcCommand("SELECT NroGrilla, Panel, Descripcion, PosX, PosY, Ancho, Alto, FontSize, MaximoArticulos, ExtraTipo, ExtraPanel, ExtraPosX, ExtraPosY, ExtraAncho, ExtraAlto, DobleClickLlamaFuncion FROM POS_GrillasVenta", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _db.POS_GrillasVenta.Add(new POS_GrillaVenta
                        {
                            NroGrilla = Convert.ToInt32(reader["NroGrilla"]),
                            Panel = GetInt(reader["Panel"]),
                            Descripcion = GetStr(reader["Descripcion"]),
                            PosX = GetInt(reader["PosX"]),
                            PosY = GetInt(reader["PosY"]),
                            Ancho = GetInt(reader["Ancho"]),
                            Alto = GetInt(reader["Alto"]),
                            FontSize = GetInt(reader["FontSize"]),
                            MaximoArticulos = GetInt(reader["MaximoArticulos"]),
                            ExtraTipo = GetStr(reader["ExtraTipo"]),
                            ExtraPanel = GetInt(reader["ExtraPanel"]),
                            ExtraPosX = GetInt(reader["ExtraPosX"]),
                            ExtraPosY = GetInt(reader["ExtraPosY"]),
                            ExtraAncho = GetInt(reader["ExtraAncho"]),
                            ExtraAlto = GetInt(reader["ExtraAlto"]),
                            DobleClickLlamaFuncion = GetInt(reader["DobleClickLlamaFuncion"])
                        });
                        result.POS_GrillasVentaCreadas++;
                    }
                }
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al importar POS_GrillasVenta.");
            }

            // 25. POS_Imagenes
            try
            {
                _logger.LogInformation("Importando POS_Imagenes...");
                await _db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"POS_Imagenes\" CASCADE;");
                using (var cmd = new OdbcCommand("SELECT NroImagen, CampoContenido, Panel, PosX, PosY, Ancho, Alto FROM POS_Imagenes", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _db.POS_Imagenes.Add(new POS_Imagen
                        {
                            NroImagen = Convert.ToInt32(reader["NroImagen"]),
                            CampoContenido = GetStr(reader["CampoContenido"]),
                            Panel = GetInt(reader["Panel"]),
                            PosX = GetInt(reader["PosX"]),
                            PosY = GetInt(reader["PosY"]),
                            Ancho = GetInt(reader["Ancho"]),
                            Alto = GetInt(reader["Alto"])
                        });
                        result.POS_ImagenesCreadas++;
                    }
                }
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al importar POS_Imagenes.");
            }

            // 26. POS_Ingresos
            try
            {
                _logger.LogInformation("Importando POS_Ingresos...");
                await _db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"POS_Ingresos\" CASCADE;");
                using (var cmd = new OdbcCommand("SELECT NroIngreso, Panel, Descripcion, PosX, PosY, Ancho, Alto, LargoMaximo, FontSize, PasswordChar FROM POS_Ingresos", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _db.POS_Ingresos.Add(new POS_Ingreso
                        {
                            NroIngreso = Convert.ToInt32(reader["NroIngreso"]),
                            Panel = GetInt(reader["Panel"]),
                            Descripcion = GetStr(reader["Descripcion"]),
                            PosX = GetInt(reader["PosX"]),
                            PosY = GetInt(reader["PosY"]),
                            Ancho = GetInt(reader["Ancho"]),
                            Alto = GetInt(reader["Alto"]),
                            LargoMaximo = GetInt(reader["LargoMaximo"]),
                            FontSize = GetInt(reader["FontSize"]),
                            PasswordChar = GetInt(reader["PasswordChar"])
                        });
                        result.POS_IngresosCreados++;
                    }
                }
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al importar POS_Ingresos.");
            }

            // 27. POS_NumerosComprobantes
            try
            {
                _logger.LogInformation("Importando POS_NumerosComprobantes...");
                await _db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"POS_NumerosComprobantes\" CASCADE;");
                using (var cmd = new OdbcCommand("SELECT TipoCbte, NroSiguienteCbte, FormatoImpresion, SumaPuntos FROM POS_NumerosComprobantes", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _db.POS_NumerosComprobantes.Add(new POS_NumeroComprobante
                        {
                            TipoCbte = reader["TipoCbte"].ToString()!,
                            NroSiguienteCbte = GetInt(reader["NroSiguienteCbte"]),
                            FormatoImpresion = GetInt(reader["FormatoImpresion"]),
                            SumaPuntos = GetBool(reader["SumaPuntos"])
                        });
                        result.POS_NumerosComprobantesCreados++;
                    }
                }
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al importar POS_NumerosComprobantes.");
            }

            // 28. POS_Paneles
            try
            {
                _logger.LogInformation("Importando POS_Paneles...");
                await _db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"POS_Paneles\" CASCADE;");
                using (var cmd = new OdbcCommand("SELECT Panel, Titulo, Ancho, Alto, GrosorBorde, Color, Animacion, Imagen, Formulario FROM POS_Paneles", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _db.POS_Paneles.Add(new POS_Panel
                        {
                            Panel = Convert.ToInt32(reader["Panel"]),
                            Titulo = GetStr(reader["Titulo"]),
                            Ancho = GetInt(reader["Ancho"]),
                            Alto = GetInt(reader["Alto"]),
                            GrosorBorde = GetInt(reader["GrosorBorde"]),
                            Color = GetInt(reader["Color"]),
                            Animacion = GetBool(reader["Animacion"]),
                            Imagen = GetStr(reader["Imagen"]),
                            Formulario = GetInt(reader["Formulario"])
                        });
                        result.POS_PanelesCreados++;
                    }
                }
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al importar POS_Paneles.");
            }

            // 29. POS_PanelesPosiciones
            try
            {
                _logger.LogInformation("Importando POS_PanelesPosiciones...");
                await _db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"POS_PanelesPosiciones\" CASCADE;");
                using (var cmd = new OdbcCommand("SELECT Panel, Posicion, PosX, PosY FROM POS_PanelesPosiciones", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _db.POS_PanelesPosiciones.Add(new POS_PanelPosicion
                        {
                            Panel = Convert.ToInt32(reader["Panel"]),
                            Posicion = Convert.ToInt32(reader["Posicion"]),
                            PosX = GetInt(reader["PosX"]),
                            PosY = GetInt(reader["PosY"])
                        });
                        result.POS_PanelesPosicionesCreados++;
                    }
                }
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al importar POS_PanelesPosiciones.");
            }

            // 30. POS_Rendiciones
            try
            {
                _logger.LogInformation("Importando POS_Rendiciones...");
                await _db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"POS_Rendiciones\" CASCADE;");
                using (var cmd = new OdbcCommand("SELECT Rendicion, Panel, PosX, PosY, Ancho, Alto, FontSize, MuestraImportesCaja FROM POS_Rendiciones", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _db.POS_Rendiciones.Add(new POS_Rendicion
                        {
                            Rendicion = reader["Rendicion"].ToString()!,
                            Panel = GetInt(reader["Panel"]),
                            PosX = GetInt(reader["PosX"]),
                            PosY = GetInt(reader["PosY"]),
                            Ancho = GetInt(reader["Ancho"]),
                            Alto = GetInt(reader["Alto"]),
                            FontSize = GetInt(reader["FontSize"]),
                            MuestraImportesCaja = GetBool(reader["MuestraImportesCaja"])
                        });
                        result.POS_RendicionesCreados++;
                    }
                }
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al importar POS_Rendiciones.");
            }

            // 31. POS_Teclas
            try
            {
                _logger.LogInformation("Importando POS_Teclas...");
                await _db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"POS_Teclas\" CASCADE;");
                using (var cmd = new OdbcCommand("SELECT IngresoNro, Tecla, FuncionNro FROM POS_Teclas", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _db.POS_Teclas.Add(new POS_Tecla
                        {
                            IngresoNro = Convert.ToInt32(reader["IngresoNro"]),
                            Tecla = Convert.ToInt32(reader["Tecla"]),
                            FuncionNro = GetInt(reader["FuncionNro"])
                        });
                        result.POS_TeclasCreadas++;
                    }
                }
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al importar POS_Teclas.");
            }

            // 32. POS_Videos
            try
            {
                _logger.LogInformation("Importando POS_Videos...");
                await _db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"POS_Videos\" CASCADE;");
                using (var cmd = new OdbcCommand("SELECT NroVideo, Panel, PathContenido, PosX, PosY, Ancho, Alto, PosicionPanelPlay, PathVideosServidor FROM POS_Videos", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _db.POS_Videos.Add(new POS_Video
                        {
                            NroVideo = Convert.ToInt32(reader["NroVideo"]),
                            Panel = GetInt(reader["Panel"]),
                            PathContenido = GetStr(reader["PathContenido"]),
                            PosX = GetInt(reader["PosX"]),
                            PosY = GetInt(reader["PosY"]),
                            Ancho = GetInt(reader["Ancho"]),
                            Alto = GetInt(reader["Alto"]),
                            PosicionPanelPlay = GetInt(reader["PosicionPanelPlay"]),
                            PathVideosServidor = GetStr(reader["PathVideosServidor"])
                        });
                        result.POS_VideosCreados++;
                    }
                }
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al importar POS_Videos.");
            }

            await transaction.CommitAsync();
            _logger.LogInformation("Importación finalizada con éxito.");
            result.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al importar datos desde la base MDB");
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    private class PromocionConditionDto
    {
        public int IdPromocion { get; set; }
        public string Tipo { get; set; } = "";
        public int? IdArticulo { get; set; }
        public decimal Cantidad { get; set; }
        public decimal Importe { get; set; }
        public int Item { get; set; }
    }
}

public class ImportResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    public int DepartamentosCreados { get; set; }
    public int DepartamentosActualizados { get; set; }

    public int ClientesCreados { get; set; }
    public int ClientesActualizados { get; set; }

    public int ArticulosCreados { get; set; }
    public int ArticulosActualizados { get; set; }

    public int PromocionesCreadas { get; set; }
    public int PromocionesActualizadas { get; set; }

    public int CondicionesCreadas { get; set; }
    public int AccionesCreadas { get; set; }

    public int PlanesCreados { get; set; }
    public int PlanesActualizados { get; set; }

    // Nuevas métricas para tablas Legacy
    public int AuditoriasCreadas { get; set; }
    public int CajerosCreados { get; set; }
    public int CuponesCreados { get; set; }
    public int EncabezadosCreados { get; set; }
    public int FantasiasCreados { get; set; }
    public int MonedasCreadas { get; set; }
    public int MonedasPPHCreadas { get; set; }
    public int PiesCreados { get; set; }
    public int POS_BusquedasCreadas { get; set; }
    public int POS_BusquedasCamposCreados { get; set; }
    public int POS_ConfigCreados { get; set; }
    public int POS_CuponesCreados { get; set; }
    public int POS_EventosCreados { get; set; }
    public int POS_FormulariosCreados { get; set; }
    public int POS_FuncionesCreadas { get; set; }
    public int POS_GrillaColumnasCreadas { get; set; }
    public int POS_GrillasVentaCreadas { get; set; }
    public int POS_ImagenesCreadas { get; set; }
    public int POS_IngresosCreados { get; set; }
    public int POS_NumerosComprobantesCreados { get; set; }
    public int POS_PanelesCreados { get; set; }
    public int POS_PanelesPosicionesCreados { get; set; }
    public int POS_RendicionesCreados { get; set; }
    public int POS_TeclasCreadas { get; set; }
    public int POS_VideosCreados { get; set; }
}

