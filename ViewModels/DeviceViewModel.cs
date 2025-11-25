using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lubrisense.Helpers;
using Lubrisense.Models;
using Lubrisense.Services;
using Lubrisense.Views;
using Shiny;
using Shiny.BluetoothLE;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Lubrisense.ViewModels
{
    public partial class DeviceViewModel : ObservableObject
    {
        // Lista principal de dispositivos (agrupada)
        public ObservableCollection<DeviceGroup> LstDevices { get; } = new();

        [ObservableProperty]
        private ShowDevice _selectedDevice;

        [ObservableProperty]
        private bool _isScanning;

        private readonly BluetoothService _bluetoothService;

        public DeviceViewModel(BluetoothService bluetoothService)
        {
            _bluetoothService = bluetoothService;
            _bluetoothService.DevicesUpdated += _bluetoothService_DevicesUpdated;

            // Inicializa os grupos vazios para evitar erro de null
            LstDevices.Add(new DeviceGroup("Dispositivos novos", new List<ShowDevice>()));
            LstDevices.Add(new DeviceGroup("Dispositivos conhecidos", new List<ShowDevice>()));
        }

        // Chamado pela View quando ela aparece (OnAppearing)
        public void OnAppearing()
        {
            UpdateKnownDevices();
            ResetDeviceStatus(); // Reseta status de online/offline
        }

        // Carrega dispositivos salvos do banco de dados local
        public void UpdateKnownDevices()
        {
            var grupoConhecidos = LstDevices.FirstOrDefault(g => g.Nome == "Dispositivos conhecidos");
            if (grupoConhecidos != null)
            {
                grupoConhecidos.Clear();
                var listConhecidos = SavedDeviceStorage.Load();

                if (listConhecidos != null)
                {
                    foreach (var device in listConhecidos)
                    {
                        grupoConhecidos.Add(new ShowDevice
                        {
                            Uuid = device.Uuid,
                            Equipamento = device.Equipamento,
                            IsOnline = false // Padrão offline até o scan achar
                        });
                    }
                }
            }
        }

        // Botão de Scan na Toolbar
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
                if (access != AccessState.Available)
                {
                    await Shell.Current.DisplayAlert("Permissão", "Bluetooth necessário.", "OK");
                    return;
                }

                ResetDeviceStatus(); // Limpa lista de novos e reseta status
                IsScanning = true;
                _bluetoothService.StartScan();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erro", $"Falha no scan: {ex.Message}", "OK");
                IsScanning = false;
            }
        }

        // Evento disparado pelo BluetoothService quando acha algo
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

            // Limpa novos para não duplicar
            grupoNovos.Clear();

            foreach (var peripheral in onlinePeripherals)
            {
                // Verifica se já conhecemos este UUID
                var dispositivoConhecido = grupoConhecidos.FirstOrDefault(d => string.Equals(d.Uuid, peripheral.Uuid, StringComparison.OrdinalIgnoreCase));

                if (dispositivoConhecido != null)
                {
                    dispositivoConhecido.IsOnline = true; // Marca como online na lista de conhecidos
                }
                else
                {
                    // Adiciona na lista de novos
                    grupoNovos.Add(new ShowDevice
                    {
                        Uuid = peripheral.Uuid,
                        Equipamento = peripheral.Name ?? "Novo Dispositivo",
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

        // CLIQUE NA LISTA (A CORREÇÃO PRINCIPAL)
        [RelayCommand]
        public async Task DeviceTapped(ShowDevice selectedDevice)
        {
            if (selectedDevice == null) return;

            _bluetoothService.StopScan();
            IsScanning = false;

            // Lógica: Navega para a tela de Detalhes passando o UUID
            // Lá na outra tela (DeviceDetailViewModel) ele vai carregar os dados e tentar conectar
            await Shell.Current.GoToAsync($"{nameof(DeviceDetailView)}?DeviceUuid={selectedDevice.Uuid}");

            SelectedDevice = null; // Limpa seleção visual
        }
    }
}