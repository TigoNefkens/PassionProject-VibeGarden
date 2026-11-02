using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VibeGardeb.Models;

namespace VibeGardeb.Services
{
    public static class PackProvider
    {
        public static readonly FlowerPack BasicPack = new()
        {
            Name = "Rookie Pack",
            Price = 100,
            Png = "basicpack.png",

            RarityWeights = new()
            {
                { FlowerRarity.Common, 80 },
                { FlowerRarity.Uncommon, 20 },
               
            }
        };

        public static readonly FlowerPack BetterPack = new()
        {
            Name = "Basic pack",
            Price = 200,
            Png = "betterpack.png",

            RarityWeights = new()
            {
                { FlowerRarity.Common, 50 },
                { FlowerRarity.Uncommon, 40 },
                { FlowerRarity.Rare, 10 },
            }
        };

        public static readonly List<FlowerPack> AllPacks = new()
        {
            BasicPack,
            BetterPack,
        };
    }
}
