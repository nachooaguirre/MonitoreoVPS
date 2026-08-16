using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using SuperPOS.API.Data;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.API.Services
{
    public class ContabilidadService : IContabilidadService
    {
        private readonly SuperPOSDbContext _db;

        public ContabilidadService(SuperPOSDbContext db)
        {
            _db = db;
        }

        #region Helpers Formateo AFIP

        private string FormatearImporte(decimal valor, int longitud = 15)
        {
            decimal absValor = Math.Abs(valor);
            long centavos = (long)Math.Round(absValor * 100);
            return centavos.ToString().PadLeft(longitud, '0');
        }

        private string FormatearDocumento(string? documento, int longitud = 20)
        {
            if (string.IsNullOrEmpty(documento))
                return new string('0', longitud);
            
            string limpia = new string(documento.Where(char.IsDigit).ToArray());
            return limpia.PadLeft(longitud, '0');
        }

        private string FormatearTexto(string? texto, int longitud = 30)
        {
            string normalizado = NormalizarTexto(texto);
            if (normalizado.Length > longitud)
                return normalizado.Substring(0, longitud);
            return normalizado.PadRight(longitud, ' ');
        }

        private string NormalizarTexto(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            string normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (char c in normalized)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    if (char.IsAscii(c))
                    {
                        sb.Append(char.ToUpper(c));
                    }
                    else
                    {
                        sb.Append(' ');
                    }
                }
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        private string FormatearCuitConGuiones(string? cuit)
        {
            if (string.IsNullOrEmpty(cuit))
                return "00-00000000-0";
            
            string limpia = new string(cuit.Where(char.IsDigit).ToArray());
            if (limpia.Length != 11)
                return limpia.PadRight(13, ' ');

            return $"{limpia.Substring(0, 2)}-{limpia.Substring(2, 8)}-{limpia.Substring(10, 1)}";
        }

        private string FormatearImporteCiti(decimal valor, int digitosEnteros, int digitosDecimales)
        {
            decimal absValor = Math.Abs(valor);
            string mascaraEnteros = new string('0', digitosEnteros);
            string mascaraDecimales = new string('0', digitosDecimales);
            string formateado = absValor.ToString($"{mascaraEnteros}.{mascaraDecimales}", CultureInfo.InvariantCulture);
            return formateado.Replace('.', ',');
        }

        private string GetLibroIvaTipoComprobante(int? codigoAfip, char letra)
        {
            if (codigoAfip == 1 || codigoAfip == 81 || (codigoAfip == null && letra == 'A')) return "081";
            if (codigoAfip == 6 || codigoAfip == 82 || (codigoAfip == null && letra == 'B')) return "082";
            if (codigoAfip == 11 || codigoAfip == 83 || (codigoAfip == null && letra == 'C')) return "083";
            if (codigoAfip == 3 || codigoAfip == 110) return "110";
            if (codigoAfip == 8 || codigoAfip == 111) return "111";
            if (codigoAfip == 13 || codigoAfip == 112) return "112";
            return codigoAfip?.ToString().PadLeft(3, '0') ?? "083";
        }

        private string GetTipoDocumento(Cliente? cliente)
        {
            if (cliente == null || cliente.CondicionIva == 5)
            {
                if (!string.IsNullOrEmpty(cliente?.Cuit))
                {
                    string clean = new string(cliente.Cuit.Where(char.IsDigit).ToArray());
                    if (clean.Length == 11) return "80";
                    if (clean.Length == 8 || clean.Length == 7) return "96";
                }
                return "99";
            }
            if (cliente.CondicionIva == 1 || cliente.CondicionIva == 4)
            {
                return "80";
            }
            return "99";
        }

        #endregion

        #region Libro IVA Ventas

        public async Task<byte[]> GenerarLibroIvaVentasCbte(int mes, int anio)
        {
            var fechaInicio = new DateTime(anio, mes, 1, 0, 0, 0, DateTimeKind.Utc);
            var fechaFin = fechaInicio.AddMonths(1).AddSeconds(-1);

            var comprobantes = await _db.Comprobantes
                .Include(c => c.Cliente)
                .Include(c => c.TipoComprobante)
                .Include(c => c.Detalles)
                .Where(c => c.Fecha >= fechaInicio && c.Fecha <= fechaFin && c.Estado == EstadoComprobante.Emitido)
                .OrderBy(c => c.Fecha)
                .ThenBy(c => c.Numero)
                .ToListAsync();

            var sb = new StringBuilder();

            // Procesar Facturas A y B de forma individual
            var individuales = comprobantes.Where(c => c.Letra == 'A' || c.Letra == 'B');
            foreach (var comp in individuales)
            {
                string fechaStr = comp.Fecha.ToString("yyyyMMdd");
                string tipoCbte = GetLibroIvaTipoComprobante(comp.TipoComprobante?.CodigoAfip, comp.Letra);
                string ptoVenta = comp.PuntoVenta.ToString().PadLeft(5, '0');
                string nroDesde = comp.Numero.ToString().PadLeft(20, '0');
                string nroHasta = comp.Numero.ToString().PadLeft(20, '0');
                string tipoDoc = GetTipoDocumento(comp.Cliente);
                string nroDoc = FormatearDocumento(comp.Cliente?.Cuit);
                string razonSocial = FormatearTexto(comp.Cliente?.RazonSocial ?? "CONSUMIDOR FINAL", 30);
                
                string total = FormatearImporte(comp.Total);
                string noGravado = FormatearImporte(0);
                string exento = FormatearImporte(comp.TotalIva0);
                string percIva = FormatearImporte(0);
                string percNac = FormatearImporte(0);

                // Calcular percepción de IIBB (la diferencia)
                decimal gravado = comp.SubTotal;
                decimal totalIva = comp.TotalIva21 + comp.TotalIva105;
                decimal percIibbCalc = comp.Total - (gravado + totalIva + comp.TotalIva0);
                if (percIibbCalc < 0) percIibbCalc = 0;
                string percIibb = FormatearImporte(percIibbCalc);
                
                string percMun = FormatearImporte(0);
                string impInternos = FormatearImporte(0);
                string moneda = "PES";
                string tipoCambio = "0001000000"; // 1.000000

                // Cantidad de alícuotas
                int cantAlic = comp.Detalles.Select(d => d.AlicuotaIva).Distinct().Count();
                if (cantAlic == 0) cantAlic = 1;
                string cantAlicStr = cantAlic.ToString();

                string codOp = "A"; // Gravado por defecto
                string ivaComputable = FormatearImporte(0); // Credito fiscal no aplica en ventas
                string otrosTributos = "00000000"; // Otros campos

                string line = $"{fechaStr}{tipoCbte}{ptoVenta}{nroDesde}{nroHasta}{tipoDoc}{nroDoc}{razonSocial}{total}{noGravado}{exento}{percIva}{percNac}{percIibb}{percMun}{impInternos}{moneda}{tipoCambio}{cantAlicStr}{codOp}{ivaComputable}{otrosTributos}";
                sb.Append(line).Append("\r\n");
            }

            // Agrupar Facturas C (Consumidor Final) por Fecha y Punto de Venta para hacer "VENTA GLOBAL DIARIA"
            var facturasC = comprobantes.Where(c => c.Letra == 'C');
            var gruposC = facturasC.GroupBy(c => new { Fecha = c.Fecha.Date, c.PuntoVenta });

            foreach (var grupo in gruposC)
            {
                string fechaStr = grupo.Key.Fecha.ToString("yyyyMMdd");
                string tipoCbte = "083"; // Tique Factura C
                string ptoVenta = grupo.Key.PuntoVenta.ToString().PadLeft(5, '0');
                
                long minNro = grupo.Min(c => c.Numero);
                long maxNro = grupo.Max(c => c.Numero);
                string nroDesde = minNro.ToString().PadLeft(20, '0');
                string nroHasta = maxNro.ToString().PadLeft(20, '0');
                
                string tipoDoc = "99"; // Sin Identificar
                string nroDoc = FormatearDocumento(null);
                string razonSocial = FormatearTexto("VENTA GLOBAL DIARIA", 30);
                
                decimal totalMonto = grupo.Sum(c => c.Total);
                decimal exentoMonto = grupo.Sum(c => c.TotalIva0);
                
                string total = FormatearImporte(totalMonto);
                string noGravado = FormatearImporte(0);
                string exento = FormatearImporte(exentoMonto);
                string percIva = FormatearImporte(0);
                string percNac = FormatearImporte(0);
                string percIibb = FormatearImporte(0);
                string percMun = FormatearImporte(0);
                string impInternos = FormatearImporte(0);
                string moneda = "PES";
                string tipoCambio = "0001000000";

                // Cantidad de alícuotas en el grupo
                int cantAlic = grupo.SelectMany(c => c.Detalles).Select(d => d.AlicuotaIva).Distinct().Count();
                if (cantAlic == 0) cantAlic = 1;
                string cantAlicStr = cantAlic.ToString();

                string codOp = "A";
                string ivaComputable = FormatearImporte(0);
                string otrosTributos = "00000000";

                string line = $"{fechaStr}{tipoCbte}{ptoVenta}{nroDesde}{nroHasta}{tipoDoc}{nroDoc}{razonSocial}{total}{noGravado}{exento}{percIva}{percNac}{percIibb}{percMun}{impInternos}{moneda}{tipoCambio}{cantAlicStr}{codOp}{ivaComputable}{otrosTributos}";
                sb.Append(line).Append("\r\n");
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public async Task<byte[]> GenerarLibroIvaVentasAlicuotas(int mes, int anio)
        {
            var fechaInicio = new DateTime(anio, mes, 1, 0, 0, 0, DateTimeKind.Utc);
            var fechaFin = fechaInicio.AddMonths(1).AddSeconds(-1);

            var comprobantes = await _db.Comprobantes
                .Include(c => c.TipoComprobante)
                .Include(c => c.Detalles)
                .Where(c => c.Fecha >= fechaInicio && c.Fecha <= fechaFin && c.Estado == EstadoComprobante.Emitido)
                .ToListAsync();

            var sb = new StringBuilder();

            // Procesar Facturas A y B
            var individuales = comprobantes.Where(c => c.Letra == 'A' || c.Letra == 'B');
            foreach (var comp in individuales)
            {
                string tipoCbte = GetLibroIvaTipoComprobante(comp.TipoComprobante?.CodigoAfip, comp.Letra);
                string ptoVenta = comp.PuntoVenta.ToString().PadLeft(5, '0');
                string nroCbte = comp.Numero.ToString().PadLeft(20, '0');

                // Agrupar detalles por alícuota
                var detallesPorAlicuota = comp.Detalles
                    .GroupBy(d => d.AlicuotaIva);

                foreach (var grupo in detallesPorAlicuota)
                {
                    decimal net = grupo.Sum(d => d.SubTotal - d.MontoIva);
                    decimal iva = grupo.Sum(d => d.MontoIva);
                    string netoStr = FormatearImporte(net);
                    string alicuotaCode = GetAlicuotaCode(grupo.Key);
                    string ivaStr = FormatearImporte(iva);

                    string line = $"{tipoCbte}{ptoVenta}{nroCbte}{netoStr}{alicuotaCode}{ivaStr}";
                    sb.Append(line).Append("\r\n");
                }
            }

            // Procesar Facturas C (agrupadas de forma diaria para venta global)
            var facturasC = comprobantes.Where(c => c.Letra == 'C');
            var gruposC = facturasC
                .SelectMany(c => c.Detalles.Select(d => new { c.Fecha, c.PuntoVenta, Detail = d }))
                .GroupBy(x => new { Fecha = x.Fecha.Date, x.PuntoVenta, Alicuota = x.Detail.AlicuotaIva });

            foreach (var grupo in gruposC)
            {
                // Para obtener el número que pusimos en el CBTE (usamos el número máximo del grupo de ese día y PV)
                var ticketsDelDia = facturasC.Where(c => c.Fecha.Date == grupo.Key.Fecha && c.PuntoVenta == grupo.Key.PuntoVenta).ToList();
                if (!ticketsDelDia.Any()) continue;

                long maxNro = ticketsDelDia.Max(c => c.Numero);

                string tipoCbte = "083"; // Tique Factura C
                string ptoVenta = grupo.Key.PuntoVenta.ToString().PadLeft(5, '0');
                string nroCbte = maxNro.ToString().PadLeft(20, '0');

                decimal net = grupo.Sum(x => x.Detail.SubTotal - x.Detail.MontoIva);
                decimal iva = grupo.Sum(x => x.Detail.MontoIva);

                string netoStr = FormatearImporte(net);
                string alicuotaCode = GetAlicuotaCode(grupo.Key.Alicuota);
                string ivaStr = FormatearImporte(iva);

                string line = $"{tipoCbte}{ptoVenta}{nroCbte}{netoStr}{alicuotaCode}{ivaStr}";
                sb.Append(line).Append("\r\n");
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private string GetAlicuotaCode(decimal alicuota)
        {
            if (alicuota == 0) return "0003";
            if (alicuota == 10.5m) return "0004";
            if (alicuota == 21m) return "0005";
            if (alicuota == 27m) return "0006";
            return "0005"; // Default 21%
        }

        #endregion

        #region Libro IVA Compras

        public async Task<byte[]> GenerarLibroIvaComprasCbte(int mes, int anio)
        {
            var fechaInicio = new DateTime(anio, mes, 1, 0, 0, 0, DateTimeKind.Utc);
            var fechaFin = fechaInicio.AddMonths(1).AddSeconds(-1);

            var compras = await _db.Compras
                .Include(c => c.Proveedor)
                .Include(c => c.TipoComprobante)
                .Include(c => c.Detalles)
                .Where(c => c.Fecha >= fechaInicio && c.Fecha <= fechaFin && c.Estado == EstadoCompra.Recibida)
                .OrderBy(c => c.Fecha)
                .ToListAsync();

            var sb = new StringBuilder();

            foreach (var compra in compras)
            {
                string fechaStr = compra.Fecha.ToString("yyyyMMdd");
                
                int codAfipVal = compra.TipoComprobante?.CodigoAfip ?? 1; // Default Factura A
                string tipoCbte = codAfipVal.ToString().PadLeft(3, '0');
                
                string ptoVenta = compra.PuntoVentaProveedor.ToString().PadLeft(5, '0');
                
                // Extraer sólo los números de la factura del proveedor
                string cleanNroStr = new string((compra.NumeroFactura ?? "").Where(char.IsDigit).ToArray());
                if (string.IsNullOrEmpty(cleanNroStr)) cleanNroStr = "1";
                string nro = cleanNroStr.PadLeft(20, '0');
                
                string despacho = FormatearTexto(compra.DespachoImportacion, 16);
                string tipoDoc = "80"; // CUIT por defecto para proveedores
                string nroDoc = FormatearDocumento(compra.Proveedor?.Cuit);
                string razonSocial = FormatearTexto(compra.Proveedor?.RazonSocial ?? "PROVEEDOR", 30);
                
                string total = FormatearImporte(compra.Total);
                string noGravado = FormatearImporte(compra.ImporteNoGravado);
                string exento = FormatearImporte(compra.ImporteExento);
                string percIva = FormatearImporte(compra.PercepcionIva);
                string percNac = FormatearImporte(compra.PercepcionNacional);
                string percIIBB = FormatearImporte(compra.PercepcionIIBB);
                string percMun = FormatearImporte(compra.PercepcionMunicipal);
                string impInternos = FormatearImporte(compra.ImpuestosInternos);
                
                string moneda = "PES";
                string tipoCambio = "0001000000";

                int cantAlic = compra.Detalles.Select(d => d.AlicuotaIva).Distinct().Count();
                if (cantAlic == 0) cantAlic = 1;
                string cantAlicStr = cantAlic.ToString();

                string codOp = "0"; // Normal
                string ivaComputable = FormatearImporte(compra.TotalIva);
                string otrosTributos = FormatearImporte(0); // Otros tributos

                string cuitCorredor = FormatearDocumento(compra.CuitCorredor, 11);
                string nombreCorredor = FormatearTexto(compra.NombreCorredor, 30);
                string ivaComision = FormatearImporte(compra.IvaComision);

                string line = $"{fechaStr}{tipoCbte}{ptoVenta}{nro}{despacho}{tipoDoc}{nroDoc}{razonSocial}{total}{noGravado}{exento}{percIva}{percNac}{percIIBB}{percMun}{impInternos}{moneda}{tipoCambio}{cantAlicStr}{codOp}{ivaComputable}{otrosTributos}{cuitCorredor}{nombreCorredor}{ivaComision}";
                sb.Append(line).Append("\r\n");
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public async Task<byte[]> GenerarLibroIvaComprasAlicuotas(int mes, int anio)
        {
            var fechaInicio = new DateTime(anio, mes, 1, 0, 0, 0, DateTimeKind.Utc);
            var fechaFin = fechaInicio.AddMonths(1).AddSeconds(-1);

            var compras = await _db.Compras
                .Include(c => c.Proveedor)
                .Include(c => c.TipoComprobante)
                .Include(c => c.Detalles)
                .Where(c => c.Fecha >= fechaInicio && c.Fecha <= fechaFin && c.Estado == EstadoCompra.Recibida)
                .ToListAsync();

            var sb = new StringBuilder();

            foreach (var compra in compras)
            {
                int codAfipVal = compra.TipoComprobante?.CodigoAfip ?? 1;
                string tipoCbte = codAfipVal.ToString().PadLeft(3, '0');
                string ptoVenta = compra.PuntoVentaProveedor.ToString().PadLeft(5, '0');
                
                string cleanNroStr = new string((compra.NumeroFactura ?? "").Where(char.IsDigit).ToArray());
                if (string.IsNullOrEmpty(cleanNroStr)) cleanNroStr = "1";
                string nro = cleanNroStr.PadLeft(20, '0');

                string tipoDoc = "80";
                string nroDoc = FormatearDocumento(compra.Proveedor?.Cuit);

                var gruposAlicuota = compra.Detalles.GroupBy(d => d.AlicuotaIva);
                foreach (var grupo in gruposAlicuota)
                {
                    decimal net = grupo.Sum(d => d.SubTotal); // En compra, el subtotal suele ser neto
                    decimal iva = net * (grupo.Key / 100m);

                    string netoStr = FormatearImporte(net);
                    string alicuotaCode = GetAlicuotaCode(grupo.Key);
                    string ivaStr = FormatearImporte(iva);

                    string line = $"{tipoCbte}{ptoVenta}{nro}{tipoDoc}{nroDoc}{netoStr}{alicuotaCode}{ivaStr}";
                    sb.Append(line).Append("\r\n");
                }
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        #endregion

        #region Percepciones Ventas (IIBB ARBA) y Compras (IIBB ARBA Suffering)

        public async Task<byte[]> GenerarPercepcionesIvaVentas(int mes, int anio)
        {
            var fechaInicio = new DateTime(anio, mes, 1, 0, 0, 0, DateTimeKind.Utc);
            var fechaFin = fechaInicio.AddMonths(1).AddSeconds(-1);

            // Reportamos percepciones de IIBB hechas a clientes RI
            var comprobantes = await _db.Comprobantes
                .Include(c => c.Cliente)
                .Where(c => c.Fecha >= fechaInicio && c.Fecha <= fechaFin && c.Estado == EstadoComprobante.Emitido)
                .ToListAsync();

            var sb = new StringBuilder();

            foreach (var comp in comprobantes)
            {
                decimal gravado = comp.SubTotal;
                decimal totalIva = comp.TotalIva21 + comp.TotalIva105;
                decimal percIibbCalc = comp.Total - (gravado + totalIva + comp.TotalIva0);
                if (percIibbCalc <= 0.05m) continue; // Si es cero o despreciable, no declarar

                string cuit = FormatearCuitConGuiones(comp.Cliente?.Cuit);
                string fechaStr = comp.Fecha.ToString("dd/MM/yyyy");
                
                string tipo = comp.Letra == 'A' ? "FA" : "FA"; // NC si es nota de credito
                if (comp.TipoComprobante?.CodigoAfip == 3 || comp.TipoComprobante?.CodigoAfip == 8 || comp.TipoComprobante?.CodigoAfip == 110 || comp.TipoComprobante?.CodigoAfip == 111)
                {
                    tipo = "NC";
                }
                
                string ptoVenta = comp.PuntoVenta.ToString().PadLeft(5, '0');
                string nroStr = comp.Numero.ToString().PadLeft(8, '0');
                string cpte = $"{tipo}{ptoVenta}{nroStr}";

                string netoFormateado = FormatearImporteCiti(gravado, 11, 2);
                
                // Calcular tasa real de percepcion (usualmente 6% o 1.75% o similar)
                decimal tasa = 0m;
                if (gravado > 0)
                {
                    tasa = Math.Round((percIibbCalc / gravado) * 100, 2);
                }
                string tasaStr = FormatearImporteCiti(tasa, 2, 2);
                string montoStr = FormatearImporteCiti(percIibbCalc, 10, 2);
                string fechaVto = comp.Fecha.ToString("dd/MM/yyyy");
                string regimenCode = "a"; // Codigo regimen general ARBA

                string line = $"{cuit}{fechaStr}{cpte}{netoFormateado}{tasaStr}{montoStr}{fechaVto}{regimenCode}";
                sb.Append(line).Append("\r\n");
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public async Task<byte[]> GenerarPercepcionesIIBBCompras(int mes, int anio)
        {
            var fechaInicio = new DateTime(anio, mes, 1, 0, 0, 0, DateTimeKind.Utc);
            var fechaFin = fechaInicio.AddMonths(1).AddSeconds(-1);

            // Reportamos las percepciones de IIBB que nos cobraron los proveedores
            var compras = await _db.Compras
                .Include(c => c.Proveedor)
                .Where(c => c.Fecha >= fechaInicio && c.Fecha <= fechaFin && c.Estado == EstadoCompra.Recibida && c.PercepcionIIBB > 0)
                .ToListAsync();

            var sb = new StringBuilder();

            foreach (var compra in compras)
            {
                string cuit = FormatearCuitConGuiones(compra.Proveedor?.Cuit);
                string fechaStr = compra.Fecha.ToString("dd/MM/yyyy");
                
                string ptoVenta = compra.PuntoVentaProveedor.ToString().PadLeft(5, '0');
                string cleanNroStr = new string((compra.NumeroFactura ?? "").Where(char.IsDigit).ToArray());
                if (string.IsNullOrEmpty(cleanNroStr)) cleanNroStr = "1";
                string nro = cleanNroStr.PadLeft(8, '0');
                string cpte = $"{ptoVenta}{nro}";

                string baseImponible = FormatearImporteCiti(compra.SubTotal, 11, 2);
                
                decimal tasa = 0m;
                if (compra.SubTotal > 0)
                {
                    tasa = Math.Round((compra.PercepcionIIBB / compra.SubTotal) * 100, 2);
                }
                string tasaStr = FormatearImporteCiti(tasa, 2, 2);
                string montoStr = FormatearImporteCiti(compra.PercepcionIIBB, 10, 2);
                
                string regimenCode = "A"; // Codigo regimen sufrido

                string line = $"{cuit}{fechaStr}{cpte}{baseImponible}{tasaStr}{montoStr}{regimenCode}";
                sb.Append(line).Append("\r\n");
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        #endregion

        #region Resúmenes en CSV

        public async Task<string> GenerarResumenVentasCsv(int mes, int anio)
        {
            var fechaInicio = new DateTime(anio, mes, 1, 0, 0, 0, DateTimeKind.Utc);
            var fechaFin = fechaInicio.AddMonths(1).AddSeconds(-1);

            var comprobantes = await _db.Comprobantes
                .Include(c => c.Cliente)
                .Include(c => c.TipoComprobante)
                .Where(c => c.Fecha >= fechaInicio && c.Fecha <= fechaFin && c.Estado == EstadoComprobante.Emitido)
                .OrderBy(c => c.Fecha)
                .ThenBy(c => c.Numero)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Fecha;TipoCpte;PV;Número;TipoDoc;NroDoc;RazonSocial;Total;NetoGravado;Iva21;Iva105;Exento;PercIIBB");

            foreach (var comp in comprobantes)
            {
                string fechaStr = comp.Fecha.ToString("yyyyMMdd");
                string tipoCpte = GetLibroIvaTipoComprobante(comp.TipoComprobante?.CodigoAfip, comp.Letra);
                string pv = comp.PuntoVenta.ToString().PadLeft(5, '0');
                string nro = comp.Numero.ToString().PadLeft(20, '0');
                string tipoDoc = GetTipoDocumento(comp.Cliente);
                string nroDoc = FormatearDocumento(comp.Cliente?.Cuit);
                string razonSocial = (comp.Cliente?.RazonSocial ?? "CONSUMIDOR FINAL").Replace(';', ' ').Trim();

                // Formato con punto decimal para CSV contable
                string total = comp.Total.ToString("F2", CultureInfo.InvariantCulture);
                string neto = comp.SubTotal.ToString("F2", CultureInfo.InvariantCulture);
                string iva21 = comp.TotalIva21.ToString("F2", CultureInfo.InvariantCulture);
                string iva105 = comp.TotalIva105.ToString("F2", CultureInfo.InvariantCulture);
                string exento = comp.TotalIva0.ToString("F2", CultureInfo.InvariantCulture);

                decimal percIibbCalc = comp.Total - (comp.SubTotal + comp.TotalIva21 + comp.TotalIva105 + comp.TotalIva0);
                if (percIibbCalc < 0) percIibbCalc = 0;
                string percIibb = percIibbCalc.ToString("F2", CultureInfo.InvariantCulture);

                sb.AppendLine($"{fechaStr};{tipoCpte};{pv};{nro};{tipoDoc};{nroDoc};{razonSocial};{total};{neto};{iva21};{iva105};{exento};{percIibb}");
            }

            return sb.ToString();
        }

        public async Task<string> GenerarResumenComprasCsv(int mes, int anio)
        {
            var fechaInicio = new DateTime(anio, mes, 1, 0, 0, 0, DateTimeKind.Utc);
            var fechaFin = fechaInicio.AddMonths(1).AddSeconds(-1);

            var compras = await _db.Compras
                .Include(c => c.Proveedor)
                .Include(c => c.TipoComprobante)
                .Where(c => c.Fecha >= fechaInicio && c.Fecha <= fechaFin && c.Estado == EstadoCompra.Recibida)
                .OrderBy(c => c.Fecha)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Fecha;TipoCpte;PV;Número;Despacho;TipoDoc;NroDoc;RazonSocial;Total;NoGravado;Exento;PercIVA;PercNac;PercIB;PercMunicip;ImpInt;Moneda;Cotizacion;CantAlic;CodOp;IVA Computable;Otros;CUIT Corredor;Nombre Corredor;IVA Comisión");

            foreach (var compra in compras)
            {
                string fechaStr = compra.Fecha.ToString("yyyyMMdd");
                
                int codAfipVal = compra.TipoComprobante?.CodigoAfip ?? 1;
                string tipoCpte = codAfipVal.ToString().PadLeft(3, '0');
                
                string pv = compra.PuntoVentaProveedor.ToString().PadLeft(5, '0');
                
                string cleanNroStr = new string((compra.NumeroFactura ?? "").Where(char.IsDigit).ToArray());
                if (string.IsNullOrEmpty(cleanNroStr)) cleanNroStr = "1";
                string nro = cleanNroStr.PadLeft(20, '0');

                string despacho = (compra.DespachoImportacion ?? "").Trim();
                string tipoDoc = "80";
                string nroDoc = FormatearDocumento(compra.Proveedor?.Cuit);
                string razonSocial = (compra.Proveedor?.RazonSocial ?? "PROVEEDOR").Replace(';', ' ').Trim();

                string total = compra.Total.ToString("F2", CultureInfo.InvariantCulture);
                string noGravado = compra.ImporteNoGravado.ToString("F2", CultureInfo.InvariantCulture);
                string exento = compra.ImporteExento.ToString("F2", CultureInfo.InvariantCulture);
                string percIva = compra.PercepcionIva.ToString("F2", CultureInfo.InvariantCulture);
                string percNac = compra.PercepcionNacional.ToString("F2", CultureInfo.InvariantCulture);
                string percIB = compra.PercepcionIIBB.ToString("F2", CultureInfo.InvariantCulture);
                string percMun = compra.PercepcionMunicipal.ToString("F2", CultureInfo.InvariantCulture);
                string impInt = compra.ImpuestosInternos.ToString("F2", CultureInfo.InvariantCulture);
                
                string moneda = "PES";
                string cotizacion = "1.00";
                
                int cantAlic = compra.Detalles.Select(d => d.AlicuotaIva).Distinct().Count();
                if (cantAlic == 0) cantAlic = 1;
                string cantAlicStr = cantAlic.ToString();
                
                string codOp = "0";
                string ivaComputable = compra.TotalIva.ToString("F2", CultureInfo.InvariantCulture);
                string otros = "0.00";
                
                string cuitCorredor = FormatearDocumento(compra.CuitCorredor, 11);
                string nombreCorredor = (compra.NombreCorredor ?? "").Trim();
                string ivaComision = compra.IvaComision.ToString("F2", CultureInfo.InvariantCulture);

                sb.AppendLine($"{fechaStr};{tipoCpte};{pv};{nro};{despacho};{tipoDoc};{nroDoc};{razonSocial};{total};{noGravado};{exento};{percIva};{percNac};{percIB};{percMun};{impInt};{moneda};{cotizacion};{cantAlicStr};{codOp};{ivaComputable};{otros};{cuitCorredor};{nombreCorredor};{ivaComision}");
            }

            return sb.ToString();
        }

        #endregion
    }
}
