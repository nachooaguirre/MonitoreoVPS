using System.Threading.Tasks;

namespace SuperPOS.API.Services
{
    public interface IContabilidadService
    {
        Task<byte[]> GenerarLibroIvaVentasCbte(int mes, int anio);
        Task<byte[]> GenerarLibroIvaVentasAlicuotas(int mes, int anio);
        Task<byte[]> GenerarLibroIvaComprasCbte(int mes, int anio);
        Task<byte[]> GenerarLibroIvaComprasAlicuotas(int mes, int anio);
        Task<byte[]> GenerarPercepcionesIvaVentas(int mes, int anio);
        Task<byte[]> GenerarPercepcionesIIBBCompras(int mes, int anio);
        Task<string> GenerarResumenVentasCsv(int mes, int anio);
        Task<string> GenerarResumenComprasCsv(int mes, int anio);
    }
}
