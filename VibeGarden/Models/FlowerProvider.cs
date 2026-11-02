using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VibeGardeb.Models
{
    public static class FlowerProvider
    {
        public static readonly FlowerType Sunflower = new()
        {
            Name = "Sunflower",
            MoneyGain = 50,
            Sprite = "sunflower.png",
            SeedSprite = "sunflower.png",
            GrowthTime = 120,
            Rarity = FlowerRarity.Common,
        };

        public static readonly FlowerType Tulip = new()
        {
            Name = "Tulip",
            MoneyGain = 75,
            Sprite = "tulip.png",
            SeedSprite = "tulip.png",
            GrowthTime = 180,
            Rarity = FlowerRarity.Common,
        };

        public static readonly FlowerType Daisy = new()
        {
            Name = "Daisy",
            MoneyGain = 100,
            Sprite = "daisy.png",
            SeedSprite = "daisy.png",
            GrowthTime = 240,
            Rarity = FlowerRarity.Common,
        };

        public static readonly FlowerType Fourleafclover = new()
        {
            Name = "Four-leaf clover",
            MoneyGain = 25,
            Sprite = "fourleafclover.png",
            SeedSprite = "fourleafclover.png",
            GrowthTime = 60,
            Rarity = FlowerRarity.Common,
        };


        public static readonly FlowerType Hibiscus = new()
        {
            Name = "Hibiscus",
            MoneyGain = 180,
            Sprite = "hibiscus.png",
            SeedSprite = "hibiscus.png",
            GrowthTime = 180,
            Rarity = FlowerRarity.Uncommon,
        };

        public static readonly FlowerType Rose = new()
        {
            Name = "Rose",
            MoneyGain = 100,
            Sprite = "Rose.png",
            SeedSprite = "Rose.png",
            GrowthTime = 90,
            Rarity = FlowerRarity.Uncommon,
        };

        public static readonly FlowerType Peony = new()
        {
            Name = "Peony",
            MoneyGain = 140,
            Sprite = "peony.png",
            SeedSprite = "peony.png",
            GrowthTime = 120,
            Rarity = FlowerRarity.Uncommon,
        };

        public static readonly FlowerType Chrysanthemum = new()
        {
            Name = "Chrysanth",
            MoneyGain = 170,
            Sprite = "Chrysanthemum.png",
            SeedSprite = "Chrysanthemum.png",
            GrowthTime = 150,
            Rarity = FlowerRarity.Uncommon,
        };


        public static readonly FlowerType Lavender = new()
        {
            Name = "Lavender",
            MoneyGain = 80,
            Sprite = "lavender.png",
            SeedSprite = "lavender.png",
            GrowthTime = 60,
            Rarity = FlowerRarity.Uncommon,
        };


        public static readonly FlowerType Goldenmarigold = new()
        {
            Name = "The golden marigold",
            MoneyGain = 250,
            Sprite = "goldenmarigold.png",
            SeedSprite = "goldenmarigold.png",
            GrowthTime = 120,
            Rarity = FlowerRarity.Rare,
        };


        public static readonly List<FlowerType> AllFlowers = new()
        {
            Sunflower,
            Tulip,
            Lavender,
            Daisy,
            Fourleafclover,
            Hibiscus,
            Rose,
            Peony,
            Chrysanthemum,
            Goldenmarigold,
        };

    }
}
