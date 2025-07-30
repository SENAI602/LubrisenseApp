using CommunityToolkit.Mvvm.ComponentModel;
using Lubrisense.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lubrisense.ViewModels
{
    [QueryProperty(nameof(DeviceId), "DeviceId")]
    public partial class DeviceDetailViewModel : ObservableObject
    {
        private string DeviceId;

        private readonly BluetoothService _bluetoothService;

        public DeviceDetailViewModel(BluetoothService bluetoothService)
        {
            _bluetoothService = bluetoothService;
        }
    }
}
