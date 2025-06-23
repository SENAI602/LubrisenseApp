using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using System.Text;

namespace Lubrisense.Services
{
    public class BluetoothService
    {
        private readonly IBluetoothLE _ble;
        private readonly IAdapter _adapter;
        private IDevice _device;
        private ICharacteristic _characteristic;

        // Seu UUID de serviço e característica
        private readonly Guid _serviceUuid = Guid.Parse("97ec4585-9e94-41b6-8902-1a2db274dfc9");
        private readonly Guid _characteristicUuid = Guid.Parse("c04c4646-d355-41ab-9097-89c2c6b9932b");

        public List<IDevice> DiscoveredDevices { get; private set; } = new();
        public event Action DevicesUpdated;
        public event Action<string> DataReceived;

        public BluetoothService()
        {
            _ble = CrossBluetoothLE.Current;
            _adapter = CrossBluetoothLE.Current.Adapter;
            _adapter.DeviceDiscovered += OnFilteredDeviceDiscovered;
        }

        public async Task StartFilteredScanAsync()
        {
            DiscoveredDevices.Clear();
            await _adapter.StartScanningForDevicesAsync();
        }

        private async void OnFilteredDeviceDiscovered(object sender, DeviceEventArgs e)
        {
            if (e.Device.Name?.Contains("LUBRICENSE_Device") == true)
            {
                try
                {
                    var services = await e.Device.GetServicesAsync();
                    if (services.Any(s => s.Id == _serviceUuid))
                    {
                        if (!DiscoveredDevices.Any(d => d.Id == e.Device.Id))
                        {
                            DiscoveredDevices.Add(e.Device);
                            DevicesUpdated?.Invoke();
                        }
                    }
                }
                catch
                {
                    // Alguns dispositivos BLE só expõem serviços após conexão completa
                }
            }
        }

        public async Task ConnectToDeviceAsync(IDevice device)
        {
            _device = device;
            await _adapter.ConnectToDeviceAsync(_device);

            var service = await _device.GetServiceAsync(_serviceUuid);
            _characteristic = await service.GetCharacteristicAsync(_characteristicUuid);

            if (_characteristic.CanUpdate)
            {
                _characteristic.ValueUpdated += OnValueUpdated;
                await _characteristic.StartUpdatesAsync();
            }
        }

        private void OnValueUpdated(object sender, CharacteristicUpdatedEventArgs e)
        {
            var value = Encoding.UTF8.GetString(e.Characteristic.Value);
            DataReceived?.Invoke(value);
        }

        public async Task SendAsync(string message)
        {
            if (_characteristic != null && _characteristic.CanWrite)
            {
                var bytes = Encoding.UTF8.GetBytes(message);
                await _characteristic.WriteAsync(bytes);
            }
        }
    }
}
