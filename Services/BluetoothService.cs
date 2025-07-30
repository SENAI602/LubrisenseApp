using Lubrisense.Helpers;
using ReactiveUI;
using Shiny;
using Shiny.BluetoothLE;
using System.Reactive.Linq;
using System.Text;

namespace Lubrisense.Services
{
    public class BluetoothService : IShinyStartupTask
    {
        private readonly IBleManager _bleManager;
        private IDisposable? _scanSubscription;

        public List<IPeripheral> DiscoveredDevices { get; private set; } = new();
        public event Action DevicesUpdated;

        public BluetoothService(IBleManager bleManager)
        {
            _bleManager = bleManager;
        }

        public async Task<AccessState> RequestAccess()
        {
            return await _bleManager.RequestAccess();
        }

        public void Start() { }

        public void StartScan()
        {
            _scanSubscription?.Dispose();
            DiscoveredDevices.Clear();

            _scanSubscription = _bleManager
                .Scan()
                .Where(scanResult => scanResult.Peripheral.Name?.Contains("LUBRICENSE_Device") == true)
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

        public async Task ConnectToDeviceAsync(IPeripheral device)
        {
            await device.ConnectAsync();
        }
    }
}