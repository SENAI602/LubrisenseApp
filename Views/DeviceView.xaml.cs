using Lubrisense.Resources.Fonts;
using Lubrisense.ViewModels;

namespace Lubrisense.Views;

public partial class DeviceView : ContentPage
{
    private readonly DeviceViewModel viewModel;

    public DeviceView(DeviceViewModel _viewModel)
    {
        InitializeComponent();
        viewModel = _viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Garante que os eventos e a lista sejam atualizados
        viewModel.OnAppearing();
        viewModel.PropertyChanged += OnViewModelPropertyChanged;

        UpdateToolbarIcon();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        viewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DeviceViewModel.IsScanning))
        {
            UpdateToolbarIcon();
        }
    }

    private void UpdateToolbarIcon()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (viewModel.IsScanning)
            {
                BluetoothToolbar.IconImageSource = new FontImageSource
                {
                    Glyph = FluentUI.bluetooth_searching_24_regular,
                    FontFamily = "FluentRegular"
                };
            }
            else
            {
                BluetoothToolbar.IconImageSource = new FontImageSource
                {
                    Glyph = FluentUI.bluetooth_24_regular,
                    FontFamily = "FluentRegular"
                };
            }
        });
    }
}