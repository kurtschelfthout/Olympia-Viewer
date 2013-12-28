using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;

namespace WpfOly
{
    class Province : Location
    {
        public Province()
        {
            Visits = new Dictionary<int, string[]>();
        }

        public override int X { get; set; }
        public override int Y { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }

        public IDictionary<int, string[]> Visits { get; set; }
        public bool Visited
        {
            get
            {
                return Visits.Any();
            }
        }

        public int TurnLastVisited
        {
            get
            {
                return Visited ? Visits.Keys.Max() : -1;
            }
        }
        public int Civ { get; set; }

        public string ControlledBy { get; set; }
        public string ControlledByIn { get; set; }

        public string FormattedIdAndName
        {
            get
            {
                return string.Format("[{0}] {1}", Id, Name);
            }
        }


        public override string ExtraInfo
        {
            get
            {
                    var result = string.Format("Civ: {0}\nPossible sublocs:\n\t{1}",
                        Civ,
                        String.Join("\n\t", Tables.InnerLocs[this.Type]
                            .GroupBy(t => t.Item2)
                            .Select(t => t.Key + " hidden: " + String.Join(", ", t.Select(tt => tt.Item1)))));
                    if (!String.IsNullOrEmpty(ControlledBy))
                    {
                        result = string.Format("Controlled by {0} in {1}\n", ControlledBy, ControlledByIn) + result;
                    }
                    var visited = Visited ? "Last seen: " + TurnLastVisited + " by " + String.Join(", ", Visits[TurnLastVisited]) + "\n" : "";
                    return base.ExtraInfo + "\n" + visited + result;
            }
        }


    }
}
