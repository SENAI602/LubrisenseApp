using Lubrisense.ViewModels;

namespace Lubrisense.Views;

public partial class DeviceConfigView : ContentPage
{
    // O construtor recebe o ViewModel pronto (Injeção de Dependência)
    public DeviceConfigView(DeviceConfigViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}