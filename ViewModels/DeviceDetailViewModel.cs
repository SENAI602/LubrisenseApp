using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lubrisense.Helpers;
using Lubrisense.Models;
using Lubrisense.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lubrisense.ViewModels
{
    public partial class DeviceDetailViewModel : ObservableObject
    {
        [ObservableProperty]
        private string textoTag;

        [ObservableProperty]
        private string textoEquipamento;

        [ObservableProperty]
        private string textoSetor;

        [ObservableProperty]
        private string textoLubrificante;

        [ObservableProperty]
        private bool erroEquipamento;

        [ObservableProperty]
        private bool erroSetor;

        public string DeviceUuid;

        private readonly BluetoothService _bluetoothService;

        public DeviceDetailViewModel(BluetoothService bluetoothService)
        {
            _bluetoothService = bluetoothService;
        }

        [RelayCommand]
        private async Task Salvar()
        {
            if (string.IsNullOrWhiteSpace(TextoEquipamento))
            {
                ErroEquipamento = true;
                return;
            }
            else ErroEquipamento = false;
            if (string.IsNullOrWhiteSpace(TextoSetor))
            {
                ErroSetor = true;
                return;
            }
            else ErroSetor = false;

            var novoDevice = new SavedDevice(DeviceUuid)
            {
                Tag = TextoTag ?? string.Empty,
                Equipamento = TextoEquipamento,
                Setor = TextoSetor,
                Lubrificante = TextoLubrificante ?? string.Empty
            };

            SavedDeviceStorage.AddOrUpdate(novoDevice);

            var confirm = await Shell.Current.DisplayAlert("Dispositivo adicionado", "Deseja configurar agora?", "Sim", "Não");
            if (confirm)
            {
                await Shell.Current.GoToAsync($"../DeviceConfigView?DeviceUuid={DeviceUuid}");
            }
            else
            {
                await Shell.Current.GoToAsync("..");
            }
        }
    }
}
