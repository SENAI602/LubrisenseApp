using Lubrisense.ViewModels;

namespace Lubrisense.Views;

[QueryProperty(nameof(DeviceUuid), "DeviceUuid")]
public partial class DeviceConfigView : ContentPage
{
    public string DeviceUuid
    {
        get => _deviceUuid;
        set
        {
            _deviceUuid = value;
            if (BindingContext is DeviceConfigViewModel vm)
                vm.SetupDevice(value);
        }
    }
    private string _deviceUuid;

    public DeviceConfigView(DeviceConfigViewModel viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;
    }
}