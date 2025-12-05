using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lubrisense.Helpers;
using Lubrisense.Models;
using Lubrisense.Services;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Lubrisense.ViewModels
{
    [QueryProperty(nameof(DeviceUuid), "DeviceUuid")]
    public partial class DeviceConfigViewModel : ObservableObject
    {
        // --- Comandos ---
        public ICommand OnChangeBasicClickedCommand { get; }
        public ICommand OnChangeAdvanceClickedCommand { get; }
        public ICommand SalvarCommand { get; }
        public ICommand ExcluirCommand { get; } // Apenas Excluir sobrou

        // =================================================================
        // 1. PROPRIEDADES DE IDENTIFICAÇÃO
        // =================================================================
        [ObservableProperty] private string textoTag;
        [ObservableProperty] private string textoEquipamento;
        [ObservableProperty] private string textoSetor;
        [ObservableProperty] private string textoLubrificante;

        [ObservableProperty] private bool erroEquipamento;
        [ObservableProperty] private bool erroSetor;

        // =================================================================
        // 2. PROPRIEDADES DE STATUS
        // =================================================================
        [ObservableProperty] private string statusConexao = "Conectado";
        [ObservableProperty] private Color corStatus = Colors.Green;
        [ObservableProperty] private bool isBusy;

        // =================================================================
        // 3. PROPRIEDADES DE CONFIGURAÇÃO
        // =================================================================
        [ObservableProperty] private int volume;
        [ObservableProperty] private bool erroVolume;

        [ObservableProperty] private int frequencia;
        [ObservableProperty] private bool erroFrequencia;

        [ObservableProperty] private string tipoFrequencia;
        [ObservableProperty] private bool erroTipoFrequencia;

        // Básico
        [ObservableProperty] private string duracaoTotal;
        [ObservableProperty] private bool erroDuracaoTotal;
        [ObservableProperty] private string tipoDuracao;
        [ObservableProperty] private bool erroTipoDuracao;

        // Controle de Modo
        [ObservableProperty] private bool configIsAdvanced;

        // =================================================================

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

            OnChangeBasicClickedCommand = new Command(() => { ClearErrors(); ConfigIsAdvanced = false; });
            OnChangeAdvanceClickedCommand = new Command(() => { ClearErrors(); ConfigIsAdvanced = true; });

            SalvarCommand = new Command(async () => await Salvar());
            ExcluirCommand = new Command(async () => await Excluir());
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
            if (device == null) device = new SavedDevice(deviceUuid);

            Device = device;

            // Carrega dados
            TextoTag = Device.Tag;
            TextoEquipamento = Device.Equipamento;
            TextoSetor = Device.Setor;
            TextoLubrificante = Device.Lubrificante;

            Volume = Device.Volume;
            Frequencia = Device.Frequencia > 0 ? Device.Frequencia : 1;
            TipoFrequencia = ConvertIntEnumToString(Device.TipoFrequencia);

            if (Device.TipoConfig == 1) // Básico
            {
                ConfigIsAdvanced = false;
                DuracaoTotal = Device.Intervalo > 0 ? Device.Intervalo.ToString() : "";
                TipoDuracao = ConvertIntEnumToString(Device.TipoIntervalo);
            }
            else // Avançado
            {
                ConfigIsAdvanced = true;
                // No modo avançado, não usamos mais Intervalo/TipoIntervalo específicos na tela,
                // pois o cálculo é feito por Volume direto e Frequência (Gatilho).
            }
        }

        private void ClearErrors()
        {
            ErroVolume = false; ErroDuracaoTotal = false; ErroTipoDuracao = false;
            ErroFrequencia = false; ErroTipoFrequencia = false;
            ErroEquipamento = false; ErroSetor = false;
        }

        private async Task Salvar()
        {
            if (Device == null || IsBusy) return;
            IsBusy = true;
            ClearErrors();

            try
            {
                bool isValid = true;

                // Validação Identificação
                if (string.IsNullOrWhiteSpace(TextoEquipamento)) { ErroEquipamento = true; isValid = false; }
                if (string.IsNullOrWhiteSpace(TextoSetor)) { ErroSetor = true; isValid = false; }

                // Validação Comum
                if (Volume <= 0) { ErroVolume = true; isValid = false; }
                if (Frequencia <= 0) { ErroFrequencia = true; isValid = false; }
                if (IsPickerInvalid(TipoFrequencia)) { ErroTipoFrequencia = true; isValid = false; }

                if (ConfigIsAdvanced)
                {
                    // No modo avançado, apenas Volume e Frequência importam.
                    if (isValid)
                    {
                        Device.TipoConfig = 2;
                        // Zeramos os campos de "Intervalo" do modo básico para evitar confusão no ESP
                        Device.Intervalo = 0;
                        Device.TipoIntervalo = 0;
                    }
                }
                else
                {
                    // Validação Básico
                    if (!int.TryParse(DuracaoTotal, out int duracaoVal) || duracaoVal <= 0) { ErroDuracaoTotal = true; isValid = false; }
                    if (IsPickerInvalid(TipoDuracao)) { ErroTipoDuracao = true; isValid = false; }

                    if (isValid)
                    {
                        Device.TipoConfig = 1;
                        Device.Intervalo = int.Parse(DuracaoTotal);
                        Device.TipoIntervalo = ConvertStringEnumToInt(TipoDuracao);
                    }
                }

                if (!isValid) return;

                // Atualiza Objeto
                Device.Tag = TextoTag ?? "";
                Device.Equipamento = TextoEquipamento;
                Device.Setor = TextoSetor;
                Device.Lubrificante = TextoLubrificante ?? "";

                Device.Volume = Volume;
                Device.Frequencia = Frequencia;
                Device.TipoFrequencia = ConvertStringEnumToInt(TipoFrequencia);
                Device.UltimaConexao = DateTime.Now;

                // Salva Local
                SavedDeviceStorage.AddOrUpdate(Device);

                // Envio Bluetooth
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
                            await Shell.Current.DisplayAlert("Sucesso", "Dados salvos e enviados ao dispositivo!", "OK");
                            await Shell.Current.GoToAsync("..");
                        }
                        else
                        {
                            await Shell.Current.DisplayAlert("Aviso", "Salvo localmente e enviado, mas sem confirmação do dispositivo.", "OK");
                            await Shell.Current.GoToAsync("..");
                        }
                    }
                    else
                    {
                        await Shell.Current.DisplayAlert("Erro", "Falha no envio Bluetooth. Salvo apenas localmente.", "OK");
                    }
                }
                else
                {
                    bool salvarLocal = await Shell.Current.DisplayAlert("Sem Conexão", "Dispositivo desconectou. Salvar apenas no celular?", "Sim", "Não");
                    if (salvarLocal) await Shell.Current.GoToAsync("..");
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task Excluir()
        {
            bool confirm = await Shell.Current.DisplayAlert("Excluir", "Remover este dispositivo da lista?", "Sim", "Não");
            if (confirm)
            {
                SavedDeviceStorage.Remove(DeviceUuid);
                await Shell.Current.GoToAsync("..");
            }
        }

        private bool IsPickerInvalid(string val) => string.IsNullOrWhiteSpace(val) || val == "Nenhum";
        private string ConvertIntEnumToString(int val) => val switch { 1 => "Hora", 2 => "Dia", 3 => "Mês", _ => "Dia" };
        private int ConvertStringEnumToInt(string val) => val switch { "Hora" => 1, "Dia" => 2, "Mês" => 3, _ => 2 };
    }
}