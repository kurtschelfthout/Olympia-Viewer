using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WpfOly
{
    class Trade
    {
        public enum TradeType
        {
            Buy, Sell
        }

        public string Who { get; set; }
        public TradeType BuySell { get; set; }
        public int Quantity { get; set; }
        public int Price { get; set; }
        public int Weight { get; set; }
        public string ItemName { get; set; }
        public string ItemId { get; set; }

        public override string ToString()
        {
            return BuySell.ToString() + " " + Quantity + " " + ItemName + " [" + ItemId + "] for " + Price + " each.";
        }

        public int TotalWeight
        {
            get
            {
                return Quantity * Weight;
            }
        }

        public int TotalWeightHorses
        {
            get
            {
                return (int) Math.Ceiling((decimal) TotalWeight / 150);
            }
        }
    }
}
