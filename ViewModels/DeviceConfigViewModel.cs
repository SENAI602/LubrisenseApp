using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lubrisense.Helpers;
using Lubrisense.Models;
using Lubrisense.Services;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace Lubrisense.ViewModels
{
    [QueryProperty(nameof(DeviceUuid), "DeviceUuid")]
    public partial class DeviceConfigViewModel : ObservableObject
    {
        // --- Comandos Manuais ---
        public ICommand OnChangeBasicClickedCommand { get; }
        public ICommand OnChangeAdvanceClickedCommand { get; }
        public ICommand SalvarCommand { get; }

        // =================================================================
        // PROPRIEDADES ALINHADAS COM O XAML (DeviceConfigView.xaml)
        // =================================================================

        // 1. Campos Comuns
        [ObservableProperty] private int volume; // XAML busca "Volume"
        [ObservableProperty] private bool erroVolume;

        [ObservableProperty] private int frequencia;
        [ObservableProperty] private bool erroFrequencia;

        [ObservableProperty] private string tipoFrequencia;
        [ObservableProperty] private bool erroTipoFrequencia;

        // 2. Campos Modo Básico
        [ObservableProperty] private string duracaoTotal; // XAML busca "DuracaoTotal"
        [ObservableProperty] private bool erroDuracaoTotal;

        [ObservableProperty] private string tipoDuracao; // XAML busca "TipoDuracao"
        [ObservableProperty] private bool erroTipoDuracao;

        // 3. Campos Modo Avançado
        [ObservableProperty] private int intervaloAvancado;
        [ObservableProperty] private bool erroIntervaloAvancado;

        [ObservableProperty] private string tipoIntervalo; // Unidade do ciclo avançado
        [ObservableProperty] private bool erroTipoIntervalo;

        // 4. Controle
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

        // --- CONSTRUTOR ---
        public DeviceConfigViewModel(BluetoothService bluetoothService)
        {
            _bluetoothService = bluetoothService;
            _bluetoothService.DataReceived += OnDataReceived;

            OnChangeBasicClickedCommand = new Command(() => {
                ClearErrors();
                ConfigIsAdvanced = false;
            });

            OnChangeAdvanceClickedCommand = new Command(() => {
                ClearErrors();
                ConfigIsAdvanced = true;
            });

            SalvarCommand = new Command(async () => await Salvar());
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

                // Carrega campos comuns
                Volume = Device.Volume;
                Frequencia = Device.Frequencia > 0 ? Device.Frequencia : 1;
                TipoFrequencia = ConvertIntEnumToString(Device.TipoFrequencia);

                if (Device.TipoConfig == 1) // Básico
                {
                    ConfigIsAdvanced = false;
                    DuracaoTotal = Device.Intervalo.ToString();
                    TipoDuracao = ConvertIntEnumToString(Device.TipoIntervalo);
                }
                else // Avançado
                {
                    ConfigIsAdvanced = true;
                    IntervaloAvancado = Device.Intervalo;
                    TipoIntervalo = ConvertIntEnumToString(Device.TipoIntervalo);
                }
            }
        }

        private void ClearErrors()
        {
            ErroVolume = false;
            ErroDuracaoTotal = false; ErroTipoDuracao = false;
            ErroIntervaloAvancado = false; ErroTipoIntervalo = false;
            ErroFrequencia = false; ErroTipoFrequencia = false;
        }

        private async Task Salvar()
        {
            if (Device == null || IsBusy) return;
            IsBusy = true;
            ClearErrors();

            try
            {
                bool isValid = true;

                // Validação Comum
                if (Volume <= 0) { ErroVolume = true; isValid = false; }
                if (Frequencia <= 0) { ErroFrequencia = true; isValid = false; }
                if (IsPickerInvalid(TipoFrequencia)) { ErroTipoFrequencia = true; isValid = false; }

                if (ConfigIsAdvanced)
                {
                    // Validação Avançado
                    if (IntervaloAvancado <= 0) { ErroIntervaloAvancado = true; isValid = false; }
                    if (IsPickerInvalid(TipoIntervalo)) { ErroTipoIntervalo = true; isValid = false; }

                    if (!isValid) return;

                    Device.TipoConfig = 2; // Avançado
                    Device.Intervalo = IntervaloAvancado;
                    Device.TipoIntervalo = ConvertStringEnumToInt(TipoIntervalo);
                }
                else
                {
                    // Validação Básico
                    if (!int.TryParse(DuracaoTotal, out int duracaoVal) || duracaoVal <= 0) { ErroDuracaoTotal = true; isValid = false; }
                    if (IsPickerInvalid(TipoDuracao)) { ErroTipoDuracao = true; isValid = false; }

                    if (!isValid) return;

                    Device.TipoConfig = 1; // Básico
                    Device.Intervalo = int.Parse(DuracaoTotal);
                    Device.TipoIntervalo = ConvertStringEnumToInt(TipoDuracao);
                }

                // Salva Comuns
                Device.Volume = Volume;
                Device.Frequencia = Frequencia;
                Device.TipoFrequencia = ConvertStringEnumToInt(TipoFrequencia);
                Device.UltimaConexao = DateTime.Now;

                SavedDeviceStorage.AddOrUpdate(Device);

                // --- Envio Bluetooth ---
                bool connected = await _bluetoothService.ConnectToDeviceAsync(Device.Uuid);

                if (connected)
                {
                    var pacote = new { comando = "set_config", payload = Device };
                    var jsonParaEnviar = JsonSerializer.Serialize(pacote);

                    _respostaRecebidaTcs = new TaskCompletionSource<bool>();

                    Console.WriteLine($"[APP] Enviando: {jsonParaEnviar}");
                    bool enviou = await _bluetoothService.SendDataAsync(jsonParaEnviar);

                    if (enviou)
                    {
                        var timeoutTask = Task.Delay(10000);
                        var completedTask = await Task.WhenAny(_respostaRecebidaTcs.Task, timeoutTask);

                        if (completedTask == _respostaRecebidaTcs.Task && await _respostaRecebidaTcs.Task)
                        {
                            await Shell.Current.DisplayAlert("Sucesso", "Configuração confirmada!", "OK");
                            await Shell.Current.GoToAsync("../..");
                        }
                        else
                        {
                            await Shell.Current.DisplayAlert("Aviso", "Enviado, mas sem confirmação (Timeout).", "OK");
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

        // --- Helpers ---
        private bool IsPickerInvalid(string val) => string.IsNullOrWhiteSpace(val) || val == "Nenhum";

        private string ConvertIntEnumToString(int val) => val switch { 1 => "Hora", 2 => "Dia", 3 => "Mês", _ => "Dia" };

        private int ConvertStringEnumToInt(string val) => val switch { "Hora" => 1, "Dia" => 2, "Mês" => 3, _ => 2 };
    }
}