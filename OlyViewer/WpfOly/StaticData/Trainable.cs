using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WpfOly
{
 

    class Trainable
    {
        public int ToTrain { get; private set; }
        public int NeededMan { get; private set; }
        public int NeededItem { get; private set; }
        public int NeededSkill { get; private set; }
        public string InLocation { get; private set; }

        public Trainable(int toTrain, int neededMan, int neededSkill, int neededItem, string inLocation)
        {
            ToTrain = toTrain;
            NeededItem = neededItem;
            NeededMan = neededMan;
            NeededSkill = neededSkill;
            InLocation = inLocation;
        }

        public Trainable(int toTrain, int neededMan, int neededSkill, int neededItem) : this(toTrain, neededMan, neededSkill, neededItem, null) {}

        public Trainable(int toTrain, int neededMan, int neededSkill) : this(toTrain, neededMan, neededSkill, -1) {}

        public Trainable(int toTrain, int neededMan) : this(toTrain, neededMan, -1) {}

    }

    class Trainables
    {
        public List<Trainable> trainables = new List<Trainable>();

        public Trainables()
        {
            trainables.Add(new Trainable(11,10));
            trainables.Add(new Trainable(12, 10, 610));
            trainables.Add(new Trainable(13, 12, 615, 72));
            trainables.Add(new Trainable(14, 20, 616, 53));
            trainables.Add(new Trainable(15, 14, 616, 73, "castle"));
            trainables.Add(new Trainable(16, 12, 610, 75));
            trainables.Add(new Trainable(17, 12, 750,-1,"temple"));
            trainables.Add(new Trainable(19, 10, 601));
            trainables.Add(new Trainable(20, 12, 616, 74));
            trainables.Add(new Trainable(21, 10, 610, 85));
            trainables.Add(new Trainable(22, 13, 615, -1, "castle"));
            trainables.Add(new Trainable(24, 19, 616, 74,"ship"));
        }
    }
}
