using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WpfOly
{
    class Noble
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Faction { get; set; }
        public IDictionary<Tuple<int,int>,Location[]> LocationHistory { get; set; }

        //some nobles are in more than one "location", i.e. some locations are in several maps, as an innerloc on one and a province in the other.
        public Location[] LastKnownLocation
        {
            get
            {
                return LocationHistory[LocationHistory.Keys.Max()];
            }
        }

        public string FormattedIdAndName
        {
            get
            {
                return Name + " [" + Id + "]";
            }
        }

        public string ExtraInfo
        {
            get
            {
                var result =
                    from keyval in LocationHistory
                    orderby keyval.Key descending
                    select string.Format("{0}: {1}", keyval.Key, keyval.Value.First().Id);
                return string.Format("{0}\n{1}", Faction, string.Join("\n", result.Take(10)));
            }
        }

        public void SetLastKnownLocation()
        {
            foreach (var loc in LastKnownLocation)
                loc.Nobles.Add(this);
        }
        
    }
}
