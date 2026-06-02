using System;

namespace SuperPOS.Shared.Entities.Ventas;

public class MonedaPlan
{
    public int Id { get; set; }
    public int PlanNro { get; set; }
    
    public int IdMedioPago { get; set; }
    public MedioPago? MedioPago { get; set; }

    public string Detalle { get; set; } = "";
    public decimal Recargo { get; set; }
    public int Acumulador { get; set; }
}
