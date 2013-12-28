using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WpfOly
{
    class UndergroundProvince : Province
    {
        public new string Type
        {
            get
            {
                return "fill";
            }

        }

        public new string FormattedIdAndName
        {
            get
            {
                return null;
            }
        }
    }
}
