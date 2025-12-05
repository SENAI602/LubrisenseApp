using Lubrisense.ViewModels;

namespace Lubrisense.Views;

public partial class DeviceMenuView : ContentPage
{
    public DeviceMenuView(DeviceMenuViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}