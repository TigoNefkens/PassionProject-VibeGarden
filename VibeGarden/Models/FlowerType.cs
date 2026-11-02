using VibeGardeb.Models;

public class FlowerType
{
    public string Name { get; set; }
    public string Sprite { get; set; }
    public string SeedSprite { get; set; }

    public int GrowthTime { get; set; }
    public int MoneyGain { get; set; }

    public FlowerRarity Rarity { get; set; }

}