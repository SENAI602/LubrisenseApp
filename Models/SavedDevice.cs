using System.Text.Json.Serialization;

namespace Lubrisense.Models
{
    public class SavedDevice
    {
        public string Uuid { get; set; }

        public string Tag { get; set; }

        public string Equipamento { get; set; }

        public string Setor { get; set; }

        public string Lubrificante { get; set; }

        public double Volume { get; set; } // Volume em litros

        public int Intervalo { get; set; } // Intervalo em horas

        // Data e hora da última conexão (ex: 2025-06-23T11:44:00)
        public DateTime UltimaConexao { get; set; }

        [JsonIgnore] // Se quiser ignorar isso ao salvar como JSON
        public string MacAddressDisplay
        {
            get
            {
                if (!Guid.TryParse(Uuid, out Guid deviceGuid))
                    return "ID Inválido";
                var bytes = deviceGuid.ToByteArray();
                var macBytes = new byte[6];
                Array.Copy(bytes, 10, macBytes, 0, 6);
                Array.Reverse(macBytes);
                return BitConverter.ToString(macBytes).Replace('-', ':');
            }
        }
    }
}


/*
 {
  "config": [
    {
      "tag": "tag_10010111",
      "equipamento": "Motor caldeira 1",
      "setor": "Planta 1",
      "lubrificante": "Ecolub Food Grade",
      "volume": 10,
      "intervalo": 5
    }
  ]
}
  
 
 */