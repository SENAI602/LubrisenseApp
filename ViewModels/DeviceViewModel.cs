using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lubrisense.Models;
using Lubrisense.Resources.Fonts;
using Lubrisense.Services;
using Lubrisense.Views;
using Shiny.BluetoothLE;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;

namespace Lubrisense.ViewModels
{
    public partial class DeviceViewModel : ObservableObject
    {
        [ObservableProperty]
        public ObservableCollection<DeviceGroup> _lstDevices;

        [ObservableProperty]
        private ShowDevice _selectedDevice;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotScanning))]
        private bool _isScanning;

        public bool IsNotScanning => !IsScanning;

        private readonly BluetoothService _bluetoothService;

        public DeviceViewModel(BluetoothService bluetoothService)
        {
            _bluetoothService = bluetoothService;
            _bluetoothService.DevicesUpdated += _bluetoothService_DevicesUpdated;
            LstDevices = new ObservableCollection<DeviceGroup>();

            AddDevice("Dispositivos novos", new List<ShowDevice>());
            AddDevice("Dispositivos conhecidos", new List<ShowDevice>());
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
                    await Shell.Current.GoToAsync($"{nameof(DeviceDetailView)}?DeviceId={selectedDevice.Uuid}");
                }
                    

                SelectedDevice = null;
            }
        }

        //private void UpdateStatusDevice(IEnumerable<string> idsDispositivosOnline)
        //{
        //    var grupoConhecidos = LstDevices.FirstOrDefault(g => g.Nome == "Dispositivos conhecidos");
        //    var grupoNovos = LstDevices.FirstOrDefault(g => g.Nome == "Dispositivos novos");

        //    if (grupoConhecidos == null || grupoNovos == null)
        //    {
        //        return;
        //    }
        //    // Limpa o grupo de novos dispositivos
        //    grupoNovos.Clear();

        //    // Marca todos os dispositivos conhecidos como offline inicialmente
        //    foreach (var device in grupoConhecidos)
        //    {
        //        device.IsOnline = false;
        //    }

        //    foreach (var idOnline in idsDispositivosOnline)
        //    {
        //        var dispositivoConhecido = grupoConhecidos.FirstOrDefault(d => string.Equals(d.Id, idOnline, StringComparison.OrdinalIgnoreCase));
        //        if (dispositivoConhecido != null) dispositivoConhecido.IsOnline = true;
        //        else
        //        {
        //            grupoNovos.Add(new ShowDevice
        //            {
        //                Id = idOnline,
        //                Equipamento = "Novo Dispositivo",
        //                IsOnline = true
        //            });
        //        }
        //    }
        //}

        
    }
}
