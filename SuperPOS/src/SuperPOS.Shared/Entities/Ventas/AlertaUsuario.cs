namespace SuperPOS.Shared.Entities.Ventas;

public class AlertaUsuario
{
    public int Id { get; set; }
    public int IdAlerta { get; set; }
    public int IdUsuario { get; set; }

    public Alerta? Alerta { get; set; }
    public Usuario? Usuario { get; set; }
}
