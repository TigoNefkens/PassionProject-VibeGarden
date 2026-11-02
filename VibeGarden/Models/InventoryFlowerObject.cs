using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VibeGardeb.Models
{
    public class InventoryFlowerObject
    {

        public FlowerType Flowertype { get; set;  }

        public int Quantity { get; set; }

        public InventoryFlowerObject(FlowerType Flowertype, int Quantity)
        {
            this.Flowertype = Flowertype;
            this.Quantity = Quantity;
        }

    }
}
