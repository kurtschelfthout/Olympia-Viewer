using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WpfOly
{
    class Gate
    {
        public string Id { get; set; }

        public string From { get; set; }
        public int FromX { get; set; }
        public int FromY { get; set; }

        public string To { get; set; }
        public int ToX { get; set; }
        public int ToY { get; set; }

        public bool Sealed { get; set; }

        public string Info
        {
            get
            {
                return "Gate [" + Id + "] " + From + " -> " + To + (Sealed ? ", sealed" : "");
            }
        }
    }
}
