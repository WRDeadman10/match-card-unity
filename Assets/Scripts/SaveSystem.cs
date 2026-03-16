using System.IO;
using UnityEngine;

namespace VectorSandboxLab.MemoryGame
{
    public sealed class SaveSystem
    {
        private readonly string savePath = Path.Combine(Application.persistentDataPath, "memory-card-save.json");

        public void Save(GameSaveData saveData)
        {
            var json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(savePath, json);
        }

        public bool TryLoad(out GameSaveData saveData)
        {
            if (!File.Exists(savePath))
            {
                saveData = null;
                return false;
            }

            var json = File.ReadAllText(savePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                saveData = null;
                return false;
            }

            saveData = JsonUtility.FromJson<GameSaveData>(json);
            return saveData != null;
        }
    }
}
