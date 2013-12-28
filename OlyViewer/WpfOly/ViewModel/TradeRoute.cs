using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WpfOly
{
    class TradeRoute
    {
        public Trade SoldTradeGood { get; set; }
        public Trade BoughtTradeGood { get; set; }
        public string From { get; set; }
        public int FromX { get; set; }
        public int FromY { get; set; }

        public string To { get; set; }
        public int ToX { get; set; }
        public int ToY { get; set; }

        public string Info
        {
            get
            {
                var buyLoad = BoughtTradeGood.Quantity * BoughtTradeGood.Price;
                var sellLoad = SoldTradeGood.Quantity * SoldTradeGood.Price;
                return SoldTradeGood.ItemName + " [" + SoldTradeGood.ItemId + "]. Weight/full load: " 
                    + SoldTradeGood.TotalWeight + " (" + SoldTradeGood.TotalWeightHorses + " horses)"
                    + "\nProfit = " + buyLoad + " - " + sellLoad + " = " + (buyLoad - sellLoad);
            }
        }
    }
}
