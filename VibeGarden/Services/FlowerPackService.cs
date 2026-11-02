using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VibeGardeb.Models;

namespace VibeGardeb.Services
{
    public class FlowerPackService
    {
        private readonly Random _random = new();

        public FlowerRarity GetRandomRarity(FlowerPack pack)
        {
            double totalWeight = pack.RarityWeights.Values.Sum();

            double roll = _random.NextDouble() * totalWeight;

            double currentWeight = 0;

            foreach (var rarity in pack.RarityWeights)
            {
                currentWeight += rarity.Value;

                if (roll < currentWeight)
                    return rarity.Key;
            }

            return pack.RarityWeights.Keys.Last();
        }

        public FlowerType GetRandomFlower(FlowerPack pack)
        {
            FlowerRarity rarity = GetRandomRarity(pack);

            var flowers = FlowerProvider.AllFlowers
                .Where(x => x.Rarity == rarity)
                .ToList();

            if (flowers.Count == 0)
                throw new InvalidOperationException(
                    $"There are no flowers with rarity {rarity}."
                );

            int index = _random.Next(flowers.Count);

            return flowers[index];
        }

        public InventoryFlowerObject OpenPack(FlowerPack pack)
        {
            FlowerType flower = GetRandomFlower(pack);

            return new InventoryFlowerObject(flower, 1);
        }
    }
}
