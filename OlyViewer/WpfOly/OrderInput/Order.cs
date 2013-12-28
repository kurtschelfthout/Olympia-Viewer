using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ICSharpCode.AvalonEdit.CodeCompletion;

namespace WpfOly
{
    class Order
    {
        public string Name { get; private set; }
        public Argument[] Arguments { get; private set; }
        public int Time { get; private set; }
        public int Priority { get; private set; }

        Order(string name, IEnumerable<Argument> arguments, int time, int priority)
        {
            this.Name = name.ToLowerInvariant();
            this.Arguments = arguments.ToArray();
        }

        public static Order Parse(string text)
        {
            var match = Regex.Match(text, @"(.+?) time: (\d+) days?\s+priority: (\d)");
            var format = match.Groups[1].Value.Trim();
            var time = Int32.Parse(match.Groups[2].Value);
            var priority = Int32.Parse(match.Groups[3].Value);

            var pieces = format.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var name = pieces[0].ToLowerInvariant();
            var arguments = new List<Argument>();
            for (int i = 1; i < pieces.Length; i++)
            {
                arguments.Add(Argument.Parse(pieces[i]));
            }
            return new Order(name, arguments, time, priority);
        }

        public string Help
        {
            get
            {
                return Name + " " + string.Join<Argument>(" ", Arguments);
            }
        }

        internal IEnumerable<ICompletionData> GetCompletions(ArgumentCompletions argumentCompletions, int arg)
        {
            if (arg >= Arguments.Length)
                return new CompletionData[0];

            var result = new List<CompletionData>();
            foreach (var argumentType in Arguments[arg].PossibleTypes)
            {
                IEnumerable<CompletionData> toAdd = null;
                if (argumentCompletions.ByArgumentType.TryGetValue(argumentType, out toAdd))
                {
                    result.AddRange(toAdd);
                }
            }
            return result;

            //if (Name.Equals("study") && arg == 0)
            //{
            //    return from skill in Tables.Skills.Values
            //           select new CompletionData(skill.Id.ToString() + " " + skill.Name, 
            //               skill.LearningTime + " weeks " + (skill.RequiredNPs != 0 ? skill.RequiredNPs + "NP" : ""), 
            //               skill.Id.ToString());
            //}
            //else
            //    return new CompletionData[0];
        }

        
    }
}
