using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections;
using System.Collections.Specialized;
using System.Xml.Linq;
using System.Linq;
using System.Text.RegularExpressions;

namespace WpfOly
{
    class Map : 
        IList,
        ZoomableCanvas.ISpatialItemsSource
    {

        public enum Region
        {
            Provinia,
            Undercity,
            Hades,
            Faery,
            Cloud,
        }

        private class TempProvince
        {
            public string Id { get; set; }
            public string North { get; set; }
            public string South { get; set; }
            public string West { get; set; }
            public string East { get; set; }
            public string Up { get; set; }
            public bool IsSeed { get; set; }
            public int X { get; set; }
            public int Y { get; set; }

            public string Down { get; set; }
        }

        public static readonly int length = 36 * 4;
        public static readonly double lengthDouble = (double)length;
        public int width = 80;
        public int height = 80;

        private Province[] provinces = new Province[0];

        public IDictionary<string,Location> LocationsById { get; private set; }

        public IDictionary<string, Tuple<int, bool>> ExploresByLocation { get; private set; }

        public string[] SearchableStrings { get; private set; }

        public Map(Region region)
        {
            ExploresByLocation = 
                XDocument.Load("explores.xml")
                .Descendants("explore")
                .ToDictionary(xelem => xelem.Element("location").Value, 
                              xelem => new Tuple<int,bool>(Int32.Parse(xelem.Element("failures").Value),
                                  Boolean.Parse(xelem.Element("hidden").Value.ToLowerInvariant())));

            var doc = XDocument.Load("locations.xml");

            var provinceXml = doc.Descendants("location").ToDictionary(xelem => xelem.Element("id").Value);

            if (region == Region.Provinia)
            {
                MakeProvinia(provinceXml);
            }
            else if (region == Region.Undercity)
            {
                MakeTunnels(provinceXml);
            }
            else if (region == Region.Faery)
            {
                MakeRegionKnownSize(provinceXml, "Faery", 1000, 7);
            }
            else if (region == Region.Hades)
            {
                MakeRegionKnownSize(provinceXml, "Hades", 75, 75);
            }
            else if (region == Region.Cloud)
            {
                MakeRegionKnownSize(provinceXml, "Cloud", 5, 5);
            }

            BuildLocationsById();

            SearchableStrings = 
                LocationsById.Keys.ToArray()
                .Concat(LocationsById.Values.Select(p => p.Name).Distinct())
                .ToArray();
        }

        

        private void MakeProvinia(Dictionary<string, XElement> provinceXml)
        {
            var notUsed = new[] { 'e', 'i', 'l', 'o', 'u', 'y' };
            var rows = from outer in Enumerable.Range(Convert.ToInt32('a'), 6)
                           .Select(Convert.ToChar)
                           .Where(c => !notUsed.Contains(c))
                       from inner in Enumerable.Range(Convert.ToInt32('a'), 26)
                            .Select(Convert.ToChar)
                            .Where(c => !notUsed.Contains(c))
                       select new String(new[] { outer, inner });
            var rowsArr = rows.ToArray();

            provinces = (from r in Enumerable.Range(0, height)
                         from c in Enumerable.Range(0, width)
                         select MakeProvince(provinceXml, rowsArr[r] + String.Format("{0:00}", c), r, c)).ToArray();
        }

        private void MakeRegionKnownSize(Dictionary<string, XElement> provinceXml, string region, int xSize, int ySize)
        {
            // these are just the visited ones - for the neightboring ones we don't have a region. 
            var todo = (from province in provinceXml.Values
                        where province.Element("region").Value == region || province.Element("name").Value == region //second is for Hades
                        let routes = province.Descendants("route")
                        select new TempProvince
                        {
                            Id = province.Element("id").Value,
                            North = GetDirection(routes, "North"),
                            South = GetDirection(routes, "South"),
                            East = GetDirection(routes, "East"),
                            West = GetDirection(routes, "West"),
                            IsSeed = province.Descendants("visit").Any() //we'll start to look from these preferentially, so that if a route is not bi
                            //directional as in Hades, we start from the best explored provinces. Otherwise pieces mights show as disconnected that reallyl aren't
                        })
                        .ToDictionary(k => k.Id);

            // now, for each of those provinces add the n,s,e,w routes that we know of
            var copy = new List<TempProvince>(todo.Values);
            foreach (var vis in copy)
            {
                if (vis.North != null && !todo.ContainsKey(vis.North))
                {
                    var province = provinceXml[vis.North];
                    var routes = province.Descendants("route");
                    todo.Add(vis.North, new TempProvince
                           {
                               Id = province.Element("id").Value,
                               North = GetDirection(routes, "North"),
                               South = GetDirection(routes, "South"),
                               East = GetDirection(routes, "East"),
                               West = GetDirection(routes, "West"),
                           });
                }
                if (vis.South != null && !todo.ContainsKey(vis.South))
                {
                    var province = provinceXml[vis.South];
                    var routes = province.Descendants("route");
                    todo.Add(vis.South,new TempProvince
                    {
                        Id = province.Element("id").Value,
                        North = GetDirection(routes, "North"),
                        South = GetDirection(routes, "South"),
                        East = GetDirection(routes, "East"),
                        West = GetDirection(routes, "West"),
                    });
                }
                if (vis.West != null && !todo.ContainsKey(vis.West))
                {
                    var province = provinceXml[vis.West];
                    var routes = province.Descendants("route");
                    todo.Add(vis.West,new TempProvince
                    {
                        Id = province.Element("id").Value,
                        North = GetDirection(routes, "North"),
                        South = GetDirection(routes, "South"),
                        East = GetDirection(routes, "East"),
                        West = GetDirection(routes, "West"),
                    });
                }
                if (vis.East != null && !todo.ContainsKey(vis.East))
                {
                    var province = provinceXml[vis.East];
                    var routes = province.Descendants("route");
                    todo.Add(vis.East,new TempProvince
                    {
                        Id = province.Element("id").Value,
                        North = GetDirection(routes, "North"),
                        South = GetDirection(routes, "South"),
                        East = GetDirection(routes, "East"),
                        West = GetDirection(routes, "West"),
                    });
                }
            }

                         

            if (!todo.Any())
                return;

            //starting points
            var seeds = todo.OrderBy(v => v.Value.IsSeed ? 0 : 1).Select( v => v.Key).ToArray(); //we want the seeds first
            var allFaery = new Dictionary<string, TempProvince>(todo);

            //first find all the connected provinces to the first seed, removing seeds 
            var connectedSetsPerSeed = new Dictionary<string, List<TempProvince>>(); // the set at index i contains the id of the sewer of the tunnels originating there.
            //while (todo.Any())
            foreach (var seed in seeds)
            {
                //var seed = todo.First().Key;
                if (todo.ContainsKey(seed))
                {
                    var connected = new List<TempProvince>();
                    MakeConnectedSet(todo[seed], todo, connected);
                    connectedSetsPerSeed.Add(seed, connected);
                }
            }


            //finally, lay them out per set left to right, level 0 -> n
            int startX = 0, startY = 0, bottomMost = 0;
            foreach (var connectedSet in connectedSetsPerSeed.Values)
            {
                var leftMost = connectedSet.Min(tp => tp.X);
                var topMost = connectedSet.Min(tp => tp.Y);

                foreach (var t in connectedSet)
                {
                    t.X += (-leftMost + startX);
                    t.Y += (-topMost + startY);
                    t.Y %= ySize; //faery is max 7 provinces high
                    t.X %= xSize;
                }

                //put all disconnected parts next to one another for now
                var rightMost = connectedSet.Max(tp => tp.X);
                bottomMost = Math.Max(bottomMost, connectedSet.Max(tp => tp.Y));
                startX = (rightMost + 2);

                //wrap
                //if (startX > 20)
                //{
                //    startX = 0;
                //    startY = bottomMost + 2;
                //}
            }
            
                
            //}
            var faeryProvinces = allFaery.Values;
            var right = faeryProvinces.Max(tp => tp.X);
            var bottom = faeryProvinces.Max(tp => tp.Y);
            width = right + 1;
            height = bottom + 1;
            provinces = new Province[width * height];
            for (int c = 0; c < width; c++)
                for (int r = 0; r < height; r++)
                {
                    var found = faeryProvinces.FirstOrDefault(tun => tun.X == c && tun.Y == r);
                    if ((found != null))
                    {
                        provinces[Index(r, c)] = MakeProvince(provinceXml, found.Id, r, c);
                    }
                    else
                    {
                        provinces[Index(r, c)] = new UndergroundProvince
                        {
                            Id = "",
                            Name = "",
                            Height = length,
                            Width = length,
                            X = c * length,
                            Y = r * length
                        };
                    }
                }
        }
        


        private void MakeTunnels(Dictionary<string, XElement> provinceXml)
        {
            var todo = (from tunnel in provinceXml.Values
                           where tunnel.Element("type").Value == "tunnel" || tunnel.Element("type").Value == "chamber"
                           let routes = tunnel.Descendants("route")
                           select new TempProvince
                           {
                               Id = tunnel.Element("id").Value,
                               North = GetDirection(routes, "North"),
                               South = GetDirection(routes, "South"),
                               East = GetDirection(routes, "East"),
                               West = GetDirection(routes, "West"),
                               Up = GetDirection(routes, "Up"),
                               Down = GetDirection(routes, "Down"),
                               IsSeed = GetDirection(routes, "Up") != null
                           })
                          .ToDictionary(k => k.Id);

            if (!todo.Any())
                return;

            //starting points for each level
            var seeds = todo.Values.Where(t => t.IsSeed).Select(t => t.Id).ToArray();
            var tunnelDict = new Dictionary<string, TempProvince>(todo);

            //first find all the connected sets, i.e. levels, per seed, i.e. tunnel where you first enter that level
            var connectedSetsPerSeed = new Dictionary<string, List<TempProvince>>(); // the set at index i contains the id of the sewer of the tunnels originating there.
            foreach (var seed in seeds)
            {
                var connected = new List<TempProvince>();
                MakeConnectedSet(todo[seed], todo, connected);
                connectedSetsPerSeed.Add(seed, connected);
            }

            //now organize the connected sets according to the sewer they originate from (the string key), and level (index in the first list)
            var tunnelSets = new Dictionary<string, List<List<TempProvince>>>();
            foreach (var seed in seeds.Where(s => new Regex(@"\w\d\d\d").IsMatch(tunnelDict[s].Up))) //get all the sewers - basically entry to level 0
            {
                var levelsPerSewer = new List<List<TempProvince>>();
                levelsPerSewer.Add(connectedSetsPerSeed[seed]);
                string nextDownId = seed;
                List<TempProvince> nextDown = null;
                while (FindLevelDown(connectedSetsPerSeed[nextDownId], out nextDownId) && connectedSetsPerSeed.TryGetValue(nextDownId, out nextDown))
                {
                    //ok, we've got the next set
                    levelsPerSewer.Add(nextDown);
                }
                //levelsPerSewer is now filled up with levels from 0 -> n
                tunnelSets.Add(seed, levelsPerSewer);
            }

            //finally, lay them out per set left to right, level 0 -> n
            int startX = 0, startY = 0, bottomMost = 0;
            foreach (var sewer in tunnelSets.Keys)
            {
                foreach (var level in tunnelSets[sewer])
                {
                    var leftMost = level.Min(tp => tp.X);
                    var topMost = level.Min(tp => tp.Y);

                    foreach (var t in level)
                    {
                        t.X += (-leftMost + startX);
                        t.Y += (-topMost + startY);
                    }

                    //put all disconnected parts next to one another for now
                    var rightMost = level.Max(tp => tp.X);
                    bottomMost = Math.Max(bottomMost, level.Max(tp => tp.Y));
                    startX = (rightMost + 2);
                }   
                //wrap
                startX = 0;
                startY = bottomMost + 2;
            }
            
                
            //}
            var tunnels = tunnelDict.Values;
            var right = tunnels.Max(tp => tp.X);
            var bottom = tunnels.Max(tp => tp.Y);
            width = right + 1;
            height = bottom + 1;
            provinces = new Province[width * height];
            for (int c = 0; c < width; c++)
                for (int r = 0; r < height; r++)
                {
                    var found = tunnels.FirstOrDefault(tun => tun.X == c && tun.Y == r);
                    if ((found != null))
                    {
                        provinces[Index(r, c)] = MakeProvince(provinceXml, found.Id, r, c);
                    }
                    else
                    {
                        provinces[Index(r, c)] = new UndergroundProvince
                        {
                            Id = "",
                            Name = "",
                            Height = length,
                            Width = length,
                            X = c * length,
                            Y = r * length
                        };
                    }
                }
        }

        private bool FindLevelDown(List<TempProvince> newLevel, out string seedDown)
        {
            seedDown = (from province in newLevel
                       where province.Down != null
                       select province.Down).SingleOrDefault();
            return seedDown != null;

        }

        private void MakeConnectedSet(TempProvince start, IDictionary<string, TempProvince> todo, IList<TempProvince> result)
        {
            if ((start.North != null) && todo.ContainsKey(start.North))
            {
                var doing = todo[start.North];
                doing.X = start.X;
                doing.Y = start.Y - 1;
                todo.Remove(start.North);
                result.Add(doing);
                MakeConnectedSet(doing, todo, result);
            }
            if ((start.South != null) && todo.ContainsKey(start.South))
            {
                var doing = todo[start.South];
                doing.X = start.X;
                doing.Y = start.Y + 1;
                todo.Remove(start.South);
                result.Add(doing);
                MakeConnectedSet(doing, todo, result);
            }
            if ((start.West != null) && todo.ContainsKey(start.West))
            {
                var doing = todo[start.West];
                doing.X = start.X - 1;
                doing.Y = start.Y;
                todo.Remove(start.West);
                result.Add(doing);
                MakeConnectedSet(doing, todo, result);
            }
            if ((start.East != null) && todo.ContainsKey(start.East))
            {
                var doing = todo[start.East];
                doing.X = start.X + 1;
                doing.Y = start.Y;
                todo.Remove(start.East);
                result.Add( doing);
                MakeConnectedSet(doing, todo, result);
            }
            if (todo.ContainsKey(start.Id))
            {
                todo.Remove(start.Id);
                result.Add(start);
            }
        }


        

        private static string GetDirection(IEnumerable<XElement> routes, string direction)
        {
            var dir = routes.SingleOrDefault(r => r.Element("direction").Value == direction);
            return dir == null ? null : dir.Element("to").Value;
        }

        private void BuildLocationsById()
        {
            var help =
                (from province in provinces
                from innerLoc in province.GetAllLocations()
                select innerLoc)
                .Distinct();
            LocationsById =
                help
                .Distinct()
                .ToDictionary(loc => loc.Id);
            
        }

        private string GetExploreCode(string id)
        {
            Tuple<int,bool> value;
            if (ExploresByLocation.TryGetValue(id, out value))
            {
                return " X" + value.Item1 + (value.Item2 ? "!" : "");
            }
            else
            {
                return " X0"; 
            }
            
        }

        private Province MakeProvince(IDictionary<string, XElement> provinceXml, string id, int r, int c)
        {
            XElement inXml = null;
            if (provinceXml.TryGetValue(id, out inXml))
            {
                //var brush = Province.BrushFromType(inXml.Element("type").Value, visited);
                var civRaw = inXml.Element("civ");
                var visits = inXml.Descendants("visit")
                    .ToLookup(visitXml => Int32.Parse(visitXml.Attribute("key").Value), visitXml => visitXml.Value)
                    .ToDictionary(gr => gr.Key, gr => gr.ToArray());
                var result = new Province()
                {
                    X = c * length,
                    Y = r * length,
                    Width = length,
                    Height = length,
                    Id = id,
                    Name = inXml.Element("name").Value + GetExploreCode(id),
                    Type = inXml.Element("type").Value,
                    Civ = Int32.Parse(civRaw == null ? "0" : civRaw.Value),
                    Visits = visits,
                    ControlledBy = inXml.Element("controlled-by").Value,
                    ControlledByIn = inXml.Element("controlled-by-in").Value
                };
                var innerLocations = from route in inXml.Descendants("route")
                                     let direction = route.Element("direction").Value
                                     where direction == "In" || direction == "Up" || direction == "Down"
                                     let routeId = route.Element("to").Value
                                     select MakeInnerLocation(provinceXml, routeId, route.Element("hidden").Value == "True", result,0);
                foreach (var innerloc in innerLocations.Distinct())
                    result.InnerLocations.Add(innerloc);
                return result;
            }
            else
            {
                return new Province()
                       {
                           X = c * length,
                           Y = r * length,
                           Width = length,
                           Height = length,
                           Id = id,
                           Name = "",
                       };
            }
        }

        private static InnerLocation MakeInnerLocation(IDictionary<string,XElement> provinceXml, string routeId, bool hidden, Location parentLocation, int recursionCount)
        {
            var innerLocs = recursionCount >= 2 ? Enumerable.Empty<InnerLocation>() :
                            from inner in provinceXml[routeId].Descendants("route")
                            where inner.Element("direction").Value == "In"
                            || inner.Element("direction").Value == "Down"
                            || inner.Element("direction").Value == "Underground"
                            let inrouteId = inner.Element("to").Value
                            select MakeInnerLocation(provinceXml, inrouteId, inner.Element("hidden").Value == "True", null /*FIXME*/, recursionCount+1);
            var type = provinceXml[routeId].Element("type").Value;
            InnerLocation result = null;
            if (type.Contains("city"))
            {
                result = new City()
                {
                    Id = routeId,
                    Name = provinceXml[routeId].Element("name").Value,
                    Type = type,
                    Hidden = hidden,
                    ParentLocation = parentLocation,
                    Skills = (from skill in provinceXml[routeId].Descendants("skill")
                              select Int32.Parse(skill.Value)).ToArray(),
                    Trades = (from trade in provinceXml[routeId].Descendants("trade")
                             select new Trade {
                                 Who = trade.Element("who").Value,
                                 ItemId = trade.Element("item-id").Value,
                                 ItemName = trade.Element("item-name").Value,
                                 Quantity = Int32.Parse(trade.Element("quantity").Value),
                                 Weight = Int32.Parse(trade.Element("weight-per-item").Value,System.Globalization.NumberStyles.AllowThousands),
                                 Price = Int32.Parse(trade.Element("price").Value),
                                 BuySell = (Trade.TradeType) Enum.Parse(typeof(Trade.TradeType),trade.Element("buysell").Value, true)
                             }).ToArray()
                };
               
            }
            else
            {
                result = new InnerLocation()
                {
                    Id = routeId,
                    Name = provinceXml[routeId].Element("name").Value,
                    Type = type,
                    Hidden = hidden,
                    ParentLocation = parentLocation,
                };
            }
            foreach (var innerloc in innerLocs)
            {
                if (parentLocation == null || innerloc.Id != parentLocation.Id)
                {
                    result.InnerLocations.Add(innerloc);
                    innerloc.ParentLocation = result;
                }
            }
            return result;
        }

        public Rect Extent
        {
            get
            {
                return new Rect(0, 0, width * length, height * length);
            }
        }

        private int Index(int row, int column) 
        {
            return row * width + column;
        }

        private static int RowFromY(double y) 
        {
            return (int) Math.Floor(y / length);
        }

        private static int ColFromX(double x) 
        {
            return (int)Math.Floor(x / length);
        }

        public IEnumerable<int> Query(Rect rectangle)
        {
            rectangle.Intersect(Extent);

            var r1 = RowFromY(rectangle.Top);
            var c1 = ColFromX(rectangle.Left);
            var r2 = RowFromY(rectangle.Bottom);
            var c2 = ColFromX(rectangle.Right);
            r2 = Math.Min(height, r2 + 1);
            c2 = Math.Min(width, c2 + 1); //add one to make sure it is drawn early enough

            return from ri in Enumerable.Range(r1, r2 - r1)
                   from ci in Enumerable.Range(c1, c2 - c1)
                   select Index(ri, ci);
        }

        public event EventHandler ExtentChanged;

        public event EventHandler QueryInvalidated;
        
        public int Count
        {
            get
            {
                return provinces.Length;
            }
        }

        public object this[int i]
        {
            get
            {
                return provinces[i];
            }
            set
            {
            }
        }

        #region Irrelevant IList Members

        int IList.Add(object value)
        {
            return 0;
        }

        void IList.Clear()
        {
        }

        bool IList.Contains(object value)
        {
            return false;
        }

        int IList.IndexOf(object value)
        {
            return 0;
        }

        void IList.Insert(int index, object value)
        {
        }

        void IList.Remove(object value)
        {
        }

        void IList.RemoveAt(int index)
        {
        }

        void ICollection.CopyTo(Array array, int index)
        {
        }

        bool IList.IsFixedSize
        {
            get { return false; }
        }

        bool IList.IsReadOnly
        {
            get { return true; }
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot
        {
            get { return null; }
        }

        int ICollection.Count
        {
            get { return provinces.Length; }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return provinces.GetEnumerator();
        }

        #endregion
    }
}
