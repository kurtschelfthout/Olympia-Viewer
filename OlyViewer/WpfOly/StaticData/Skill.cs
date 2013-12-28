using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WpfOly
{
    class Skill
    {
        public string Name { get; private set; }
        public int Id { get; private set; }
        public int RequiredNPs { get; private set; }
        public int ParentId { get; private set; }
        public int LearningTime { get; private set; }

        public Skill(string name, int id, int nps, int learningTime, int parent)
        {
            Name = name;
            Id = id;
            RequiredNPs = nps;
            ParentId = parent;
            LearningTime = learningTime;
        }

        public Skill(string name, int id, int nps, int learningTime) : this(name, id, nps, learningTime ,-1)
        {
        }

        public override string ToString()
        {
            return this.Name;
        }
    }
}
