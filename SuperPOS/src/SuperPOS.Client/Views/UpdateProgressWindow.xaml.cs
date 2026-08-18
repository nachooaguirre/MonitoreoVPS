using Wpf.Ui.Controls;

namespace SuperPOS.Client.Views;

public partial class UpdateProgressWindow : FluentWindow
{
    public UpdateProgressWindow()
    {
        InitializeComponent();
    }

    public void SetEstado(string texto) => TxtEstado.Text = texto;

    public void SetProgreso(int porcentaje)
    {
        BarProgreso.Value = porcentaje;
        TxtPorcentaje.Text = $"{porcentaje}%";
    }
}
