using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WpfOly
{
    class Argument
    {
        public string Name { get; private set; }
        public ArgumentType[] PossibleTypes { get; private set; }
        public bool Optional { get; private set; }
        public bool Multiple { get; set; }

        Argument(string name, IEnumerable<ArgumentType> possibleTypes, bool optional, bool multiple)
        {
            this.Name = name;
            this.PossibleTypes = possibleTypes.ToArray();
            this.Optional = optional;
            this.Multiple = multiple;
        }

        Argument(string text, bool optional, bool multiple)
            : this(GetName(text), TypeFromName(text), optional, multiple)
        {
        }

        private static string GetName(string name)
        {
            return name.Split(':')[0];
        }

        private static ArgumentType[] TypeFromName(string name)
        {
            var pieces = name.Split(':');
            if (pieces.Length == 1)
            {
                if (name.ToUpperInvariant().Equals(name))
                    return new[] { ArgumentType.Constant };
                ArgumentType type = ArgumentType.Unknown;
                Enum.TryParse<ArgumentType>(pieces[0], true, out type);
                return new[] { type };

            }
            else
            {
                var types = pieces[1].Split(',');
                return
                    (from type in types
                     select (ArgumentType)Enum.Parse(typeof(ArgumentType), type, true)).ToArray();
            }
        }

        internal static Argument Parse(string argument)
        {
            return new Argument(argument.TrimStart('<', '[').TrimEnd('>', ']', '*'), argument.StartsWith("["), argument.EndsWith("*"));
        }

        public override string ToString()
        {
            var b = this.Name + " : " + string.Join(" or ", this.PossibleTypes.Select(t => t.ToString().ToLowerInvariant()));
            b = this.Optional ? "[" + b + "]" : "<" + b + ">";
            return this.Multiple ? b + "*" : b;
        }

    }
}
