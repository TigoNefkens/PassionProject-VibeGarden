
using System;
using VibeGardeb.Models;

namespace VibeGardeb.Services
{
    public class FlowerMoneyService
    {
        public bool IsReadyToCollect(Flower flower)
        {
            if (flower.WateredAt == null)
                return false;

            TimeSpan elapsed = DateTime.UtcNow - flower.WateredAt.Value;
            TimeSpan growthTime = TimeSpan.FromMinutes(flower.Type.GrowthTime);

            return elapsed >= growthTime;
        }

        public TimeSpan GetRemainingTime(Flower flower)
        {
            if (flower.WateredAt == null)
                return TimeSpan.Zero;

            TimeSpan elapsed = DateTime.UtcNow - flower.WateredAt.Value;
            TimeSpan growthTime = TimeSpan.FromMinutes(flower.Type.GrowthTime);

            TimeSpan remaining = growthTime - elapsed;

            return remaining > TimeSpan.Zero
                ? remaining
                : TimeSpan.Zero;
        }

        public void WaterFlower(Flower flower)
        {
            if (flower.WateredAt != null)
                return;

            flower.WateredAt = DateTime.UtcNow;
        }

        public int CollectFlower(Flower flower)
        {
            if (!IsReadyToCollect(flower))
                return 0;

            int money = flower.MoneyGain;

            flower.WateredAt = null;

            return money;
        }
    }
}
