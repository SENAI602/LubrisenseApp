using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Lubrisense.Models
{
    public partial class ShowDevice : ObservableObject
    {
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MacAddressDisplay))]
        private string _uuid;

        [ObservableProperty]
        private string _equipamento;

        [ObservableProperty]
        private bool _isOnline;

        public string MacAddressDisplay
        {
            get
            {
                if (!Guid.TryParse(Uuid, out Guid deviceGuid))
                    return "ID Inválido";
                var bytes = deviceGuid.ToByteArray();
                var macBytes = new byte[6];
                Array.Copy(bytes, 10, macBytes, 0, 6);
                return BitConverter.ToString(macBytes).Replace('-', ':');
            }
        }
    }
}
