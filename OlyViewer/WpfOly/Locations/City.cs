using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WpfOly
{
    sealed class City : InnerLocation
    {
        public int[] Skills { get; set; }

        public Trade[] Trades { get; set; }

        public override string FormattedIdAndName
        {
            get
            {
                var result = "[" + Id + "]" + Name;
                return Hidden ? result += ",hidden" : result;
            }
        }

        public override string ExtraInfo
        {
            get
            {
                return Skills.Aggregate("", (s, e) => s + Tables.Skills[e] + "[" + e.ToString() + "]" + " ")
                    + "\n" + String.Join<Trade>("\n", Trades); 
            }
        }
    }
}
