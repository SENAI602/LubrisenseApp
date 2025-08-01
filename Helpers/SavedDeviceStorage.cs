using Lubrisense.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Lubrisense.Helpers
{
    public static class SavedDeviceStorage
    {
        private const string StorageKey = "DispositivosSalvos";

        public static List<SavedDevice> Load()
        {
            var json = Preferences.Get(StorageKey, "");
            return string.IsNullOrWhiteSpace(json)
                ? new List<SavedDevice>()
                : JsonSerializer.Deserialize<List<SavedDevice>>(json) ?? new List<SavedDevice>();
        }

        public static SavedDevice? GetByUuid(string uuid)
        {
            var list = Load();
            return list.FirstOrDefault(d => d.Uuid.Equals(uuid, StringComparison.OrdinalIgnoreCase));
        }

        public static void Save(List<SavedDevice> devices)
        {
            var json = JsonSerializer.Serialize(devices);
            Preferences.Set(StorageKey, json);
        }

        public static void AddOrUpdate(SavedDevice device)
        {
            var list = Load();
            var existing = list.FirstOrDefault(d => d.Uuid.Equals(device.Uuid, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                existing.Equipamento = device.Equipamento;
                existing.UltimaConexao = device.UltimaConexao;
            }
            else
            {
                list.Add(device);
            }

            Save(list);
        }

        public static void Remove(string Uuid)
        {
            var list = Load();
            list.RemoveAll(d => d.Uuid.Equals(Uuid, StringComparison.OrdinalIgnoreCase));
            Save(list);
        }

        public static void Clear()
        {
            Preferences.Remove(StorageKey);
        }
    }
}