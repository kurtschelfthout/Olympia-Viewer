using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WpfOly
{
    class GateHit
    {
        public string LocationId { get; set; }
        public int LocationX { get; set; }
        public int LocationY { get; set; }

        public IEnumerable<string> Origins { get; set; }
        public int HitCount { get { return Origins.Count(); } }

        
    }
}
