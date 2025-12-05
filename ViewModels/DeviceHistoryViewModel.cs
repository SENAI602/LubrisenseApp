using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lubrisense.Models;
using Lubrisense.Services;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace Lubrisense.ViewModels
{
    [QueryProperty(nameof(DeviceUuid), "DeviceUuid")]
    public partial class DeviceHistoryViewModel : ObservableObject
    {
        private readonly BluetoothService _bluetoothService;

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private ObservableCollection<LogEvent> historicoList;
        [ObservableProperty] private bool temDados;

        private string _deviceUuid;
        public string DeviceUuid
        {
            get => _deviceUuid;
            set
            {
                SetProperty(ref _deviceUuid, value);
                if (!string.IsNullOrEmpty(value)) _ = CarregarHistorico();
            }
        }

        public DeviceHistoryViewModel(BluetoothService bluetoothService)
        {
            _bluetoothService = bluetoothService;
            HistoricoList = new ObservableCollection<LogEvent>();
        }

        [RelayCommand]
        private async Task CarregarHistorico()
        {
            if (IsBusy) return;
            IsBusy = true;
            HistoricoList.Clear();
            TemDados = false;

            try
            {
                // 1. Garante conexão
                bool conectado = await _bluetoothService.ConnectToDeviceAsync(DeviceUuid);
                if (!conectado)
                {
                    await Shell.Current.DisplayAlert("Erro", "Não foi possível conectar ao dispositivo para ler o histórico.", "OK");
                    await Shell.Current.GoToAsync("..");
                    return;
                }

                // 2. Pede o log
                var comando = "{\"comando\": \"get_log\"}";
                string? jsonRecebido = await _bluetoothService.RequestDataAsync(comando, 15000);

                if (!string.IsNullOrEmpty(jsonRecebido))
                {
                    using JsonDocument doc = JsonDocument.Parse(jsonRecebido);
                    if (doc.RootElement.TryGetProperty("logs", out JsonElement logsArray))
                    {
                        var logs = logsArray.Deserialize<List<LogEvent>>();

                        if (logs != null && logs.Count > 0)
                        {
                            // Ordena do mais recente para o mais antigo
                            logs.Reverse();

                            foreach (var log in logs)
                            {
                                HistoricoList.Add(log);
                            }
                            TemDados = true;
                        }
                    }
                }
                else
                {
                    await Shell.Current.DisplayAlert("Vazio", "Nenhum histórico recebido ou falha na comunicação.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erro", $"Falha ao processar histórico: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task Voltar()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}