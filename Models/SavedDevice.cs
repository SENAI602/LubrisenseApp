using System;
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

        // --- Configurações Originais ---
        public int TipoConfig { get; set; }     // 0=Basico, 1=Avancado
        public int TipoIntervalo { get; set; }  // 1=Hora, 2=Dia, 3=Mês
        public int Volume { get; set; }         // Gramas
        public int Intervalo { get; set; }      // Valor do intervalo

        // --- NOVOS CAMPOS PARA O FIRMWARE HÍBRIDO ---
        public int Frequencia { get; set; } = 1;     // Ex: 1 vez
        public int TipoFrequencia { get; set; } = 2; // Ex: por Dia (2)

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

            // Defaults
            TipoConfig = (int)CONFIGTYPE.BASICO;
            TipoIntervalo = (int)INTERVALTYPE.MES;

            Volume = 0;
            Intervalo = 0;
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