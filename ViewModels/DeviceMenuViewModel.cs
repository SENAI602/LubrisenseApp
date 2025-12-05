using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lubrisense.Helpers;
using Lubrisense.Models;

namespace Lubrisense.ViewModels
{
    [QueryProperty(nameof(DeviceUuid), "DeviceUuid")]
    public partial class DeviceMenuViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _deviceTitle = "Dispositivo";

        [ObservableProperty]
        private string _deviceSubTitle = "";

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
                DeviceSubTitle = string.IsNullOrEmpty(device.Tag) ? "Sem TAG" : device.Tag;
            }
        }

        // 1. Navegar para Configuração (Edição de Parâmetros)
        [RelayCommand]
        private async Task GoToParameters()
        {
            // Navega para a tela de configuração que já criamos
            await Shell.Current.GoToAsync($"DeviceConfigView?DeviceUuid={DeviceUuid}");
        }

        // 2. Navegar para Histórico (Futuro)
        [RelayCommand]
        private async Task GoToHistory()
        {
            await Shell.Current.DisplayAlert("Em Breve", "A tela de histórico será implementada na próxima etapa.", "OK");
            // Futuro: await Shell.Current.GoToAsync($"DeviceHistoryView?DeviceUuid={DeviceUuid}");
        }

        // 3. Controle Manual
        [RelayCommand]
        private async Task ToggleManualDosage()
        {
            await Shell.Current.DisplayAlert("Controle Manual", "Funcionalidade de habilitar/desabilitar dosagem manual.", "OK");
        }
    }
}