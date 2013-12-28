using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace WpfOly
{
    class TradeRoutesLayer
    {
        private Map map;

        public ObservableCollection<TradeRoute> TradeRoutes { get; private set; }

        public TradeRoutesLayer(Map map)
        {
            this.map = map;
            var cities = from loc in map.LocationsById.Values
                         where loc is City
                         select loc as City;
            var trades = from city in cities
                         from trade in city.Trades
                         where trade.ItemId.Length > 3 //only keep the actual routes, not the crap like woven baskets etc
                         && trade.Who.Length == 3 //only keep buy/sells from cities
                         select trade;
            var result = (from trade in trades
                         group trade by trade.ItemId into route
                         where route.Count() == 2
                         select route).ToDictionary( r => r.Key, r => ToTradeRoute(map, r.ToArray()));
            TradeRoutes = new ObservableCollection<TradeRoute>(result.Values);
        }

        private Random random = new Random();

        private TradeRoute ToTradeRoute(Map map, Trade[] trade)
        {
            Trade buy;
            Trade sell;
            if (trade[0].BuySell == Trade.TradeType.Buy)
            {
                buy = trade[0];
                sell = trade[1];
            }
            else
            {
                buy = trade[1];
                sell = trade[0];
            }
            var offset = Map.length / 2;
            var dist = offset / 2;
            var fromX = map.LocationsById[sell.Who].X;
            var fromY = map.LocationsById[sell.Who].Y;
            var toX = map.LocationsById[buy.Who].X;
            var toY = map.LocationsById[buy.Who].Y;
            var signX = Math.Sign(fromX - toX);
            var signY = Math.Sign(fromY - toY);
            var result = new TradeRoute
            {
                From = sell.Who,
                FromX = fromX + offset + dist * signX,
                FromY = fromY + offset + dist * signY,
                To = buy.Who,
                ToX = toX + offset + dist * signX,
                ToY = toY + offset + dist * signY,
                SoldTradeGood = sell,
                BoughtTradeGood = buy
            };
            return result;
        }
    }
}
