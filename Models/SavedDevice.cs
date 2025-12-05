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

        // Configurações
        public int TipoConfig { get; set; }
        public int TipoIntervalo { get; set; }
        public int Volume { get; set; }
        public int Intervalo { get; set; }
        public int Frequencia { get; set; }
        public int TipoFrequencia { get; set; }

        // --- NOVOS CAMPOS DE STATUS ---
        public int Bateria { get; set; } = 0;       // % (0-100)
        public double Temperatura { get; set; } = 0.0; // Graus Celsius
        // ------------------------------

        public DateTime UltimaConexao { get; set; }

        [JsonIgnore]
        public bool IsOnline { get; set; }

        public SavedDevice() { }

        public SavedDevice(string uuid)
        {
            Uuid = uuid;
            Tag = string.Empty;
            Equipamento = string.Empty;
            Setor = string.Empty;
            Lubrificante = string.Empty;
            TipoConfig = (int)CONFIGTYPE.BASICO;
            TipoIntervalo = (int)INTERVALTYPE.MES;
            Volume = 0;
            Intervalo = 0;

            // Inicializa status
            Bateria = 0;
            Temperatura = 0;

            UltimaConexao = DateTime.Now;
            IsOnline = false;
        }

        [JsonIgnore]
        public string MacAddressDisplay
        {
            get
            {
                if (string.IsNullOrEmpty(Uuid) || !Guid.TryParse(Uuid, out Guid deviceGuid))
                    return "ID Desconhecido";
                var bytes = deviceGuid.ToByteArray();
                var macBytes = new byte[6];
                Array.Copy(bytes, 10, macBytes, 0, 6);
                return BitConverter.ToString(macBytes).Replace('-', ':');
            }
        }
    }
}