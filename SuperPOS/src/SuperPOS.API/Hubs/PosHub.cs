using Microsoft.AspNetCore.SignalR;

namespace SuperPOS.API.Hubs;

public class PosHub : Hub
{
    public async Task NotificarVenta(int idCaja, decimal total)
    {
        await Clients.All.SendAsync("VentaRealizada", idCaja, total, DateTime.UtcNow);
    }

    public async Task NotificarStockBajo(int idArticulo, string descripcion, decimal stockActual)
    {
        await Clients.All.SendAsync("StockBajo", idArticulo, descripcion, stockActual);
    }
}
