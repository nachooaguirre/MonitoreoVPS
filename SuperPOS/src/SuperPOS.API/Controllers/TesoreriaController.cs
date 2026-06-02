using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperPOS.API.Data;
using SuperPOS.API.Helpers;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TesoreriaController(SuperPOSDbContext db) : ControllerBase
{
    // ═══════════════════════════════════════════
    // CUENTAS
    // ═══════════════════════════════════════════
    [HttpGet("cuentas")]
    public async Task<IActionResult> GetCuentas()
    {
        var cuentas = await db.CuentasTesoreria
            .Where(c => c.Activa)
            .OrderBy(c => c.Tipo).ThenBy(c => c.Nombre)
            .Select(c => new { c.Id, c.Nombre, c.Tipo, c.NroCuenta, c.Banco, c.SaldoActual, c.Activa })
            .ToListAsync();
        return Ok(cuentas);
    }

    [HttpPost("cuentas")]
    public async Task<IActionResult> CrearCuenta([FromBody] CuentaTesoreria cuenta)
    {
        cuenta.FechaAlta = DateTime.UtcNow;
        cuenta.SaldoActual = cuenta.SaldoInicial;
        db.CuentasTesoreria.Add(cuenta);
        await db.SaveChangesAsync();
        return Ok(new { cuenta.Id, cuenta.Nombre, cuenta.SaldoActual });
    }

    // ═══════════════════════════════════════════
    // MOVIMIENTOS
    // ═══════════════════════════════════════════
    [HttpGet("movimientos")]
    public async Task<IActionResult> GetMovimientos(
        [FromQuery] int? idCuenta,
        [FromQuery] TipoMovimientoTesoreria? tipo,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100)
    {
        var q = db.MovimientosTesoreria.AsQueryable();
        if (idCuenta.HasValue) q = q.Where(m => m.IdCuenta == idCuenta.Value || m.IdCuentaDestino == idCuenta.Value);
        if (tipo.HasValue) q = q.Where(m => m.Tipo == tipo.Value);
        if (desde.HasValue) q = q.Where(m => m.Fecha >= desde.Value.ToUtc());
        if (hasta.HasValue) q = q.Where(m => m.Fecha <= hasta.Value.ToUtc().AddDays(1));

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(m => m.Fecha)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(m => new
            {
                m.Id,
                m.Fecha,
                m.Tipo,
                m.Monto,
                m.Concepto,
                m.NroDocumento,
                m.Beneficiario,
                m.Conciliado,
                CuentaNombre = m.Cuenta != null ? m.Cuenta.Nombre : null,
                CuentaDestinoNombre = m.CuentaDestino != null ? m.CuentaDestino.Nombre : null
            })
            .ToListAsync();
        return Ok(new { total, items });
    }

    [HttpPost("movimientos")]
    public async Task<IActionResult> RegistrarMovimiento([FromBody] MovimientoTesoreria mov)
    {
        mov.Fecha = DateTime.UtcNow;
        db.MovimientosTesoreria.Add(mov);

        // Actualizar saldo cuenta origen
        var cuenta = await db.CuentasTesoreria.FindAsync(mov.IdCuenta);
        if (cuenta != null)
        {
            cuenta.SaldoActual += mov.Tipo == TipoMovimientoTesoreria.Ingreso || mov.Tipo == TipoMovimientoTesoreria.AjustePositivo
                ? mov.Monto : -mov.Monto;
        }

        // Si es transferencia, actualizar cuenta destino
        if (mov.Tipo == TipoMovimientoTesoreria.Transferencia && mov.IdCuentaDestino.HasValue)
        {
            var cuentaDest = await db.CuentasTesoreria.FindAsync(mov.IdCuentaDestino.Value);
            if (cuentaDest != null) cuentaDest.SaldoActual += mov.Monto;
        }

        await db.SaveChangesAsync();
        return Ok(new { mov.Id, mov.Fecha, mov.Monto });
    }

    [HttpPut("movimientos/conciliar")]
    public async Task<IActionResult> ConciliarMovimientos([FromBody] List<ConciliacionItemDto> items)
    {
        if (items == null || items.Count == 0) return BadRequest("No se enviaron ítems para conciliar.");

        foreach (var item in items)
        {
            var mov = await db.MovimientosTesoreria.FindAsync(item.IdMovimiento);
            if (mov != null)
            {
                mov.Conciliado = item.Conciliado;
                mov.FechaConciliacion = item.Conciliado ? DateTime.UtcNow : null;
            }
        }

        await db.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("depositos")]
    public async Task<IActionResult> RegistrarDeposito([FromBody] RegistrarDepositoDto dto)
    {
        if (dto == null) return BadRequest("Datos inválidos.");
        if (dto.MontoEfectivo == 0 && (dto.ChequesIds == null || dto.ChequesIds.Count == 0))
            return BadRequest("El depósito debe contener efectivo y/o cheques.");

        using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var cuentaDest = await db.CuentasTesoreria.FindAsync(dto.IdCuentaDestino);
            if (cuentaDest == null) return BadRequest("La cuenta destino no existe.");
            if (cuentaDest.Tipo != TipoCuentaTesoreria.CuentaCorrienteBancaria && cuentaDest.Tipo != TipoCuentaTesoreria.CajaAhorroBancaria)
                return BadRequest("La cuenta destino debe ser bancaria.");

            decimal totalDeposito = dto.MontoEfectivo;

            // 1. Procesar Cheques
            if (dto.ChequesIds != null && dto.ChequesIds.Count > 0)
            {
                var cheques = await db.Cheques
                    .Where(c => dto.ChequesIds.Contains(c.Id))
                    .ToListAsync();

                if (cheques.Count != dto.ChequesIds.Count)
                    return BadRequest("Algunos cheques no fueron encontrados.");

                foreach (var cheque in cheques)
                {
                    if (cheque.Estado != EstadoCheque.Cartera)
                        return BadRequest($"El cheque N° {cheque.NroCheque} no está en cartera.");

                    cheque.Estado = EstadoCheque.Depositado;
                    cheque.IdCuenta = dto.IdCuentaDestino;
                    totalDeposito += cheque.Monto;
                }
            }

            // 2. Procesar Efectivo
            if (dto.MontoEfectivo > 0)
            {
                if (!dto.IdCuentaOrigen.HasValue)
                    return BadRequest("Debe especificar la cuenta de origen (Caja) para depositar efectivo.");

                var cuentaOrig = await db.CuentasTesoreria.FindAsync(dto.IdCuentaOrigen.Value);
                if (cuentaOrig == null) return BadRequest("La cuenta de origen no existe.");
                if (cuentaOrig.Tipo != TipoCuentaTesoreria.CajaEfectivo)
                    return BadRequest("La cuenta de origen debe ser de tipo Caja/Efectivo.");

                if (cuentaOrig.SaldoActual < dto.MontoEfectivo)
                    return BadRequest($"Saldo insuficiente en la caja de origen.");

                cuentaOrig.SaldoActual -= dto.MontoEfectivo;

                // Registrar movimiento de egreso en la Caja
                var movEgreso = new MovimientoTesoreria
                {
                    IdCuenta = dto.IdCuentaOrigen.Value,
                    Tipo = TipoMovimientoTesoreria.Egreso,
                    Fecha = dto.Fecha.ToUniversalTime(),
                    Monto = dto.MontoEfectivo,
                    Concepto = $"Depósito efectivo en {cuentaDest.Nombre} - Boleta N° {dto.NroComprobante}",
                    NroDocumento = dto.NroComprobante,
                    Observaciones = dto.Observaciones,
                    IdUsuario = dto.IdUsuario
                };
                db.MovimientosTesoreria.Add(movEgreso);
            }

            // 3. Aumentar saldo cuenta destino
            cuentaDest.SaldoActual += totalDeposito;

            // Registrar movimiento de ingreso en el Banco
            var conceptoBanco = $"Depósito Boleta N° {dto.NroComprobante}";
            if (dto.ChequesIds != null && dto.ChequesIds.Count > 0)
            {
                conceptoBanco += $" ({dto.ChequesIds.Count} chq)";
            }
            if (dto.MontoEfectivo > 0)
            {
                conceptoBanco += $" + ${dto.MontoEfectivo:N2} efc";
            }

            var movIngreso = new MovimientoTesoreria
            {
                IdCuenta = dto.IdCuentaDestino,
                Tipo = TipoMovimientoTesoreria.Ingreso,
                Fecha = dto.Fecha.ToUniversalTime(),
                Monto = totalDeposito,
                Concepto = conceptoBanco,
                NroDocumento = dto.NroComprobante,
                Observaciones = dto.Observaciones,
                IdUsuario = dto.IdUsuario
            };
            db.MovimientosTesoreria.Add(movIngreso);

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { Total = totalDeposito });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, $"Error interno al registrar depósito: {ex.Message}");
        }
     }

    [HttpGet("saldos")]
    public async Task<IActionResult> GetSaldos()
    {
        var cuentas = await db.CuentasTesoreria.Where(c => c.Activa).ToListAsync();
        return Ok(new
        {
            TotalEfectivo  = cuentas.Where(c => c.Tipo == TipoCuentaTesoreria.CajaEfectivo).Sum(c => c.SaldoActual),
            TotalBancos    = cuentas.Where(c => c.Tipo is TipoCuentaTesoreria.CuentaCorrienteBancaria or TipoCuentaTesoreria.CajaAhorroBancaria).Sum(c => c.SaldoActual),
            TotalGeneral   = cuentas.Sum(c => c.SaldoActual),
            Cuentas        = cuentas.Select(c => new { c.Id, c.Nombre, c.Tipo, c.SaldoActual })
        });
    }

    // ═══════════════════════════════════════════
    // CHEQUERAS
    // ═══════════════════════════════════════════
    [HttpGet("chequeras")]
    public async Task<IActionResult> GetChequeras()
    {
        var list = await db.Chequeras
            .Include(c => c.Cuenta)
            .OrderBy(c => c.Cuenta!.Nombre).ThenBy(c => c.Nombre)
            .Select(c => new
            {
                c.Id,
                c.IdCuentaTesoreria,
                c.Nombre,
                c.Desde,
                c.Hasta,
                c.SiguienteNumero,
                c.Tipo,
                c.Activa,
                c.FechaAlta,
                CuentaNombre = c.Cuenta != null ? c.Cuenta.Nombre : null
            })
            .ToListAsync();
        return Ok(list);
    }

    [HttpGet("chequeras/cuenta/{idCuenta}")]
    public async Task<IActionResult> GetChequerasPorCuenta(int idCuenta)
    {
        var list = await db.Chequeras
            .Where(c => c.IdCuentaTesoreria == idCuenta && c.Activa)
            .OrderBy(c => c.Nombre)
            .ToListAsync();
        return Ok(list);
    }

    [HttpPost("chequeras")]
    public async Task<IActionResult> RegistrarChequera([FromBody] Chequera chequera)
    {
        if (chequera == null) return BadRequest("Datos inválidos.");
        chequera.FechaAlta = DateTime.UtcNow;
        
        var cuenta = await db.CuentasTesoreria.FindAsync(chequera.IdCuentaTesoreria);
        if (cuenta == null) return BadRequest("La cuenta bancaria seleccionada no existe.");

        chequera.SiguienteNumero = chequera.Desde;

        db.Chequeras.Add(chequera);
        await db.SaveChangesAsync();
        return Ok(chequera);
    }

    [HttpGet("chequeras/{id}/numeros-disponibles")]
    public async Task<IActionResult> GetNumerosDisponibles(int id)
    {
        var chequera = await db.Chequeras.FindAsync(id);
        if (chequera == null) return NotFound("Chequera no encontrada.");

        if (!int.TryParse(chequera.Desde, out int start) || !int.TryParse(chequera.Hasta, out int end))
        {
            return Ok(new List<string> { chequera.SiguienteNumero });
        }

        var nrosUsados = await db.Cheques
            .Where(c => c.IdCuenta == chequera.IdCuentaTesoreria && c.Tipo == TipoCheque.Emitido)
            .Select(c => c.NroCheque)
            .ToListAsync();

        var usadosSet = new HashSet<string>(nrosUsados);
        var disponibles = new List<string>();
        int len = chequera.Desde.Length;

        for (int i = start; i <= end; i++)
        {
            var nroStr = i.ToString().PadLeft(len, '0');
            if (!usadosSet.Contains(nroStr))
            {
                disponibles.Add(nroStr);
                if (disponibles.Count >= 100) break;
            }
        }

        return Ok(disponibles);
    }

    // ═══════════════════════════════════════════
    // BANCOS (CATÁLOGO)
    // ═══════════════════════════════════════════
    [HttpGet("bancos")]
    public async Task<IActionResult> GetBancos()
    {
        var list = await db.Bancos
            .Where(b => b.Activo)
            .OrderBy(b => b.Nombre)
            .ToListAsync();
        return Ok(list);
    }

    [HttpPost("bancos")]
    public async Task<IActionResult> RegistrarBanco([FromBody] Banco banco)
    {
        if (banco == null) return BadRequest("Datos inválidos.");
        db.Bancos.Add(banco);
        await db.SaveChangesAsync();
        return Ok(banco);
    }

    // ═══════════════════════════════════════════
    // REPORTES Y PROYECCIÓN
    // ═══════════════════════════════════════════
    [HttpGet("reportes/cheques")]
    public async Task<IActionResult> GetReporteCheques(
        [FromQuery] TipoCheque? tipo,
        [FromQuery] EstadoCheque? estado,
        [FromQuery] string? banco,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta)
    {
        var q = db.Cheques.AsQueryable();
        if (tipo.HasValue) q = q.Where(c => c.Tipo == tipo.Value);
        if (estado.HasValue) q = q.Where(c => c.Estado == estado.Value);
        if (!string.IsNullOrWhiteSpace(banco)) q = q.Where(c => c.Banco == banco);
        if (desde.HasValue) q = q.Where(c => c.FechaPago >= desde.Value.ToUtc());
        if (hasta.HasValue) q = q.Where(c => c.FechaPago <= hasta.Value.ToUtc());

        var list = await q.OrderBy(c => c.FechaPago)
            .Select(c => new
            {
                c.Id,
                c.Tipo,
                c.Estado,
                c.NroCheque,
                c.Banco,
                c.Monto,
                c.FechaEmision,
                c.FechaPago,
                c.Librador,
                c.EsRechazado,
                ClienteNombre = c.Cliente != null ? c.Cliente.RazonSocial : null,
                ProveedorNombre = c.Proveedor != null ? c.Proveedor.RazonSocial : null
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpGet("reportes/depositos")]
    public async Task<IActionResult> GetReporteDepositos(
        [FromQuery] int? idCuentaBanco,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta)
    {
        var q = db.MovimientosTesoreria
            .Include(m => m.Cuenta)
            .Where(m => m.Cuenta!.Tipo == TipoCuentaTesoreria.CuentaCorrienteBancaria || m.Cuenta!.Tipo == TipoCuentaTesoreria.CajaAhorroBancaria);

        if (idCuentaBanco.HasValue) q = q.Where(m => m.IdCuenta == idCuentaBanco.Value);
        if (desde.HasValue) q = q.Where(m => m.Fecha >= desde.Value.ToUtc());
        if (hasta.HasValue) q = q.Where(m => m.Fecha <= hasta.Value.ToUtc().AddDays(1));

        q = q.Where(m => m.Tipo == TipoMovimientoTesoreria.Ingreso && !string.IsNullOrEmpty(m.NroDocumento));

        var list = await q.OrderByDescending(m => m.Fecha)
            .Select(m => new
            {
                m.Id,
                m.Fecha,
                m.Monto,
                m.Concepto,
                m.NroDocumento,
                m.Beneficiario,
                m.IdCuenta,
                CuentaNombre = m.Cuenta != null ? m.Cuenta.Nombre : null
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpGet("reportes/proyeccion")]
    public async Task<IActionResult> GetProyeccionFinanciera(
        [FromQuery] int idCuentaBanco,
        [FromQuery] DateTime fechaHasta)
    {
        var cuenta = await db.CuentasTesoreria.FindAsync(idCuentaBanco);
        if (cuenta == null) return NotFound("La cuenta corriente bancaria no existe.");

        var fechaDesde = DateTime.Today.ToUniversalTime();
        var utcFechaHasta = fechaHasta.ToUniversalTime().Date.AddDays(1);

        var chequesACobrar = await db.Cheques
            .Where(c => c.Tipo == TipoCheque.Recibido && c.Estado == EstadoCheque.Cartera 
                && c.FechaPago >= fechaDesde && c.FechaPago < utcFechaHasta)
            .Select(c => new
            {
                c.Id,
                c.NroCheque,
                c.Banco,
                c.Monto,
                c.FechaPago,
                Detalle = "Cheque Recibido N° " + c.NroCheque + " (" + c.Librador + ")",
                EsIngreso = true
            })
            .ToListAsync();

        var chequesADebitar = await db.Cheques
            .Where(c => c.Tipo == TipoCheque.Emitido && c.Estado == EstadoCheque.Entregado
                && c.IdCuenta == idCuentaBanco
                && c.FechaPago >= fechaDesde && c.FechaPago < utcFechaHasta)
            .Select(c => new
            {
                c.Id,
                c.NroCheque,
                c.Banco,
                c.Monto,
                c.FechaPago,
                Detalle = "Cheque Emitido N° " + c.NroCheque + " (a proveedor)",
                EsIngreso = false
            })
            .ToListAsync();

        var itemsProyeccion = chequesACobrar.Select(c => new
        {
            c.Id,
            c.NroCheque,
            c.Banco,
            c.Monto,
            Fecha = c.FechaPago.ToLocalTime().Date,
            c.Detalle,
            c.EsIngreso
        })
        .Concat(chequesADebitar.Select(c => new
        {
            c.Id,
            c.NroCheque,
            c.Banco,
            c.Monto,
            Fecha = c.FechaPago.ToLocalTime().Date,
            c.Detalle,
            c.EsIngreso
        }))
        .OrderBy(c => c.Fecha)
        .ToList();

        decimal saldoActual = cuenta.SaldoActual;
        decimal saldoProyectado = saldoActual;

        var proyDiaria = new List<object>();
        var fechaActual = DateTime.Today.Date;
        var finProyeccion = fechaHasta.Date;

        while (fechaActual <= finProyeccion)
        {
            var delDia = itemsProyeccion.Where(item => item.Fecha == fechaActual).ToList();
            decimal ingresos = delDia.Where(item => item.EsIngreso).Sum(item => item.Monto);
            decimal egresos = delDia.Where(item => !item.EsIngreso).Sum(item => item.Monto);

            saldoProyectado += (ingresos - egresos);

            proyDiaria.Add(new
            {
                Fecha = fechaActual,
                Ingresos = ingresos,
                Egresos = egresos,
                SaldoProyectado = saldoProyectado,
                Detalles = delDia.Select(d => new
                {
                    d.Id,
                    d.NroCheque,
                    d.Banco,
                    d.Monto,
                    Tipo = d.EsIngreso ? "COBRO (+)" : "DEBITO (-)",
                    d.Detalle
                }).ToList()
            });

            fechaActual = fechaActual.AddDays(1);
        }

        return Ok(new
        {
            CuentaNombre = cuenta.Nombre,
            Banco = cuenta.Banco,
            NroCuenta = cuenta.NroCuenta,
            SaldoActual = saldoActual,
            TotalIngresos = chequesACobrar.Sum(c => c.Monto),
            TotalEgresos = chequesADebitar.Sum(c => c.Monto),
            SaldoProyectadoFinal = saldoActual + chequesACobrar.Sum(c => c.Monto) - chequesADebitar.Sum(c => c.Monto),
            ProyeccionDiaria = proyDiaria,
            DetalleChequesPendientes = itemsProyeccion
        });
    }

    // ═══════════════════════════════════════════
    // CHEQUES
    // ═══════════════════════════════════════════
    [HttpGet("cheques")]
    public async Task<IActionResult> GetCheques(
        [FromQuery] TipoCheque? tipo,
        [FromQuery] EstadoCheque? estado,
        [FromQuery] DateTime? venceDesde,
        [FromQuery] DateTime? venceHasta)
    {
        var q = db.Cheques.AsQueryable();
        if (tipo.HasValue) q = q.Where(c => c.Tipo == tipo.Value);
        if (estado.HasValue) q = q.Where(c => c.Estado == estado.Value);
        if (venceDesde.HasValue) q = q.Where(c => c.FechaPago >= venceDesde.Value.ToUtc());
        if (venceHasta.HasValue) q = q.Where(c => c.FechaPago <= venceHasta.Value.ToUtc());

        var lista = await q.OrderBy(c => c.FechaPago)
            .Select(c => new
            {
                c.Id,
                c.Tipo,
                c.Estado,
                c.NroCheque,
                c.Banco,
                c.Monto,
                c.FechaEmision,
                c.FechaPago,
                c.Librador,
                c.EsRechazado,
                ClienteNombre   = c.Cliente   != null ? c.Cliente.RazonSocial   : null,
                ProveedorNombre = c.Proveedor != null ? c.Proveedor.RazonSocial : null
            })
            .ToListAsync();
        return Ok(lista);
    }

    [HttpPost("cheques")]
    public async Task<IActionResult> RegistrarCheque([FromBody] Cheque cheque)
    {
        cheque.FechaAlta = DateTime.UtcNow;
        db.Cheques.Add(cheque);
        await db.SaveChangesAsync();
        return Ok(new { cheque.Id, cheque.NroCheque, cheque.Monto });
    }

    [HttpPut("cheques/{id}/estado")]
    public async Task<IActionResult> ActualizarEstadoCheque(int id, [FromBody] ActualizarEstadoChequeRequest req)
    {
        var ch = await db.Cheques.FindAsync(id);
        if (ch is null) return NotFound();
        ch.Estado = req.NuevoEstado;
        if (req.NuevoEstado == EstadoCheque.Rechazado)
        {
            ch.EsRechazado = true;
            ch.FechaRechazo = DateTime.UtcNow;
        }
        if (req.NuevoEstado == EstadoCheque.Depositado && req.IdCuentaDestino.HasValue)
        {
            ch.IdCuenta = req.IdCuentaDestino.Value;
            var cuenta = await db.CuentasTesoreria.FindAsync(req.IdCuentaDestino.Value);
            if (cuenta != null) cuenta.SaldoActual += ch.Monto;
        }
        await db.SaveChangesAsync();
        return Ok(new { ch.Id, ch.Estado });
    }

    // ═══════════════════════════════════════════
    // GASTOS POR CAJA
    // ═══════════════════════════════════════════
    [HttpGet("gastos")]
    public async Task<IActionResult> GetGastos([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var q = db.GastosCaja.AsQueryable();
        if (desde.HasValue) q = q.Where(g => g.Fecha >= desde.Value.ToUtc());
        if (hasta.HasValue) q = q.Where(g => g.Fecha <= hasta.Value.ToUtc().AddDays(1));
        return Ok(await q.OrderByDescending(g => g.Fecha).ToListAsync());
    }

    [HttpPost("gastos")]
    public async Task<IActionResult> RegistrarGasto([FromBody] GastoCaja gasto)
    {
        gasto.Fecha = DateTime.UtcNow;
        db.GastosCaja.Add(gasto);
        await db.SaveChangesAsync();
        return Ok(new { gasto.Id, gasto.Monto, gasto.Descripcion });
    }
}

public class ActualizarEstadoChequeRequest
{
    public EstadoCheque NuevoEstado { get; set; }
    public int? IdCuentaDestino { get; set; }
    public string? Observaciones { get; set; }
}

public class ConciliacionItemDto
{
    public int IdMovimiento { get; set; }
    public bool Conciliado { get; set; }
}

public class RegistrarDepositoDto
{
    public int IdCuentaDestino { get; set; }
    public int? IdCuentaOrigen { get; set; }
    public string NroComprobante { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public decimal MontoEfectivo { get; set; }
    public List<int> ChequesIds { get; set; } = new();
    public string? Observaciones { get; set; }
    public int IdUsuario { get; set; }
}
