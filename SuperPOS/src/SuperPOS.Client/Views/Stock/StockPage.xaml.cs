using System.Windows;
using System.Windows.Controls;

namespace SuperPOS.Client.Views.Stock;

public partial class StockPage : Page
{
    public StockPage() { InitializeComponent(); Loaded += OnLoaded; }
    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var (_, items) = await App.Api.GetArticulos(pageSize: 200);
        DgStock.ItemsSource = items;
    }
}
