using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lubrisense.Helpers;
using Lubrisense.Models;
using Lubrisense.Services;
using Microsoft.Maui.Graphics;
using System.Threading.Tasks;
using Microsoft.Maui.Controls; // Necessário para Shell e Alerts

namespace Lubrisense.ViewModels
{
    [QueryProperty(nameof(DeviceUuid), "DeviceUuid")]
    public partial class DeviceDetailViewModel : ObservableObject
    {
        private readonly BluetoothService _bluetoothService;

        // --- Propriedades da Tela ---
        [ObservableProperty] private string textoTag;
        [ObservableProperty] private string textoEquipamento;
        [ObservableProperty] private string textoSetor;
        [ObservableProperty] private string textoLubrificante;

        // Validação Visual
        [ObservableProperty] private bool erroEquipamento;
        [ObservableProperty] private bool erroSetor;

        // Controle de Carregamento e Status
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string statusConexao = "Desconectado";
        [ObservableProperty] private Color corStatus = Colors.Red; // Vermelho padrão

        // ID do Dispositivo
        private string _deviceUuid;
        public string DeviceUuid
        {
            get => _deviceUuid;
            set
            {
                SetProperty(ref _deviceUuid, value);
                if (!string.IsNullOrEmpty(value))
                {
                    CarregarDadosSalvos();
                    // Tenta conectar assim que entra na tela
                    _ = ConectarAutomaticamente();
                }
            }
        }

        public DeviceDetailViewModel(BluetoothService bluetoothService)
        {
            _bluetoothService = bluetoothService;

            // Ouve evento de queda de conexão do Serviço (Se implementado no Service)
            // Se o Service não tiver esse evento ainda, esta linha pode ser removida ou comentada
            // _bluetoothService.DeviceDisconnected += OnDeviceDisconnected; 
        }

        // Método para atualizar a UI quando cai a conexão (Opcional)
        private void OnDeviceDisconnected()
        {
            StatusConexao = "Conexão Perdida";
            CorStatus = Colors.Red;
        }

        // 1. CARREGA DADOS
        private void CarregarDadosSalvos()
        {
            var deviceSalvo = SavedDeviceStorage.GetById(DeviceUuid);
            if (deviceSalvo != null)
            {
                TextoTag = deviceSalvo.Tag;
                TextoEquipamento = deviceSalvo.Equipamento;
                TextoSetor = deviceSalvo.Setor;
                TextoLubrificante = deviceSalvo.Lubrificante;
            }
        }

        // 2. RECONEXÃO AUTOMÁTICA
        private async Task ConectarAutomaticamente()
        {
            if (IsBusy) return;

            StatusConexao = "Conectando...";
            CorStatus = Colors.Orange;
            IsBusy = true;

            // Pequeno delay visual
            await Task.Delay(500);

            bool sucesso = await _bluetoothService.ConnectToDeviceAsync(DeviceUuid);

            IsBusy = false;

            if (sucesso)
            {
                StatusConexao = "Conectado";
                CorStatus = Colors.Green;
            }
            else
            {
                StatusConexao = "Desconectado";
                CorStatus = Colors.Red;
            }
        }

        // 3. SALVAR E NAVEGAR
        [RelayCommand]
        private async Task Salvar()
        {
            // Validação Básica
            bool temErro = false;
            if (string.IsNullOrWhiteSpace(TextoEquipamento)) { ErroEquipamento = true; temErro = true; } else ErroEquipamento = false;
            if (string.IsNullOrWhiteSpace(TextoSetor)) { ErroSetor = true; temErro = true; } else ErroSetor = false;

            if (temErro) return;

            var deviceAtualizado = new SavedDevice(DeviceUuid)
            {
                Tag = TextoTag ?? string.Empty,
                Equipamento = TextoEquipamento,
                Setor = TextoSetor ?? string.Empty,
                Lubrificante = TextoLubrificante ?? string.Empty
            };

            // Salva no banco local
            SavedDeviceStorage.AddOrUpdate(deviceAtualizado);

            // Pergunta se quer configurar a dosagem
            var confirm = await Shell.Current.DisplayAlert("Salvo", "Deseja configurar os parâmetros (Dose/Intervalo) agora?", "Sim", "Não");

            if (confirm)
            {
                // Navega para a tela de Configuração
                await Shell.Current.GoToAsync($"../DeviceConfigView?DeviceUuid={DeviceUuid}");
            }
            else
            {
                // Volta para a lista
                await Shell.Current.GoToAsync("..");
            }
        }

        // 4. EXCLUIR
        [RelayCommand]
        private async Task Excluir()
        {
            bool confirm = await App.Current.MainPage.DisplayAlert("Excluir", "Remover este dispositivo da lista?", "Sim", "Não");
            if (confirm)
            {
                SavedDeviceStorage.Remove(DeviceUuid);
                await Shell.Current.GoToAsync("..");
            }
        }

        // 5. TESTES DE HARDWARE (LED)
        [RelayCommand]
        public async Task LigarLed()
        {
            IsBusy = true;

            // Se não estiver conectado, tenta reconectar antes
            if (StatusConexao != "Conectado")
            {
                await ConectarAutomaticamente();
            }

            if (StatusConexao == "Conectado")
            {
                bool enviou = await _bluetoothService.SendDataAsync("1");
                if (!enviou)
                {
                    StatusConexao = "Erro ao Enviar";
                    CorStatus = Colors.Red;
                }
            }
            else
            {
                await App.Current.MainPage.DisplayAlert("Erro", "Não foi possível conectar ao dispositivo.", "OK");
            }

            IsBusy = false;
        }

        [RelayCommand]
        public async Task DesligarLed()
        {
            IsBusy = true;

            if (StatusConexao != "Conectado")
            {
                await ConectarAutomaticamente();
            }

            if (StatusConexao == "Conectado")
            {
                bool enviou = await _bluetoothService.SendDataAsync("0");
                if (!enviou)
                {
                    StatusConexao = "Erro ao Enviar";
                    CorStatus = Colors.Red;
                }
            }

            IsBusy = false;
        }
    }
}