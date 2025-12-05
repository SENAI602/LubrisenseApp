using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lubrisense.Helpers;
using Lubrisense.Models;
using Lubrisense.Services;
using Shiny;
using Shiny.BluetoothLE;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace Lubrisense.ViewModels
{
    public partial class DeviceViewModel : ObservableObject
    {
        [ObservableProperty] private ObservableCollection<DeviceGroup> _lstDevices;
        [ObservableProperty] private ShowDevice _selectedDevice;
        [ObservableProperty] private bool _isScanning;
        [ObservableProperty] private bool _isBusyConnecting;

        private readonly BluetoothService _bluetoothService;

        public DeviceViewModel(BluetoothService bluetoothService)
        {
            _bluetoothService = bluetoothService;
            _bluetoothService.DevicesUpdated += _bluetoothService_DevicesUpdated;
            LstDevices = new ObservableCollection<DeviceGroup>();

            AddDevice("Dispositivos novos", new List<ShowDevice>());
            AddDevice("Dispositivos conhecidos", new List<ShowDevice>());
        }

        // --- MÉTODO CHAMADO AO ENTRAR NA TELA ---
        public async void OnAppearing()
        {
            IsBusyConnecting = false;
            SelectedDevice = null;
            UpdateKnownDevices();
            ClearNewDevices();

            // Inicia o scan automaticamente (se já não estiver escaneando)
            if (!IsScanning)
            {
                await StartScanAsync();
            }
        }

        public void ClearNewDevices()
        {
            var grupoNovos = LstDevices.FirstOrDefault(g => g.Nome == "Dispositivos novos");
            grupoNovos?.Clear();
        }

        public void UpdateKnownDevices()
        {
            var grupoConhecidos = LstDevices.FirstOrDefault(g => g.Nome == "Dispositivos conhecidos");
            if (grupoConhecidos != null)
            {
                grupoConhecidos.Clear();
                var listConhecidos = SavedDeviceStorage.Load();
                if (listConhecidos == null) return;

                foreach (var device in listConhecidos)
                {
                    grupoConhecidos.Add(new ShowDevice { Uuid = device.Uuid, Equipamento = device.Equipamento, IsOnline = false });
                }
            }
        }

        public void AddDevice(string name, List<ShowDevice> devices) { LstDevices.Add(new DeviceGroup(name, devices)); }

        // Método auxiliar para iniciar o scan com segurança
        private async Task StartScanAsync()
        {
            try
            {
                var access = await _bluetoothService.RequestAccess();
                if (access != Shiny.AccessState.Available)
                {
                    await Shell.Current.DisplayAlert("Permissão", "Bluetooth necessário.", "OK");
                    return;
                }

                ResetDeviceStatus();
                IsScanning = true;
                _bluetoothService.StartScan();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erro", $"Scan falhou: {ex.Message}", "OK");
                IsScanning = false;
            }
        }

        private void _bluetoothService_DevicesUpdated() { var online = _bluetoothService.DiscoveredDevices; UpdateStatusDevice(online); }

        private void UpdateStatusDevice(IReadOnlyList<IPeripheral> onlinePeripherals)
        {
            var grupoConhecidos = LstDevices.FirstOrDefault(g => g.Nome == "Dispositivos conhecidos");
            var grupoNovos = LstDevices.FirstOrDefault(g => g.Nome == "Dispositivos novos");
            if (grupoConhecidos == null || grupoNovos == null) return;

            foreach (var peripheral in onlinePeripherals)
            {
                var dispositivoConhecido = grupoConhecidos.FirstOrDefault(d => string.Equals(d.Uuid, peripheral.Uuid, StringComparison.OrdinalIgnoreCase));
                if (dispositivoConhecido != null) { dispositivoConhecido.IsOnline = true; }
                else
                {
                    if (!grupoNovos.Any(d => d.Uuid == peripheral.Uuid))
                    {
                        grupoNovos.Add(new ShowDevice { Uuid = peripheral.Uuid, Equipamento = peripheral.Name ?? "Novo Disp.", IsOnline = true });
                    }
                }
            }
        }

        private void ResetDeviceStatus()
        {
            var grupoConhecidos = LstDevices.FirstOrDefault(g => g.Nome == "Dispositivos conhecidos");
            if (grupoConhecidos != null) { foreach (var device in grupoConhecidos) device.IsOnline = false; }
            var grupoNovos = LstDevices.FirstOrDefault(g => g.Nome == "Dispositivos novos");
            grupoNovos?.Clear();
        }

        [RelayCommand]
        public async Task DeviceTapped(ShowDevice selectedDevice)
        {
            if (selectedDevice == null || IsBusyConnecting) return;

            if (!selectedDevice.IsOnline)
            {
                var savedCheck = SavedDeviceStorage.GetByUuid(selectedDevice.Uuid);
                if (savedCheck == null)
                {
                    await Shell.Current.DisplayAlert("Indisponível", "Dispositivo novo fora de alcance. Faça um scan primeiro.", "OK");
                    return;
                }
            }

            try
            {
                IsBusyConnecting = true;
                _bluetoothService.StopScan();
                IsScanning = false;

                bool conectado = await _bluetoothService.ConnectToDeviceAsync(selectedDevice.Uuid);

                if (!conectado)
                {
                    await Shell.Current.DisplayAlert("Erro", "Não foi possível conectar ao dispositivo.", "OK");
                    return;
                }

                var comandoGet = "{\"comando\": \"get_config\"}";
                string? jsonRecebido = await _bluetoothService.RequestDataAsync(comandoGet, 15000);

                if (!string.IsNullOrEmpty(jsonRecebido))
                {
                    try
                    {
                        using JsonDocument doc = JsonDocument.Parse(jsonRecebido);
                        JsonElement root = doc.RootElement;

                        if (root.TryGetProperty("payload", out JsonElement payload)) root = payload;

                        var deviceParaSalvar = SavedDeviceStorage.GetByUuid(selectedDevice.Uuid) ?? new SavedDevice(selectedDevice.Uuid);

                        if (root.TryGetProperty("Tag", out JsonElement tag)) deviceParaSalvar.Tag = tag.GetString() ?? "";
                        if (root.TryGetProperty("Equipamento", out JsonElement equip)) deviceParaSalvar.Equipamento = equip.GetString() ?? "";
                        if (root.TryGetProperty("Setor", out JsonElement setor)) deviceParaSalvar.Setor = setor.GetString() ?? "";
                        if (root.TryGetProperty("Lubrificante", out JsonElement lub)) deviceParaSalvar.Lubrificante = lub.GetString() ?? "";

                        if (root.TryGetProperty("Volume", out JsonElement vol)) deviceParaSalvar.Volume = vol.GetInt32();
                        if (root.TryGetProperty("Intervalo", out JsonElement inter)) deviceParaSalvar.Intervalo = inter.GetInt32();
                        if (root.TryGetProperty("TipoIntervalo", out JsonElement tInter)) deviceParaSalvar.TipoIntervalo = tInter.GetInt32();
                        if (root.TryGetProperty("Frequencia", out JsonElement freq)) deviceParaSalvar.Frequencia = freq.GetInt32();
                        if (root.TryGetProperty("TipoFrequencia", out JsonElement tFreq)) deviceParaSalvar.TipoFrequencia = tFreq.GetInt32();
                        if (root.TryGetProperty("TipoConfig", out JsonElement tConf)) deviceParaSalvar.TipoConfig = tConf.GetInt32();

                        deviceParaSalvar.UltimaConexao = DateTime.Now;
                        SavedDeviceStorage.AddOrUpdate(deviceParaSalvar);
                        Console.WriteLine("[APP] Dados sincronizados.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[APP] Erro JSON: {ex.Message}");
                    }
                }
                else
                {
                    bool entrar = await Shell.Current.DisplayAlert("Sem Resposta", "Conectado, mas sem dados do dispositivo. Entrar com dados locais?", "Sim", "Não");
                    if (!entrar) { _bluetoothService.Disconnect(); return; }
                }

                await Shell.Current.GoToAsync($"DeviceMenuView?DeviceUuid={selectedDevice.Uuid}");
                SelectedDevice = null;
            }
            finally
            {
                IsBusyConnecting = false;
            }
        }

        [RelayCommand]
        public async Task DeviceDelete(ShowDevice device)
        {
            if (device == null) return;
            var confirm = await Shell.Current.DisplayAlert("Remover", $"Esquecer {device.Equipamento}?", "Sim", "Não");
            if (confirm)
            {
                SavedDeviceStorage.Remove(device.Uuid);
                UpdateKnownDevices();
            }
        }
    }
}