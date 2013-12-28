using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WpfOly
{
    enum ArgumentType
    {
        Any, //used when we don't know all possibilities
        Zero, //used for anyone or anything, depending on context
        Faction,
        Item,
        Character,
        Castle,
        Building, //includes castle
        Ship,
        Constant, //a constant string, like 'ALL' in admit.
        Flag,    // 0 or 1
        Location, // not a sublocation
        Direction, //i.e. n e s w etc
        SubLocation,
        Text,
        Behind,
        Amount,
        Structure, //either literal inn mine temple tower castle galley or roundship
        Unformed, //an id from the unformed list
        Breedable, //an id of a breedable thing
        Trainable, //an id of something that can be trained to something else, e.g. peasant
        Decree, //watch or hostile
        Skill,
        Garrison,
        Oath,
        WaitConditions,
        Unknown
    }

    
}
