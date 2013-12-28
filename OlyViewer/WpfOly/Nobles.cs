using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace WpfOly
{
    class Nobles
    {
        public IEnumerable<Noble> All { get; private set; }

        internal Nobles(IEnumerable<Map> maps)
        {
            All = MakeNobles(maps);
            foreach (var noble in All)
            {
                noble.SetLastKnownLocation();
            }
        }

        private static IEnumerable<Noble> MakeNobles(IEnumerable<Map> maps)
        {
            var doc = XDocument.Load("nobles.xml");

            return from noble in doc.Descendants("noble")
                   select new Noble()
                   {
                       Faction = noble.Element("faction").Value,
                       Id = noble.Element("id").Value,
                       Name = noble.Element("name").Value,
                       LocationHistory = MakeLocationHistory(noble.Descendants("location"),maps)
                   };

        }

        private static Tuple<int, int, string> ParseNobleLocation(string time, string loc)
        {
            var p = time.TrimStart('(')
                .TrimEnd(')')
                .Split(' ');
            var turn = p[0];
            var day = p[1];
            return new Tuple<int, int, string>(Int32.Parse(turn), Int32.Parse(day), loc);
        }

        private class ComparerFirstTwo : IEqualityComparer<Tuple<int, int, string>>
        {
            bool IEqualityComparer<Tuple<int, int, string>>.Equals(Tuple<int, int, string> x, Tuple<int, int, string> y)
            {
                return x.Item1 == x.Item1 && x.Item2 == x.Item2;
            }

            int IEqualityComparer<Tuple<int, int, string>>.GetHashCode(Tuple<int, int, string> obj)
            {
                return new Tuple<int, int>(obj.Item1, obj.Item2).GetHashCode();
            }
        }


        private static IDictionary<Tuple<int,int>,Location[]> MakeLocationHistory(IEnumerable<XElement> nobleLocs, IEnumerable<Map> maps)
        {
            return
                (from xml in nobleLocs
                 let nl = ParseNobleLocation(xml.Attribute("key").Value,xml.Value)
                 select nl)
                 .Distinct(new ComparerFirstTwo()) //FIXME!! just throws away moves between locs time 0
                .ToDictionary(t => new Tuple<int,int>(t.Item1,t.Item2),
                    t =>
                    {
                        var result = from map in maps
                                     where map.LocationsById.ContainsKey(t.Item3)
                                     select map.LocationsById[t.Item3];
                        return result.Any() ? result.ToArray() : new[] { new UnknownLocation() };
                    });
        }
    }
}
