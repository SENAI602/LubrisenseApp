using Lubrisense.ViewModels;

namespace Lubrisense.Views;

public partial class DeviceDetailView : ContentPage
{
	public DeviceDetailView(DeviceDetailViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
    }
}