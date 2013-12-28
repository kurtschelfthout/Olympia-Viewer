using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Xml.Linq;
using System.Windows.Data;

namespace WpfOly
{
    class GatesLayer
    {
        private Map Map { get; set; }
        public ObservableCollection<Gate> Gates { get; private set; }
        //public ObservableCollection<GateHit> GateHits { get; private set; }
        ///public ObservableCollection<GateDetection> GateDetections { get; private set; }
        public ObservableCollection<object> GateHitsAndDetections { get; set; }

        public GatesLayer(Map map)
        {
            Map = map;
            var offset = Map.length / 2;
            Gates =
                new ObservableCollection<Gate>(
                from xelem in XDocument.Load("gates.xml").Descendants("gate")
                let fromLoc = xelem.Element("from").Value
                let toLoc = xelem.Element("to").Value
                let id = xelem.Element("id").Value
                where map.LocationsById.ContainsKey(fromLoc) //TODO: gate detections in cloudlands/faery etc
                where map.LocationsById.ContainsKey(toLoc) //TODO: gate detections in cloudlands/faery etc
                select new Gate
                {
                    Id = id,
                    From = fromLoc,
                    FromX = GetX(map, fromLoc, null) + offset, //no backup values - should be known
                    FromY = GetY(map, fromLoc, null) + offset,
                    To = toLoc,
                    ToX = GetX(map, toLoc, fromLoc) + offset,
                    ToY = GetY(map, toLoc, fromLoc) + offset,
                    Sealed = Boolean.Parse(xelem.Element("sealed").Value.ToLowerInvariant())
                });
            var gateHits =
                from xelem in XDocument.Load("gate-hits.xml").Descendants("gate-hit")
                let loc = xelem.Element("location").Value
                select new GateHit
                {
                    LocationId = loc,
                    LocationX = GetX(map, loc, null), //no backup values - should be known
                    LocationY = GetY(map, loc, null),
                    Origins = xelem.Descendants("origin").Select(e => e.Value)
                };
            var gateDetections  =
                from xelem in XDocument.Load("gate-distances.xml").Descendants("gate-distance")
                let loc = xelem.Element("location").Value
                where map.LocationsById.ContainsKey(loc) //TODO: gate detections in cloudlands/faery etc
                select new GateDetection
                {
                    LocationId = loc,
                    LocationX = GetX(map, loc, null), //no backup values - should be known
                    LocationY = GetY(map, loc, null),
                    Distance = Int32.Parse(xelem.Element("distance").Value)
                };
            GateHitsAndDetections =
                new ObservableCollection<object>(gateHits.Cast<object>().Concat(gateDetections));

        }

        private static int GetY(Map map, string loc, string backup)
        {
            Location value;
            if (map.LocationsById.TryGetValue(loc, out value))
                return value.Y;
            else
                return map.LocationsById[backup].Y;
        }

        private static int GetX(Map map, string loc, string backup)
        {
            Location value;
            if (map.LocationsById.TryGetValue(loc, out value))
                return value.X;
            else
                return map.LocationsById[backup].X;
        }


    }
}
