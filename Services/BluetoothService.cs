using ReactiveUI;
using Shiny;
using Shiny.BluetoothLE;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Diagnostics;

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

        // --- Scan ---
        public void StartScan()
        {
            _scanSubscription?.Dispose();
            DiscoveredDevices.Clear();

            _scanSubscription = _bleManager
                .Scan()
                .Where(scanResult =>
                    !string.IsNullOrEmpty(scanResult.Peripheral.Name) &&
                    scanResult.Peripheral.Name.Contains("Lubri", StringComparison.OrdinalIgnoreCase))
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

        // --- Conexão ---
        public async Task<bool> ConnectToDeviceAsync(string deviceUuid)
        {
            try
            {
                Disconnect(); // Limpa conexão anterior
                StopScan();

                var deviceToConnect = DiscoveredDevices.FirstOrDefault(p => p.Uuid.Equals(deviceUuid, StringComparison.OrdinalIgnoreCase));

                if (deviceToConnect == null)
                {
                    var known = _bleManager.GetKnownPeripheral(deviceUuid);
                    if (known != null) deviceToConnect = known;
                }

                if (deviceToConnect == null)
                {
                    Debug.WriteLine($"[BluetoothService] Dispositivo {deviceUuid} não encontrado.");
                    return false;
                }

                if (deviceToConnect.Status == ConnectionState.Connected)
                    deviceToConnect.CancelConnection();

                await deviceToConnect.ConnectAsync(timeout: TimeSpan.FromSeconds(10));
                _connectedDevice = deviceToConnect;

                _writeReadNotifyCharacteristic = await deviceToConnect.GetCharacteristic(
                    App.Instance.LubricenseServiceUuid,
                    App.Instance.LubricenseCharacteristicUuid
                ).Take(1).ToTask();

                if (_writeReadNotifyCharacteristic == null)
                {
                    Debug.WriteLine("[BluetoothService] Característica não encontrada.");
                    Disconnect();
                    return false;
                }

                SetupNotifications();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BluetoothService] Falha ao conectar: {ex.Message}");
                Disconnect();
                return false;
            }
        }

        // ==================================================================
        // MUDANÇA CRÍTICA AQUI: Envio Fatiado (Chunking)
        // ==================================================================
        public async Task<bool> SendDataAsync(string message)
        {
            if (_connectedDevice?.IsConnected() != true || _writeReadNotifyCharacteristic == null) return false;

            try
            {
                var bytes = Encoding.UTF8.GetBytes(message);

                // O BLE padrão suporta ~20 bytes de payload útil por pacote.
                // Enviar mais que isso sem negociação de MTU causa erro ou perda de dados.
                int chunkSize = 20;
                int offset = 0;

                while (offset < bytes.Length)
                {
                    // Calcula o tamanho do pedaço atual
                    int size = Math.Min(chunkSize, bytes.Length - offset);
                    var chunk = new byte[size];
                    Array.Copy(bytes, offset, chunk, 0, size);

                    // Envia o pedaço
                    await _connectedDevice.WriteCharacteristicAsync(_writeReadNotifyCharacteristic, chunk);

                    // Avança para o próximo
                    offset += size;

                    // Pequeno delay para dar tempo do ESP32 processar e limpar o buffer do Android
                    await Task.Delay(50);
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BluetoothService] Erro ao enviar dados: {ex.Message}");
                return false;
            }
        }

        // --- Recepção (Request/Response) ---
        public async Task<string?> RequestDataAsync(string commandJson, int timeoutMs = 15000)
        {
            if (_connectedDevice?.IsConnected() != true) return null;

            var tcs = new TaskCompletionSource<string?>();
            var receivedBuffer = new StringBuilder();
            int openBraces = 0;
            bool foundStart = false;

            Action<string> tempHandler = null;
            tempHandler = (data) =>
            {
                Debug.WriteLine($"[BLE RAW] Recebido: {data}");
                receivedBuffer.Append(data);
                string currentStr = receivedBuffer.ToString();

                openBraces = 0;
                foundStart = false;
                foreach (char c in currentStr)
                {
                    if (c == '{') { openBraces++; foundStart = true; }
                    else if (c == '}') { openBraces--; }
                }

                if (foundStart && openBraces == 0)
                {
                    Debug.WriteLine("[BLE DEBUG] JSON Completo Detectado!");
                    DataReceived -= tempHandler;
                    tcs.TrySetResult(currentStr);
                }
            };

            DataReceived += tempHandler;

            try
            {
                Debug.WriteLine($"[BLE DEBUG] Enviando comando: {commandJson}");
                bool sent = await SendDataAsync(commandJson); // Usa o método fatiado agora!
                if (!sent)
                {
                    DataReceived -= tempHandler;
                    return null;
                }

                var timeoutTask = Task.Delay(timeoutMs);
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

                if (completedTask == tcs.Task) return await tcs.Task;
                else
                {
                    DataReceived -= tempHandler;
                    Debug.WriteLine($"[BLE TIMEOUT] Buffer atual: {receivedBuffer}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                DataReceived -= tempHandler;
                Debug.WriteLine($"[BLE ERROR] {ex.Message}");
                return null;
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
                    if (result.Data != null)
                    {
                        var data = Encoding.UTF8.GetString(result.Data);
                        DataReceived?.Invoke(data);
                    }
                });
        }

        public void Disconnect()
        {
            if (_connectedDevice != null)
            {
                try { _notificationSubscription?.Dispose(); _connectedDevice.CancelConnection(); } catch { }
                _connectedDevice = null;
            }
        }
    }
}