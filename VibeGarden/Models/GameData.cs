namespace VibeGardeb.Models
{
    public class GameData
    {
        public List<Flower> FlowerPotContent { get; set; } = new();

        public List<InventoryFlowerObject> SeedInventory { get; set; } = new();

        public bool IsFirstLaunch { get; set; } = true;

        public int Coins { get; set; } = new();
    }
}