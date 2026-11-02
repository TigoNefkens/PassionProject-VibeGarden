
using System;

namespace VibeGardeb.Models
{
    public class Flower
    {
        public FlowerType Type { get; set; }
        public FlowerPosition Position { get; set; }
        public DateTime? WateredAt { get; set; }

        public int MoneyGain { get; set; }

        public Flower(FlowerType type, FlowerPosition position)
        {
            Type = type;
            Position = position;
            WateredAt = null;
            MoneyGain = type.MoneyGain;
        }
    }
}