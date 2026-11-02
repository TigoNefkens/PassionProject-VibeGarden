using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VibeGardeb.Models
{
    public class FlowerPack
    {
        public string Name { get; set; }
        public int Price { get; set; }

        public string Png { get; set; }

        public Dictionary<FlowerRarity, double> RarityWeights { get; set; }
    }
}
