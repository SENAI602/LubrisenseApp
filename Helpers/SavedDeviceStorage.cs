using Lubrisense.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Lubrisense.Helpers
{
    public static class SavedDeviceStorage
    {
        private const string StorageKey = "DispositivosSalvos";

        // Carrega a lista (Mantido igual ao original)
        public static List<SavedDevice> Load()
        {
            var json = Preferences.Get(StorageKey, "");
            return string.IsNullOrWhiteSpace(json)
                ? new List<SavedDevice>()
                : JsonSerializer.Deserialize<List<SavedDevice>>(json) ?? new List<SavedDevice>();
        }

        // --- CORREÇÃO 1: Adicionado o método GetById que o ViewModel pede ---
        public static SavedDevice? GetById(string uuid)
        {
            var list = Load();
            return list.FirstOrDefault(d => d.Uuid.Equals(uuid, StringComparison.OrdinalIgnoreCase));
        }

        // (Mantido para compatibilidade com outras partes do código, se houver)
        public static SavedDevice? GetByUuid(string uuid) => GetById(uuid);

        public static void Save(List<SavedDevice> devices)
        {
            var json = JsonSerializer.Serialize(devices);
            Preferences.Set(StorageKey, json);
        }

        // --- CORREÇÃO 2: Atualiza TODOS os campos ao editar ---
        public static void AddOrUpdate(SavedDevice device)
        {
            var list = Load();
            var existing = list.FirstOrDefault(d => d.Uuid.Equals(device.Uuid, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                // Atualiza todos os campos editáveis
                existing.Tag = device.Tag;
                existing.Equipamento = device.Equipamento;
                existing.Setor = device.Setor;
                existing.Lubrificante = device.Lubrificante;
                existing.UltimaConexao = DateTime.Now; // Atualiza horário
            }
            else
            {
                device.UltimaConexao = DateTime.Now;
                list.Add(device);
            }

            Save(list);
        }

        // Remove da lista
        public static void Remove(string uuid)
        {
            var list = Load();
            // Remove qualquer item que tenha esse ID
            list.RemoveAll(d => d.Uuid.Equals(uuid, StringComparison.OrdinalIgnoreCase));
            Save(list);
        }

        public static void Clear()
        {
            Preferences.Remove(StorageKey);
        }
    }
}