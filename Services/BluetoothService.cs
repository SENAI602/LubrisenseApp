using ReactiveUI;
using Shiny;
using Shiny.BluetoothLE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;

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

        // --- SCAN ---
        public void StartScan()
        {
            _scanSubscription?.Dispose();
            DiscoveredDevices.Clear();

            _scanSubscription = _bleManager
                .Scan()
                .Where(scanResult =>
                    (scanResult.Peripheral.Name != null && scanResult.Peripheral.Name.Contains("Lubri", StringComparison.OrdinalIgnoreCase))
                    ||
                    (scanResult.AdvertisementData.ServiceUuids != null && scanResult.AdvertisementData.ServiceUuids.Contains(App.Instance.LubricenseServiceUuid))
                )
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
                    if (newDeviceFound) DevicesUpdated?.Invoke();
                });
        }

        public void StopScan() => _scanSubscription?.Dispose();

        // --- CONEXÃO INTELIGENTE ---
        public async Task<bool> ConnectToDeviceAsync(string deviceUuid)
        {
            StopScan();

            // 1. VERIFICAÇÃO INTELIGENTE (A CORREÇÃO)
            // Se já estamos conectados no dispositivo certo e a característica está pronta, NÃO DESCONECTA!
            if (_connectedDevice != null &&
                _connectedDevice.Status == ConnectionState.Connected &&
                _connectedDevice.Uuid.Equals(deviceUuid, StringComparison.OrdinalIgnoreCase) &&
                _writeReadNotifyCharacteristic != null)
            {
                Console.WriteLine("[BluetoothService] Já conectado e pronto. Reutilizando conexão.");
                return true;
            }

            // Se chegou aqui, é porque não estava conectado ou era outro dispositivo.
            // Agora sim, inicia o processo de conexão do zero.

            // Tenta até 3 vezes
            for (int tentativa = 1; tentativa <= 3; tentativa++)
            {
                try
                {
                    Console.WriteLine($"[BLE] Tentativa {tentativa}...");

                    // Limpeza de conexão anterior (se houver)
                    if (_connectedDevice != null)
                    {
                        try { _connectedDevice.CancelConnection(); } catch { }
                        _connectedDevice = null;
                        _writeReadNotifyCharacteristic = null;
                        await Task.Delay(300);
                    }

                    // Busca dispositivo
                    var deviceToConnect = DiscoveredDevices.FirstOrDefault(p => p.Uuid.Equals(deviceUuid, StringComparison.OrdinalIgnoreCase));

                    // Scan de emergência se não estiver na lista
                    if (deviceToConnect == null)
                    {
                        var scanResult = await _bleManager.Scan()
                            .Where(x => x.Peripheral.Uuid.Equals(deviceUuid, StringComparison.OrdinalIgnoreCase))
                            .Take(1)
                            .Timeout(TimeSpan.FromSeconds(4))
                            .ToTask();
                        deviceToConnect = scanResult?.Peripheral;
                    }

                    if (deviceToConnect == null) throw new Exception("Dispositivo não encontrado.");

                    // Conecta
                    await deviceToConnect.ConnectAsync(new ConnectionConfig { AutoConnect = false });
                    _connectedDevice = deviceToConnect;

                    // Busca Característica
                    // Retry interno para buscar serviços (Android às vezes demora para descobrir)
                    for (int i = 0; i < 3; i++)
                    {
                        try
                        {
                            _writeReadNotifyCharacteristic = await deviceToConnect.GetCharacteristic(
                                App.Instance.LubricenseServiceUuid,
                                App.Instance.LubricenseCharacteristicUuid
                            ).Take(1).ToTask();
                            if (_writeReadNotifyCharacteristic != null) break;
                        }
                        catch { await Task.Delay(500); }
                    }

                    if (_writeReadNotifyCharacteristic == null) throw new Exception("Característica não encontrada.");

                    SetupNotifications();
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[BLE] Erro tentativa {tentativa}: {ex.Message}");
                    await Task.Delay(1000);
                }
            }
            return false;
        }

        public async Task<bool> SendDataAsync(string message)
        {
            if (_connectedDevice?.Status != ConnectionState.Connected || _writeReadNotifyCharacteristic == null) return false;

            try
            {
                // Pequeno delay para garantir que o buffer do BLE não está cheio
                await Task.Delay(50);
                var bytes = Encoding.UTF8.GetBytes(message);
                await _connectedDevice.WriteCharacteristicAsync(_writeReadNotifyCharacteristic, bytes); // WithResponse = true por padrão no Shiny
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BluetoothService] Erro envio: {ex.Message}");
                return false;
            }
        }

        private void SetupNotifications()
        {
            if (_connectedDevice == null || _writeReadNotifyCharacteristic == null) return;

            try
            {
                _notificationSubscription?.Dispose();
                if (_writeReadNotifyCharacteristic.CanNotify())
                {
                    _notificationSubscription = _connectedDevice
                        .NotifyCharacteristic(_writeReadNotifyCharacteristic)
                        .Subscribe(result =>
                        {
                            if (result.Data != null)
                            {
                                var data = Encoding.UTF8.GetString(result.Data);
                                DataReceived?.Invoke(data);
                            }
                        });
                }
            }
            catch { }
        }

        public void Disconnect()
        {
            try
            {
                _notificationSubscription?.Dispose();
                _connectedDevice?.CancelConnection();
            }
            catch { }
            _connectedDevice = null;
            _writeReadNotifyCharacteristic = null;
        }
    }
}