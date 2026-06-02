using System.Windows.Controls;

namespace SuperPOS.Client.Views.Shared;

public partial class ProximamentePage : Page
{
    public ProximamentePage(string modulo = "")
    {
        InitializeComponent();
        TxtModulo.Text = modulo;
    }
}
