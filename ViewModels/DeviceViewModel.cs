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
        [ObservableProperty]
        public ObservableCollection<DeviceGroup> _lstDevices;

        [ObservableProperty]
        private ShowDevice _selectedDevice;

        [ObservableProperty]
        private bool _isScanning;

        private readonly BluetoothService _bluetoothService;

        public DeviceViewModel(BluetoothService bluetoothService)
        {
            _bluetoothService = bluetoothService;
            _bluetoothService.DevicesUpdated += _bluetoothService_DevicesUpdated;
            LstDevices = new ObservableCollection<DeviceGroup>();

            AddDevice("Dispositivos novos", new List<ShowDevice>());
            AddDevice("Dispositivos conhecidos", new List<ShowDevice>());
        }

        public void UpdateKnownDevices()
        {
            var grupoConhecidos = LstDevices.FirstOrDefault(g => g.Nome == "Dispositivos conhecidos");
            if (grupoConhecidos != null)
            {
                grupoConhecidos.Clear();
                var listConhecidos = SavedDeviceStorage.Load();
                if(listConhecidos == null || listConhecidos.Count == 0) return;

                foreach (var device in listConhecidos)
                {
                    grupoConhecidos.Add(new ShowDevice
                    {
                        Uuid = device.Uuid,
                        Equipamento = device.Equipamento,
                        IsOnline = false
                    });
                }
            }
        }

        public void AddDevice(string name, List<ShowDevice> devices)
        {
            LstDevices.Add(new DeviceGroup(name, devices));
        }

        [RelayCommand]
        private async Task ToggleScanAsync()
        {
            if (IsScanning)
            {
                _bluetoothService.StopScan();
                IsScanning = false;
                return;
            }

            try
            {
                var access = await _bluetoothService.RequestAccess();
                switch (access)
                {
                    case AccessState.Disabled:
                        // Bluetooth está desligado
                        await Shell.Current.DisplayAlert(
                            "Bluetooth Desligado",
                            "Para continuar, por favor, ative o Bluetooth do seu dispositivo.",
                            "OK"
                        );
                        break;

                    case AccessState.Denied:
                        // Permissão foi negada
                        bool openSettings = await Shell.Current.DisplayAlert(
                            "Permissão Necessária",
                            "O acesso ao Bluetooth foi negado. Para usar esta funcionalidade, você precisa conceder a permissão nas configurações do aplicativo.",
                            "Abrir Configurações",
                            "Cancelar"
                        );

                        if (openSettings)
                        {
                            AppInfo.Current.ShowSettingsUI();
                        }
                        break;

                    case AccessState.NotSupported:
                        // Dispositivo não tem suporte
                        await Shell.Current.DisplayAlert(
                           "Não Suportado",
                           "Infelizmente, seu dispositivo não possui suporte a Bluetooth LE.",
                           "OK"
                       );
                        break;
                }

                if (access != Shiny.AccessState.Available)
                {
                    await Shell.Current.DisplayAlert("Permissão Negada", "A permissão de Bluetooth é necessária para localizar os dispositivos Lubricense.", "OK");
                    return;
                }
                ResetDeviceStatus();
                IsScanning = true;
                _bluetoothService.StartScan();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erro", $"Não foi possível iniciar o scan: {ex.Message}", "OK");
                IsScanning = false;
            }
        }

        private void _bluetoothService_DevicesUpdated()
        {
            var onlinePeripherals = _bluetoothService.DiscoveredDevices;
            UpdateStatusDevice(onlinePeripherals);
        }

        private void UpdateStatusDevice(IReadOnlyList<IPeripheral> onlinePeripherals)
        {
            var grupoConhecidos = LstDevices.FirstOrDefault(g => g.Nome == "Dispositivos conhecidos");
            var grupoNovos = LstDevices.FirstOrDefault(g => g.Nome == "Dispositivos novos");

            if (grupoConhecidos == null || grupoNovos == null) return;

            foreach (var peripheral in onlinePeripherals)
            {
                var dispositivoConhecido = grupoConhecidos.FirstOrDefault(d => string.Equals(d.Uuid, peripheral.Uuid, StringComparison.OrdinalIgnoreCase));

                if (dispositivoConhecido != null)
                {
                    dispositivoConhecido.IsOnline = true;
                }
                else
                {
                    grupoNovos.Add(new ShowDevice
                    {
                        Uuid = peripheral.Uuid,
                        Equipamento = "Novo Dispositivo Lubricense",
                        IsOnline = true
                    });
                }
            }
        }

        private void ResetDeviceStatus()
        {
            var grupoConhecidos = LstDevices.FirstOrDefault(g => g.Nome == "Dispositivos conhecidos");
            if (grupoConhecidos != null)
            {
                foreach (var device in grupoConhecidos)
                {
                    device.IsOnline = false;
                }
            }

            var grupoNovos = LstDevices.FirstOrDefault(g => g.Nome == "Dispositivos novos");
            grupoNovos?.Clear();
        }

        [RelayCommand]
        public async Task DeviceTapped(ShowDevice selectedDevice)
        {
            if (selectedDevice != null)
            {
                if (!selectedDevice.IsOnline)
                {
                    await Shell.Current.DisplayAlert("", "Não detectamos este dispositivo nas proximidades", "OK");
                }
                else
                {
                    var confirm = await Shell.Current.DisplayAlert("Conectar", $"Deseja conectar ao dispositivo {selectedDevice.MacAddressDisplay}?", "Sim", "Não");
                    if(!confirm) return;

                    _bluetoothService.StopScan();
                    IsScanning = false;

                    var device = SavedDeviceStorage.GetByUuid(selectedDevice.Uuid);
                    if(device == null)
                    {
                        await Shell.Current.GoToAsync($"DeviceDetailView?DeviceUuid={selectedDevice.Uuid}");
                        return;
                    }

                    var connected = await _bluetoothService.ConnectToDeviceAsync(selectedDevice.Uuid);
                    if (connected)
                    {
                        var json = JsonSerializer.Serialize(device);
                        await _bluetoothService.SendDataAsync(json);
                    }


                }
                SelectedDevice = null;
            }
        }       
    }
}