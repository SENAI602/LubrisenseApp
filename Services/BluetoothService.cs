using ReactiveUI;
using Shiny;
using Shiny.BluetoothLE;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;

namespace Lubrisense.Services
{
    public class BluetoothService : IShinyStartupTask
    {
        private readonly IBleManager _bleManager;
        private IDisposable? _scanSubscription;
        private IPeripheral? _connectedDevice;
        private BleCharacteristicInfo? _writeReadNotifyCharacteristic;
        private IDisposable? _notificationSubscription;

        public List<IPeripheral> DiscoveredDevices { get; private set; } = new();
        public event Action DevicesUpdated;
        public event Action<string> DataReceived;

        public BluetoothService(IBleManager bleManager)
        {
            _bleManager = bleManager;
        }

        public async Task<AccessState> RequestAccess() => await _bleManager.RequestAccess();

        public void Start() { }

        public void StartScan()
        {
            _scanSubscription?.Dispose();
            DiscoveredDevices.Clear();

            _scanSubscription = _bleManager
                .Scan()
                .Where(scanResult => scanResult.Peripheral.Name?.Contains("LUBRICENSE_Device") == true)
                .Where(scanResult => scanResult.AdvertisementData.ServiceUuids?.Contains(App.Instance.LubricenseServiceUuid, StringComparer.OrdinalIgnoreCase) == true)
                .Buffer(TimeSpan.FromSeconds(1))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(peripherals =>
                {
                    bool newDeviceFound = false;
                    foreach (var p in peripherals)
                    {
                        if (!DiscoveredDevices.Any(d => d.Uuid == p.Peripheral.Uuid))
                        {
                            DiscoveredDevices.Add(p.Peripheral);
                            newDeviceFound = true;
                        }
                    }

                    if (newDeviceFound)
                    {
                        DevicesUpdated?.Invoke();
                    }
                });
        }

        public void StopScan()
        {
            _scanSubscription?.Dispose();
        }

        public async Task<bool> ConnectToDeviceAsync(string deviceUuid)
        {
            try
            {
                StopScan();
                var deviceToConnect = DiscoveredDevices.FirstOrDefault(p => p.Uuid.Equals(deviceUuid, StringComparison.OrdinalIgnoreCase));

                if (deviceToConnect == null)
                {
                    Console.WriteLine($"[BluetoothService] Dispositivo com UUID {deviceUuid} não encontrado na lista de descobertos.");
                    return false;
                }


                await deviceToConnect.ConnectAsync();
                _connectedDevice = deviceToConnect;
                _writeReadNotifyCharacteristic = await deviceToConnect.GetCharacteristic(
                    App.Instance.LubricenseServiceUuid,
                    App.Instance.LubricenseCharacteristicUuid
                )
                .Take(1)
                .ToTask();

                if (_writeReadNotifyCharacteristic == null)
                {
                    Disconnect();
                    return false;
                }

                SetupNotifications();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BluetoothService] Falha ao conectar: {ex.Message}");
                Disconnect();
                return false;
            }
        }

        public async Task<bool> SendDataAsync(string message)
        {
            if (_connectedDevice?.IsConnected() != true || _writeReadNotifyCharacteristic == null) return false;

            try
            {
                var bytes = Encoding.UTF8.GetBytes(message);
                await _connectedDevice.WriteCharacteristicAsync(_writeReadNotifyCharacteristic, bytes);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BluetoothService] Erro ao enviar dados: {ex.Message}");
                return false;
            }
        }

        private void SetupNotifications()
        {
            if (_connectedDevice == null || _writeReadNotifyCharacteristic == null || !_writeReadNotifyCharacteristic.CanNotify()) return;

            _notificationSubscription?.Dispose();
            _notificationSubscription = _connectedDevice
                .NotifyCharacteristic(_writeReadNotifyCharacteristic)
                .Subscribe(result =>
                {
                    var data = Encoding.UTF8.GetString(result.Data);
                    DataReceived?.Invoke(data);
                });
        }

        public void Disconnect()
        {
            if (_connectedDevice != null)
            {
                _notificationSubscription?.Dispose();
                _connectedDevice.CancelConnection();
                _connectedDevice = null;
            }
        }
    }
}