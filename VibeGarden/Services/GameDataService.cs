using System.Text.Json;
using VibeGardeb.Models;

namespace VibeGardeb.Services
{
    public class GameDataService
    {
        private readonly string savePath;

        public GameDataService()
        {
            savePath = Path.Combine(
                FileSystem.AppDataDirectory,
                "gamedata.json");
        }


        public async Task SaveGameDataAsync(GameData gameData)
        {
            string json = JsonSerializer.Serialize(
                gameData,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            await File.WriteAllTextAsync(savePath, json);
        }


        public async Task<GameData> LoadGameDataAsync()
        {
            if (!File.Exists(savePath))
            {
                return new GameData();
            }

            string json = await File.ReadAllTextAsync(savePath);

            return JsonSerializer.Deserialize<GameData>(json)
                   ?? new GameData();
        }
    }
}