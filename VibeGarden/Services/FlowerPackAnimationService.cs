using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VibeGardeb.Models;

namespace VibeGardeb.Services
{
    class FlowerPackAnimationService
    {
        private readonly Random random = new();

        public List<FlowerType> CreateRoll(FlowerPack pack, FlowerType winningFlower, int amountOfFlowers = 60)
        {
            
            if (pack == null) 
                throw new ArgumentNullException(nameof(winningFlower));

            if (amountOfFlowers < 2) 
                throw new ArgumentException("amountOfFlowers must be at least 2.", nameof(amountOfFlowers));

            HashSet<FlowerRarity> availableRarities = pack.RarityWeights.Keys.ToHashSet();

            List<FlowerType> availableFlowers = FlowerProvider.AllFlowers.Where(flower => availableRarities.Contains(flower.Rarity)).ToList();

            if (availableFlowers.Count == 0) 
                throw new InvalidOperationException("No flowers are available for the rarities in this pack.");

            List<FlowerType> roll = new(amountOfFlowers);

            for (int i = 0; i < amountOfFlowers; i++) {
                FlowerType randomFlower = availableFlowers[random.Next(availableFlowers.Count)]; 
                roll.Add(randomFlower); 
            }
            int winningIndex = amountOfFlowers - 8;

            roll[winningIndex] = winningFlower;

            return roll;
        }

    }
}
