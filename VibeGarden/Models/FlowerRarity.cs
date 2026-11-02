using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VibeGardeb.Models
{
    public enum FlowerRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Mythic
    }

    public static class RarityHelper
    {
        public static Color GetColor(FlowerRarity rarity)
        {
            return rarity switch
            {
                FlowerRarity.Common => Color.FromArgb("FFFFFF"),
                FlowerRarity.Uncommon => Color.FromArgb("#62FF52"),
                FlowerRarity.Rare => Color.FromArgb("#52D4FF"),
                FlowerRarity.Epic => Color.FromArgb("#C852FF"),
                FlowerRarity.Legendary => Color.FromArgb("#FFDC52"),
                FlowerRarity.Mythic => Color.FromArgb("#FF5252"),
                _ => Color.FromArgb("FFFFFF")
            };
        }
    }
}
