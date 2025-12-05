using System.Text.Json.Serialization;
using Microsoft.Maui.Graphics;

namespace Lubrisense.Models
{
    public class LogEvent
    {
        // Propriedades que vêm do JSON do ESP32
        public string Hora { get; set; }       // Ex: "2025-12-05T14:30:00"
        public bool Sucesso { get; set; }      // true/false
        public string Modo { get; set; }       // "Manual" ou "Auto"
        public double Temperatura { get; set; }
        public int Bateria { get; set; }

        [JsonPropertyName("e")]
        public bool EnviadoGateway { get; set; } // true/false

        // --- Propriedades Visuais (Calculadas) ---

        [JsonIgnore]
        public string HoraFormatada
        {
            get
            {
                if (DateTime.TryParse(Hora, out DateTime dt))
                {
                    return dt.ToString("dd/MM/yy HH:mm");
                }
                return Hora;
            }
        }

        [JsonIgnore]
        public string IconeSucesso => Sucesso ? "✅" : "❌";

        [JsonIgnore]
        public string StatusTexto => Sucesso ? "Sucesso" : "Falha";

        [JsonIgnore]
        public Color StatusCor => Sucesso ? Colors.Green : Colors.Red;

        [JsonIgnore]
        public string IconeModo => Modo == "Manual" ? "👤 Manual" : "🤖 Auto";

        [JsonIgnore]
        public string TextoBateria => $"🔋 {Bateria}%";

        [JsonIgnore]
        public string TextoTemperatura => $"🌡️ {Temperatura:F1}°C";

        [JsonIgnore]
        public string IconeGateway => EnviadoGateway ? "📡 Enviado" : "⏳ Pendente";

        [JsonIgnore]
        public Color GatewayCor => EnviadoGateway ? Colors.Blue : Colors.Orange;
    }
}