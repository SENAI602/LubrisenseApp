using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lubrisense.Helpers;
using Lubrisense.Models;

namespace Lubrisense.ViewModels
{
    [QueryProperty(nameof(DeviceUuid), "DeviceUuid")]
    public partial class DeviceMenuViewModel : ObservableObject
    {
        [ObservableProperty] private string _deviceTitle = "Dispositivo";
        [ObservableProperty] private string _deviceSubTitle = "";
        [ObservableProperty] private string _deviceSetor = "";
        [ObservableProperty] private string _deviceLubricant = "";

        // --- NOVAS PROPRIEDADES VISUAIS ---
        [ObservableProperty] private string _deviceBattery = "-- %";
        [ObservableProperty] private string _deviceTemperature = "-- °C";
        // ----------------------------------

        private string _deviceUuid;
        public string DeviceUuid
        {
            get => _deviceUuid;
            set
            {
                SetProperty(ref _deviceUuid, value);
                LoadDeviceInfo(value);
            }
        }

        private void LoadDeviceInfo(string uuid)
        {
            var device = SavedDeviceStorage.GetById(uuid);
            if (device != null)
            {
                DeviceTitle = string.IsNullOrEmpty(device.Equipamento) ? "Dispositivo Sem Nome" : device.Equipamento;
                DeviceSubTitle = string.IsNullOrEmpty(device.Tag) ? "Sem TAG" : $"TAG: {device.Tag}";
                DeviceSetor = string.IsNullOrEmpty(device.Setor) ? "Setor não definido" : $"Setor: {device.Setor}";
                DeviceLubricant = string.IsNullOrEmpty(device.Lubrificante) ? "Lubrificante não definido" : $"Lubrificante: {device.Lubrificante}";

                // Carrega Status
                DeviceBattery = $"{device.Bateria}%";
                DeviceTemperature = $"{device.Temperatura:F1}°C";
            }
        }

        [RelayCommand]
        private async Task GoToParameters()
        {
            await Shell.Current.GoToAsync($"DeviceConfigView?DeviceUuid={DeviceUuid}");
        }

        [RelayCommand]
        private async Task GoToHistory()
        {
            await Shell.Current.DisplayAlert("Em Breve", "A tela de histórico será implementada na próxima etapa.", "OK");
        }

        [RelayCommand]
        private async Task ToggleManualDosage()
        {
            await Shell.Current.DisplayAlert("Controle Manual", "Funcionalidade de habilitar/desabilitar dosagem manual.", "OK");
        }
    }
}