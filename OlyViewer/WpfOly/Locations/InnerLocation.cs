using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WpfOly
{
    class InnerLocation : Location
    {
        public bool Hidden { get; set; }
        public Location ParentLocation { get; set; }
        public override int X
        {
            get
            {
                return ParentLocation.X;
            }
            set
            {
                throw new InvalidOperationException("Canot set X on InnerLocation");
            }
        }
        public override int Y
        {
            get
            {
                return ParentLocation.Y;
            }
            set
            {
                throw new InvalidOperationException("Canot set Y on InnerLocation");
            }
        }

        public virtual string FormattedIdAndName
        {
            get
            {
                var result = "[" + Id + "]" + (String.IsNullOrWhiteSpace(Type) ? Name : Type);
                return Hidden ? result += ",hidden" : result;
            }
        }

        public override string ExtraInfo
        {
            get
            {
                var result =
                    from quests in Tables.Quests
                    where quests.Key == Type
                    from quest in quests
                    select String.Format("{0} [{3}]: {1}-{2}", Tables.Items[quest.Item1], quest.Item2, quest.Item3, quest.Item1);
                var ret = result.Any() ? String.Format("Quests:\n\t{0}", String.Join("\n\t", result)) : "No quests here.";
                return base.ExtraInfo + "\n" + ret;
            }
        }

    }
}
