using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WpfOly
{
    class ArgumentCompletions
    {
        public IDictionary<ArgumentType, IEnumerable<CompletionData>> ByArgumentType = new Dictionary<ArgumentType, IEnumerable<CompletionData>>();

        private IList<Trainable> trainables = new Trainables().trainables;

        public ArgumentCompletions(IDictionary<int,Skill> skills, IDictionary<int,string> items)
        {
            //first the static stuff
            ByArgumentType.Add(ArgumentType.Zero, new[] { new CompletionData("0") });
            ByArgumentType.Add(ArgumentType.Flag, new[] { "0", "1" }.Select(s => new CompletionData(s)));
            ByArgumentType.Add(ArgumentType.Direction, new[] { "n", "e", "s", "w", "in", "out", "up", "down" }.Select(s => new CompletionData(s)));
            ByArgumentType.Add(ArgumentType.Behind, new[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" }.Select(s => new CompletionData(s)));
            ByArgumentType.Add(ArgumentType.Structure, new[] { "inn", "mine", "temple", "tower", "castle", "galley", "roundship" }.Select(s => new CompletionData(s)));
            ByArgumentType.Add(ArgumentType.Decree, new[] { "watch", "hostile" }.Select(s => new CompletionData(s)));
            ByArgumentType.Add(ArgumentType.Oath, new[] { "1", "2" }.Select(s => new CompletionData(s)));

            //read from the xml
            ByArgumentType.Add(ArgumentType.Item, Tables.Items.Select(kvp => new CompletionData(kvp.Key + " " + kvp.Value, "", kvp.Key.ToString())));
            ByArgumentType.Add(ArgumentType.Skill, 
                from skill in Tables.Skills.Values
                select new CompletionData(skill.Id.ToString() + " " + skill.Name, 
                           skill.LearningTime + " weeks " + (skill.RequiredNPs != 0 ? skill.RequiredNPs + "NP" : ""), 
                           skill.Id.ToString()));
            ByArgumentType.Add(ArgumentType.Trainable, trainables.Select(t => new CompletionData(t.ToTrain + " " + Tables.Items[t.ToTrain],
                NeededDescription(t, skills, items), t.ToTrain.ToString())));
        }

        private string NeededDescription(Trainable trainable, IDictionary<int, Skill> skills, IDictionary<int, string> items)
        {
            var result = "Needs " + items[trainable.NeededMan] + "[" + trainable.NeededMan + "]";
            if (trainable.NeededSkill > 0)
                result += ", " + skills[trainable.NeededSkill].Name + "[" + trainable.NeededSkill + "]";
            if (trainable.NeededItem > 0)
                result += ", " + items[trainable.NeededItem] + "[" + trainable.NeededItem + "]";
            if (trainable.InLocation != null)
                result += " in " + trainable.InLocation;
            return result;
        }
    }
}
