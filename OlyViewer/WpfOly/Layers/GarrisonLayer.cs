using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Xml.Linq;

namespace WpfOly
{
    class GarrisonLayer
    {
        private Map Map { get; set; }

        public ObservableCollection<Garrison> Garrisons { get; private set; }

        public GarrisonLayer(Map map)
        {
            Map = map;

            Garrisons = new ObservableCollection<Garrison>(
                from xelem in XDocument.Load("garrisons.xml").Descendants("garrison")
                let id = xelem.Element("id").Value
                where xelem.Element("location-id") != null //some garrisons' location can not be determined
                let locationId = xelem.Element("location-id").Value
                where map.LocationsById.ContainsKey(locationId) //garrisons can be in faery etc
                select new Garrison
                {
                    Id = id,
                    LocationId = locationId,
                    CastleId = xelem.Element("castle-id").Value,
                    GarrisonX = map.LocationsById[locationId].X,
                    GarrisonY = map.LocationsById[locationId].Y + Map.length / 2,
                    Inventory = MakeInventory(xelem)
                }); 
        }

        private IDictionary<string, int> MakeInventory(XElement xelem)
        {
            return (from item in xelem.Descendants("inventory")
                    where item.HasAttributes
                    select new
                             {
                                 Key = item.Attribute("key").Value,
                                 Val = Int32.Parse(item.Value)
                             }).ToDictionary(v => v.Key, v => v.Val);

        }
    }
}
