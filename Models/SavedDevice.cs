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

        // --- Configurações (Usamos int para garantir compatibilidade JSON com o ESP32) ---
        public int TipoConfig { get; set; }     // 0=Basico, 1=Avancado
        public int TipoIntervalo { get; set; }  // 1=Hora, 2=Dia, 3=Mês
        public int Volume { get; set; }         // Gramas
        public int Intervalo { get; set; }      // Valor do intervalo

        // --- Novos Campos para o Firmware Híbrido ---
        public int Frequencia { get; set; } = 1;
        public int TipoFrequencia { get; set; } = 2; // 2=Dia

        public DateTime UltimaConexao { get; set; }

        // --- Propriedade Visual (Não é salva no JSON do ESP32) ---
        [JsonIgnore]
        public bool IsOnline { get; set; }

        // Construtor vazio necessário para deserialização
        public SavedDevice() { }

        public SavedDevice(string uuid)
        {
            Uuid = uuid;
            Tag = string.Empty;
            Equipamento = string.Empty;
            Setor = string.Empty;
            Lubrificante = string.Empty;

            // Defaults compatíveis com os Enums do App
            TipoConfig = (int)CONFIGTYPE.BASICO;
            TipoIntervalo = (int)INTERVALTYPE.MES;

            Volume = 0;
            Intervalo = 0;
            UltimaConexao = DateTime.Now;
            IsOnline = false;
        }

        // Mantido do seu código original (Formatação bonita do MAC)
        [JsonIgnore]
        public string MacAddressDisplay
        {
            get
            {
                if (string.IsNullOrEmpty(Uuid) || !Guid.TryParse(Uuid, out Guid deviceGuid))
                    return "ID Desconhecido";

                var bytes = deviceGuid.ToByteArray();
                var macBytes = new byte[6];
                // O UUID Bluetooth é longo, pegamos os bytes que representam o MAC (geralmente o final)
                // Nota: Isso é uma aproximação visual para o usuário identificar o dispositivo
                Array.Copy(bytes, 10, macBytes, 0, 6);
                return BitConverter.ToString(macBytes).Replace('-', ':');
            }
        }
    }
}