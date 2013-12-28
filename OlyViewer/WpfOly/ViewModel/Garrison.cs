using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WpfOly
{
    class Garrison
    {
        public string Id { get; set; }
        public string LocationId { get; set; }

        public string CastleId { get; set; }
        
        public IDictionary<string,int> Inventory { get; set; }

        public int GarrisonX { get; set; }
        public int GarrisonY { get; set; }

        public string Info
        {
            get
            {
                return "[" + Id + "] controlled by " + CastleId + "\n" +
                    String.Join("\n", Inventory.Where(keyval => keyval.Value != 0).Select(keyval => keyval.Value + " " + Tables.GetItemNameOrNumber(keyval.Key) + "[" + keyval.Key +"]"));
            }
        }

    }
}
