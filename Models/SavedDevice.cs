using System.Text.Json.Serialization;
using static Lubrisense.App;

namespace Lubrisense.Models
{
    public class SavedDevice
    {
        public string Uuid { get; set; }

        public string Tag { get; set; }

        public string Equipamento { get; set; }

        public string Setor { get; set; }

        public string Lubrificante { get; set; }

        public CONFIGTYPE TipoConfig { get; set; }

        public INTERVALTYPE TipoIntervalo { get; set; }

        public int Volume { get; set; } //gramas

        public int Intervalo { get; set; }

        public DateTime UltimaConexao { get; set; }

        public SavedDevice(string uuid)
        {
            Uuid = uuid;
            Tag = string.Empty;
            Equipamento = string.Empty;
            Setor = string.Empty;
            Lubrificante = string.Empty;
            TipoConfig = CONFIGTYPE.BASICO;
            TipoIntervalo = INTERVALTYPE.NONE;
            Volume = 0;
            Intervalo = 0;
            UltimaConexao = DateTime.Now;
        }


        [JsonIgnore]
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