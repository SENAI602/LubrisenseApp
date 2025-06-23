using Lubrisense.Services;
using Plugin.BLE.Abstractions.Contracts;

namespace Lubrisense
{
    public partial class MainPage : ContentPage
    {
        private readonly BluetoothService _bluetoothService;

        public MainPage()
        {
            InitializeComponent();
            _bluetoothService = new BluetoothService();
            _bluetoothService.DevicesUpdated += OnDevicesUpdated;
            _bluetoothService.DataReceived += OnDataReceived;
        }

        private async void OnScanClicked(object sender, EventArgs e)
        {
            DevicesList.ItemsSource = null;
            await _bluetoothService.StartFilteredScanAsync();
        }

        private void OnDevicesUpdated()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                DevicesList.ItemsSource = _bluetoothService.DiscoveredDevices;
            });
        }

        private async void OnDeviceSelected(object sender, SelectionChangedEventArgs e)
        {
            var device = e.CurrentSelection.FirstOrDefault() as IDevice;
            if (device != null)
            {
                await _bluetoothService.ConnectToDeviceAsync(device);
                await DisplayAlert("Conectado", device.Name, "OK");
            }
        }

        private async void OnSendClicked(object sender, EventArgs e)
        {
            await _bluetoothService.SendAsync("LED_ON");
        }

        private void OnDataReceived(string data)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                DisplayAlert("Recebido", data, "OK");
            });
        }
    }
}
