using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace WpfOly
{
    static class Tables
    {
        public static IDictionary<int, Skill> Skills { get; private set; }

        public static IDictionary<int, string> Items { get; private set; }

        public static string GetItemNameOrNumber(string number)
        {
            int parsedNumber = 0;
            string name = "";

            if (Int32.TryParse(number, out parsedNumber)
                && Items.TryGetValue(parsedNumber, out name))
                return name;
            else
                return number;
        }

        public static ILookup<string, Tuple<string, Hidden>> InnerLocs { get; private set; }

        public static ILookup<string, Tuple<int, int, int>> Quests { get; private set; }

        public static ILookup<string, Tuple<int, int>> Productions { get; private set; }

        static Tables()
        {
            var skills = XDocument.Load("skills.xml");
            Skills = (from skill in skills.Descendants("skill")
                      select new Skill(skill.Element("name").Value,
                          Int32.Parse(skill.Element("id").Value),
                          Int32.Parse(skill.Element("req-nps").Value),
                          Int32.Parse(skill.Element("learning-time").Value)))
                          .Concat(
                        (from skill in skills.Descendants("skill")
                         from subskill in skill.Descendants("sub-skill")
                         select new Skill(subskill.Element("name").Value,
                             Int32.Parse(subskill.Element("id").Value),
                             Int32.Parse(subskill.Element("req-nps").Value),
                             Int32.Parse(subskill.Element("learning-time").Value),
                             Int32.Parse(skill.Element("id").Value))))
                          .ToDictionary(sk => sk.Id);

            var items = XDocument.Load("items.xml");
            Items = (from item in items.Descendants("item")
                      select new { Id = item.Element("id").Value, Name = item.Element("name").Value })
                     .ToDictionary(elem => Int32.Parse(elem.Id), elem => elem.Name);

            var innerLocs = XDocument.Load("innerlocs.xml");
            InnerLocs = (from innerloc in innerLocs.Descendants("innerloc")
                         select new { Prov = innerloc.Element("province-type").Value,
                                      Inner = innerloc.Element("subloc-type").Value,
                                      IsHidden = innerloc.Element("hidden").Value == ":yes" ? 
                                      Hidden.Always :
                                      innerloc.Element("hidden").Value == ":no" ? Hidden.Never : Hidden.Maybe

                         }
                         ).ToLookup(elem => elem.Prov, elem => new Tuple<string,Hidden>(elem.Inner,elem.IsHidden))
                         ;

            var quests = XDocument.Load("quests.xml");
            Quests = (from quest in quests.Descendants("quest")
                      select new
                      {
                          Id = Int32.Parse(quest.Element("what").Value),
                          Where = quest.Element("where").Value,
                          AtLeast = Int32.Parse(quest.Element("at-least").Value),
                          AtMost = Int32.Parse(quest.Element("at-most").Value),
                      })
                    .ToLookup(elem => elem.Where, elem => new Tuple<int, int, int>(elem.Id, elem.AtLeast, elem.AtMost));

            var productions = XDocument.Load("productions.xml");
            Productions = (from production in productions.Descendants("production")
                           select new
                           {
                               LocationType = production.Element("location-type").Value,
                               Item = Int32.Parse(production.Element("item-id").Value),
                               Quantity = Int32.Parse(production.Element("quantity").Value)
                           })
                           .ToLookup(elem => elem.LocationType, elem => new Tuple<int, int>(elem.Item, elem.Quantity));
        }

    }

     enum Hidden 
    {
         Always,
         Maybe,
         Never
    }
}
