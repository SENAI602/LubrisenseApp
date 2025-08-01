using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lubrisense.Helpers;
using Lubrisense.Models;
using Lubrisense.Services;
using System.Text.Json;

namespace Lubrisense.ViewModels
{

    public partial class DeviceConfigViewModel : ObservableObject
    {
        [ObservableProperty]
        private int volumeBasico;

        [ObservableProperty]
        private bool erroVolumeBasico;

        [ObservableProperty]
        private string intervaloBasico;

        [ObservableProperty]
        private bool erroIntervaloBasico;

        [ObservableProperty]
        private int volumeAvancado;

        [ObservableProperty]
        private bool erroVolumeAvancado;

        [ObservableProperty]
        private int intervaloAvancado;

        [ObservableProperty]
        private bool erroIntervaloAvancado;

        [ObservableProperty]
        private string tipoIntervalo;

        [ObservableProperty]
        private bool erroTipoIntervalo;

        [ObservableProperty]
        private bool configIsAdvanced;

        public string DeviceUuid;

        public SavedDevice? Device;

        private readonly BluetoothService _bluetoothService;

        public DeviceConfigViewModel(BluetoothService bluetoothService)
        {
            _bluetoothService = bluetoothService;
        }

        public void SetupDevice(string deviceUuid)
        {
            DeviceUuid = deviceUuid;
            var device = SavedDeviceStorage.GetByUuid(deviceUuid);
            if (device != null)
            {
                Device = device;
                if(Device.TipoConfig == App.CONFIGTYPE.BASICO)
                {
                    IntervaloBasico = Device.Intervalo.ToString();
                    VolumeBasico = Device.Volume;
                    ConfigIsAdvanced = false;
                }
                else
                {
                    IntervaloAvancado = Device.Intervalo;
                    VolumeAvancado = Device.Volume;
                    TipoIntervalo = Device.TipoIntervalo.ToString();
                    ConfigIsAdvanced = true;
                    switch(Device.TipoIntervalo)
                    {
                        case App.INTERVALTYPE.HORA:
                            TipoIntervalo = "Hora";
                            break;
                        case App.INTERVALTYPE.DIA:
                            TipoIntervalo = "Dia";
                            break;
                        case App.INTERVALTYPE.MES:
                            TipoIntervalo = "Mês";
                            break;
                        default:
                            TipoIntervalo = "Nenhum";
                            break;
                    }
                }
            }
        }

        [RelayCommand]
        private void OnChangeBasicClicked()
        {
            ClearDevice();
            ConfigIsAdvanced = false;
            Device.TipoConfig = App.CONFIGTYPE.BASICO;
            IntervaloBasico = Device.Intervalo.ToString();
            VolumeBasico = Device.Volume;
        }

        [RelayCommand]
        private void OnChangeAdvanceClicked()
        {
            ClearDevice();
            ConfigIsAdvanced = true;
            Device.TipoConfig = App.CONFIGTYPE.AVANCADO;
            IntervaloAvancado = Device.Intervalo;
            VolumeAvancado = Device.Volume;
        }

        private void ClearDevice()
        {
            Device.Intervalo = 0;
            Device.Volume = 0;
            Device.TipoIntervalo = App.INTERVALTYPE.NONE;
            ErroIntervaloAvancado = false;
            ErroVolumeAvancado = false;
            ErroIntervaloBasico = false;
            ErroVolumeBasico = false;
        }

        [RelayCommand]
        private async Task Salvar()
        {
            bool isValid = true;

            if (ConfigIsAdvanced)
            {
                if(volumeAvancado <= 0)
                {
                    ErroVolumeAvancado = true;
                    isValid = false;
                }
                else ErroVolumeAvancado = false;

                if (intervaloAvancado <= 0)
                {
                    ErroIntervaloAvancado = true;
                    isValid = false;
                }
                else ErroIntervaloAvancado = false;

                if (string.IsNullOrWhiteSpace(TipoIntervalo) || TipoIntervalo.Equals("Nenhum"))
                {
                    ErroTipoIntervalo = true;
                    isValid = false;
                }
                else ErroTipoIntervalo = false;

                if (!isValid) return;

                Device.TipoConfig = App.CONFIGTYPE.AVANCADO;
                Device.TipoIntervalo = 
                    TipoIntervalo == "Hora" ? App.INTERVALTYPE.HORA :
                    TipoIntervalo == "Dia" ? App.INTERVALTYPE.DIA :
                    TipoIntervalo == "Mês" ? App.INTERVALTYPE.MES : 
                    App.INTERVALTYPE.NONE;
                Device.Volume = VolumeAvancado;
                Device.Intervalo = IntervaloAvancado;
            }
            else
            {
                if(VolumeBasico <= 0)
                {
                    ErroVolumeBasico = true;
                    isValid = false;
                }
                else ErroVolumeBasico = false;

                if (!int.TryParse(IntervaloBasico, out int result))
                {
                    ErroIntervaloBasico = true;
                    isValid = false;
                }
                else if(result <= 0)
                {
                    ErroIntervaloBasico = true;
                    isValid = false;
                }
                else ErroIntervaloBasico = false;

                if (!isValid) return;

                Device.TipoConfig = App.CONFIGTYPE.BASICO;
                Device.Volume = VolumeBasico;
                Device.Intervalo = result;
            }

            var connected = await _bluetoothService.ConnectToDeviceAsync(Device.Uuid);
            if (connected)
            {
                var json = JsonSerializer.Serialize(Device);
                bool result = await _bluetoothService.SendDataAsync(json);

                if (result)
                {
                    SavedDeviceStorage.AddOrUpdate(Device);
                    await Shell.Current.DisplayAlert("Configuração salva", "As configurações foram enviadas com sucesso.", "OK");
                    await Shell.Current.GoToAsync("..");

                }
                else
                {
                    await Shell.Current.DisplayAlert("Erro ao salvar", "Não foi possível enviar as configurações para o dispositivo.", "OK");
                }
            }
            else
            {
                await Shell.Current.DisplayAlert("Erro ao se conectar", "Não foi possível conectar ao dispositivo para configurá-lo.", "OK");
            }
        }
    }
}