using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace WpfOly
{
    abstract class Location : INotifyPropertyChanged
    {
        public virtual int X { get; set; }
        public virtual int Y { get; set;  }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }

        public ObservableCollection<Noble> Nobles { get; private set; }

        public ObservableCollection<InnerLocation> InnerLocations { get; private set; }

        public virtual string ExtraInfo
        {
            get
            {
                var result =
                   from productions in Tables.Productions
                   where productions.Key == Type
                   from production in productions
                   select String.Format("{2} {0} [{1}]", Tables.Items[production.Item1], production.Item1, production.Item2);
                return result.Any() ? String.Format("Produces per turn:\n\t{0}", String.Join("\n\t", result)) : "Doesn't produce anything.";
            }
        }
        
        protected Location()
        {
            InnerLocations = new ObservableCollection<InnerLocation>();
            Nobles = new ObservableCollection<Noble>();
        }

        /// <summary>
        /// Returns this location and all its sublocations.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Location> GetAllLocations()
        {
            yield return this;
            foreach (var subloc in InnerLocations)
                foreach (var subsubloc in subloc.GetAllLocations())
                    yield return subsubloc;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public override bool Equals(object obj)
        {
            var other = obj as Location;
            return (other != null) && (other.Id == this.Id);
        }

        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }
    }
}
