using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lubrisense.Models
{
    public class DeviceGroup : ObservableCollection<ShowDevice>
    {
        public string Nome { get; private set; }

        public DeviceGroup(string nome, List<ShowDevice> devices) : base(devices)
        {
            Nome = nome;
        }
    }
}
