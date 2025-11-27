using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lubrisense.Helpers;
using Lubrisense.Models;
using Lubrisense.Services;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace Lubrisense.ViewModels
{
    [QueryProperty(nameof(DeviceUuid), "DeviceUuid")]
    public partial class DeviceConfigViewModel : ObservableObject
    {
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
        [ObservableProperty] private bool isBusy;

        private string _deviceUuid;
        public string DeviceUuid
        {
            get => _deviceUuid;
            set
            {
                SetProperty(ref _deviceUuid, value);
                if (!string.IsNullOrEmpty(value)) SetupDevice(value);
            }
        }

        public SavedDevice? Device;
        private readonly BluetoothService _bluetoothService;
        private TaskCompletionSource<bool> _respostaRecebidaTcs;

        public DeviceConfigViewModel(BluetoothService bluetoothService)
        {
            _bluetoothService = bluetoothService;
            _bluetoothService.DataReceived += OnDataReceived;
        }

        ~DeviceConfigViewModel()
        {
            if (_bluetoothService != null) _bluetoothService.DataReceived -= OnDataReceived;
        }

        private void OnDataReceived(string data)
        {
            if (!string.IsNullOrEmpty(data) && data.Contains("OK"))
            {
                _respostaRecebidaTcs?.TrySetResult(true);
            }
        }

        public void SetupDevice(string deviceUuid)
        {
            var device = SavedDeviceStorage.GetById(deviceUuid);
            if (device != null)
            {
                Device = device;
                if (Device.TipoConfig == 1)
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
                        case 1: TipoIntervalo = "Hora"; break;
                        case 2: TipoIntervalo = "Dia"; break;
                        case 3: TipoIntervalo = "Mês"; break;
                        default: TipoIntervalo = "Nenhum"; break;
                    }
                }
            }
        }

        [RelayCommand] private void OnChangeBasicClicked() { ClearDevice(); ConfigIsAdvanced = false; }
        [RelayCommand] private void OnChangeAdvanceClicked() { ClearDevice(); ConfigIsAdvanced = true; }

        private void ClearDevice()
        {
            if (Device == null) return;
            ErroIntervaloAvancado = false; ErroVolumeAvancado = false;
            ErroIntervaloBasico = false; ErroVolumeBasico = false;
        }

        [RelayCommand]
        private async Task Salvar()
        {
            if (Device == null || IsBusy) return;
            IsBusy = true;

            try
            {
                // Validação
                bool isValid = true;
                if (ConfigIsAdvanced)
                {
                    if (VolumeAvancado <= 0) { ErroVolumeAvancado = true; isValid = false; } else ErroVolumeAvancado = false;
                    if (IntervaloAvancado <= 0) { ErroIntervaloAvancado = true; isValid = false; } else ErroIntervaloAvancado = false;
                    if (string.IsNullOrWhiteSpace(TipoIntervalo) || TipoIntervalo == "Nenhum") { ErroTipoIntervalo = true; isValid = false; }
                    else ErroTipoIntervalo = false;

                    if (!isValid) return;

                    Device.TipoConfig = 2;
                    Device.Volume = VolumeAvancado;
                    Device.Intervalo = IntervaloAvancado;
                    Device.TipoIntervalo = TipoIntervalo == "Hora" ? 1 : TipoIntervalo == "Dia" ? 2 : 3;
                }
                else
                {
                    if (VolumeBasico <= 0) { ErroVolumeBasico = true; isValid = false; } else ErroVolumeBasico = false;
                    if (!int.TryParse(IntervaloBasico, out int result) || result <= 0) { ErroIntervaloBasico = true; isValid = false; }
                    else { ErroIntervaloBasico = false; Device.Intervalo = result; }

                    if (!isValid) return;

                    Device.TipoConfig = 1;
                    Device.Volume = VolumeBasico;
                    Device.TipoIntervalo = 3;
                }

                SavedDeviceStorage.AddOrUpdate(Device);

                // --- ENVIO COM FATIAMENTO ---
                bool connected = await _bluetoothService.ConnectToDeviceAsync(Device.Uuid);

                if (connected)
                {
                    var pacote = new { comando = "set_config", payload = Device };
                    var jsonParaEnviar = JsonSerializer.Serialize(pacote);

                    _respostaRecebidaTcs = new TaskCompletionSource<bool>();

                    Console.WriteLine($"[APP] Enviando JSON fatiado...");
                    bool enviou = await _bluetoothService.SendDataAsync(jsonParaEnviar);

                    if (enviou)
                    {
                        // Aumentado para 10s para garantir tempo de remontagem e gravação
                        var timeoutTask = Task.Delay(10000);
                        var completedTask = await Task.WhenAny(_respostaRecebidaTcs.Task, timeoutTask);

                        if (completedTask == _respostaRecebidaTcs.Task && await _respostaRecebidaTcs.Task)
                        {
                            await Shell.Current.DisplayAlert("Sucesso", "Configuração confirmada!", "OK");
                            await Shell.Current.GoToAsync("../..");
                        }
                        else
                        {
                            await Shell.Current.DisplayAlert("Aviso", "Dados enviados, mas sem confirmação (Timeout).", "OK");
                            await Shell.Current.GoToAsync("../..");
                        }
                    }
                    else
                    {
                        await Shell.Current.DisplayAlert("Erro", "Falha no envio Bluetooth.", "OK");
                    }
                }
                else
                {
                    bool salvarLocal = await Shell.Current.DisplayAlert("Sem Conexão", "Salvar apenas no celular?", "Sim", "Não");
                    if (salvarLocal) await Shell.Current.GoToAsync("../..");
                }
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}