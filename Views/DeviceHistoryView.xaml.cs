using Lubrisense.ViewModels;

namespace Lubrisense.Views;

public partial class DeviceHistoryView : ContentPage
{
    public DeviceHistoryView(DeviceHistoryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}