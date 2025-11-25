using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lubrisense.Helpers;
using Lubrisense.Models;
using Lubrisense.Services;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls; // Para Shell e Alerts

namespace Lubrisense.ViewModels
{
    [QueryProperty(nameof(DeviceUuid), "DeviceUuid")]
    public partial class DeviceConfigViewModel : ObservableObject
    {
        // --- PROPRIEDADES VISUAIS ---
        [ObservableProperty] private int volumeBasico;
        [ObservableProperty] private bool erroVolumeBasico;

        [ObservableProperty] private string intervaloBasico;
        [ObservableProperty] private bool erroIntervaloBasico;

        [ObservableProperty] private int volumeAvancado;
        [ObservableProperty] private bool erroVolumeAvancado;

        [ObservableProperty] private int intervaloAvancado;
        [ObservableProperty] private bool erroIntervaloAvancado;

        [ObservableProperty] private string tipoIntervalo;
        [ObservableProperty] private bool erroTipoIntervalo;

        [ObservableProperty] private bool configIsAdvanced;

        // --- DADOS DO DISPOSITIVO ---
        public string DeviceUuid
        {
            get => _deviceUuid;
            set
            {
                SetProperty(ref _deviceUuid, value);
                if (!string.IsNullOrEmpty(value)) SetupDevice(value);
            }
        }
        private string _deviceUuid;

        public SavedDevice? Device;
        private readonly BluetoothService _bluetoothService;

        public DeviceConfigViewModel(BluetoothService bluetoothService)
        {
            _bluetoothService = bluetoothService;
        }

        public void SetupDevice(string deviceUuid)
        {
            var device = SavedDeviceStorage.GetById(deviceUuid); // Usa GetById corrigido
            if (device != null)
            {
                Device = device;

                // Define qual tela mostrar (Básico ou Avançado)
                if (Device.TipoConfig == App.CONFIGTYPE.BASICO)
                {
                    IntervaloBasico = Device.Intervalo.ToString();
                    VolumeBasico = Device.Volume;
                    ConfigIsAdvanced = false;
                }
                else
                {
                    IntervaloAvancado = Device.Intervalo;
                    VolumeAvancado = Device.Volume;

                    ConfigIsAdvanced = true;

                    switch (Device.TipoIntervalo)
                    {
                        case App.INTERVALTYPE.HORA: TipoIntervalo = "Hora"; break;
                        case App.INTERVALTYPE.DIA: TipoIntervalo = "Dia"; break;
                        case App.INTERVALTYPE.MES: TipoIntervalo = "Mês"; break;
                        default: TipoIntervalo = "Nenhum"; break;
                    }
                }
            }
        }

        [RelayCommand]
        private void OnChangeBasicClicked()
        {
            ClearDevice();
            ConfigIsAdvanced = false;
        }

        [RelayCommand]
        private void OnChangeAdvanceClicked()
        {
            ClearDevice();
            ConfigIsAdvanced = true;
        }

        private void ClearDevice()
        {
            if (Device == null) return;

            // Apenas reseta os erros visuais, não zera os dados para facilitar a edição
            ErroIntervaloAvancado = false;
            ErroVolumeAvancado = false;
            ErroIntervaloBasico = false;
            ErroVolumeBasico = false;
        }

        // --- LÓGICA DE SALVAR E ENVIAR ---
        [RelayCommand]
        private async Task Salvar()
        {
            if (Device == null) return;

            bool isValid = true;

            // --- VALIDAÇÃO DOS CAMPOS ---
            if (ConfigIsAdvanced)
            {
                if (VolumeAvancado <= 0) { ErroVolumeAvancado = true; isValid = false; } else ErroVolumeAvancado = false;
                if (IntervaloAvancado <= 0) { ErroIntervaloAvancado = true; isValid = false; } else ErroIntervaloAvancado = false;

                if (string.IsNullOrWhiteSpace(TipoIntervalo) || TipoIntervalo.Equals("Nenhum")) { ErroTipoIntervalo = true; isValid = false; }
                else ErroTipoIntervalo = false;

                if (!isValid) return;

                Device.TipoConfig = App.CONFIGTYPE.AVANCADO;
                Device.TipoIntervalo = TipoIntervalo == "Hora" ? App.INTERVALTYPE.HORA :
                                       TipoIntervalo == "Dia" ? App.INTERVALTYPE.DIA :
                                       TipoIntervalo == "Mês" ? App.INTERVALTYPE.MES : App.INTERVALTYPE.NONE;
                Device.Volume = VolumeAvancado;
                Device.Intervalo = IntervaloAvancado;
            }
            else
            {
                if (VolumeBasico <= 0) { ErroVolumeBasico = true; isValid = false; } else ErroVolumeBasico = false;

                if (!int.TryParse(IntervaloBasico, out int result) || result <= 0) { ErroIntervaloBasico = true; isValid = false; }
                else { ErroIntervaloBasico = false; Device.Intervalo = result; }

                if (!isValid) return;

                Device.TipoConfig = App.CONFIGTYPE.BASICO;
                Device.Volume = VolumeBasico;
                // No básico, geralmente assumimos "Mês" ou "Dia" como padrão se não houver seletor
                // Vamos manter o que estava antes ou forçar um padrão:
                if (Device.TipoIntervalo == App.INTERVALTYPE.NONE) Device.TipoIntervalo = App.INTERVALTYPE.MES;
            }

            // --- ENVIO PARA O ESP32 ---

            // 1. Conecta
            bool connected = await _bluetoothService.ConnectToDeviceAsync(Device.Uuid);

            if (connected)
            {
                // 2. Prepara o JSON no formato { comando, payload }
                // CORREÇÃO CRÍTICA AQUI:
                var pacote = new
                {
                    comando = "set_config",
                    payload = Device
                };

                var jsonParaEnviar = JsonSerializer.Serialize(pacote);

                // 3. Envia
                bool result = await _bluetoothService.SendDataAsync(jsonParaEnviar);

                if (result)
                {
                    // 4. Se enviou com sucesso, salva no celular também
                    SavedDeviceStorage.AddOrUpdate(Device);
                    await Shell.Current.DisplayAlert("Sucesso", "Configuração enviada para o LubriSense!", "OK");
                    await Shell.Current.GoToAsync("../.."); // Volta para a Home
                }
                else
                {
                    await Shell.Current.DisplayAlert("Erro", "Falha no envio dos dados.", "OK");
                }
            }
            else
            {
                // Opção: Salvar localmente mesmo sem conexão?
                bool salvarLocal = await Shell.Current.DisplayAlert("Erro de Conexão", "Não foi possível conectar ao ESP32. Deseja salvar apenas no celular?", "Sim", "Não");
                if (salvarLocal)
                {
                    SavedDeviceStorage.AddOrUpdate(Device);
                    await Shell.Current.GoToAsync("../..");
                }
            }
        }
    }
}