using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Text.RegularExpressions;

namespace WpfOly
{

    class Orders
    {
        public static Order[] All = new Order[] {
            Order.Parse("ACCEPT     <from-who:faction,character,zero> <item>  [qty:amount]             time: 0 days    priority: 0"),
            Order.Parse("ADMIT      <who-or-what:character,building,ship> [ALL] [units:character]*          time: 0 days    priority: 0"),
            Order.Parse("ATTACK     <target:character,ship,building,sublocation> [flag]                      time: 1 day     priority: 3"),
            Order.Parse("BANNER     [unit] <message:text>                     time: 0 days    priority: 1"),
            Order.Parse("BEHIND     <number:behind>                             time: 0 days    priority: 1"),
            Order.Parse("BREED     <parent1:breedable> <parent2:breedable>                             time: 999 days    priority: 3"),
            Order.Parse("BRIBE      <who:character> <amount> [flag]                time: 7 days    priority: 3"),
            Order.Parse("BUILD      <structure> <name:text> [max-days:amount] [id:unformed]   time: 999 days   priority: 3"),
            Order.Parse("BUY        <item> <qty:amount> <price:amount> [have-left:amount]     time: 0 days    priority: 1"),
            Order.Parse("CATCH      [number-of-horses:amount,zero] [days:amount]            time: 999 days  priority: 3"),
            Order.Parse("CLAIM      <item> [number:amount]                       time: 0 days   priority: 1"),
            Order.Parse("COLLECT    <item> [number:amount,zero] [days:amount]               time: 999 days  priority: 3"),
            Order.Parse("CONTACT    <who:character,faction>                                time: 0 days    priority: 0"),
            Order.Parse("DECREE     <decree> <who:character>                            time: 0 days    priority: 0"),
            Order.Parse("DEFAULT    <who:character,faction>                                time: 0 days    priority: 0"),
            Order.Parse("DEFEND     <who:character,faction>                                time: 0 days    priority: 0"),
            Order.Parse("DIE   time: 0 days   priority: 3"),
            Order.Parse("DROP       <item> <qty:amount,zero> [have-left:amount]             time: 0 days    priority: 1"),
            //Order.Parse("EMAIL      <new email address:text>"), must appear after begin order
            Order.Parse("EXECUTE    [prisoner:character]                           time: 0 days    priority: 1"),
            Order.Parse("EXPLORE                                         time: 7 days    priority: 3"),
            Order.Parse("FEE        [gold-per-100-wt:amount]                    time: 0 days    priority: 1"),
            Order.Parse("FERRY                                           time: 0 days    priority: 1"),
            Order.Parse("FISH       [number-of-fish:amount,zero]  [days:amount]             time: 999 days  priority: 3"),
            Order.Parse("FLAG       <string:text>                               time: 0 days    priority: 1"),
            Order.Parse("FLY        <direction-or-destination:location,direction,sublocation,building,ship>*     time: 999 days    priority: 2"),
            Order.Parse("FORGET       <skill>                               time: 0 days    priority: 3"),
            Order.Parse("FORM       <unit:unformed> <name:text>       time: 7 days    priority: 3"),
            //Order.Parse("FORMAT     <flag>"), after begin only
            Order.Parse("GARRISON   <castle>                             time: 1 day     priority: 3"),
            Order.Parse("GET        <who:character,garrison> <item> [qty:amount] [have-left:amount]       time: 0 days    priority: 1"),
            Order.Parse("GIVE       <to-who:character,garrison> <item> [qty:amount] [have-left:amount]    time: 0 days    priority: 1"),
            Order.Parse("GUARD      <flag>                               time: 0 days    priority: 1"),
            Order.Parse("HONOR      <amount>                             time: 1 day     priority: 3"),
            Order.Parse("HOSTILE    <who:character,faction>                                time: 0 days    priority: 0"),
            Order.Parse("IMPROVE    [days:amount]                               time: 999 days    priority: 3"),
            //Order.Parse("LORE       <lore sheet>"), after begin order
            Order.Parse("MAKE       <item> [qty:amount]                         time: 999 days  priority: 3"),
            Order.Parse("MESSAGE    <nb-of-lines-of-text:amount> <to-who:character,faction,location>        time: 1 day     priority: 3"),

            Order.Parse("MOVE       <direction-or-destination:location,direction,sublocation,building,ship>*     time: 999 days    priority: 2"),
            Order.Parse("NAME       [unit:character or faction] <new-name-for-unit:text>           time: 0 days    priority: 1"),
            Order.Parse("NEUTRAL    <who:character or faction>                                time: 0 days    priority: 0"),
            //Order.Parse("NOTAB      <num>"), //after begin order
            Order.Parse("OATH       <level:oath>                              time: 1 day     priority: 3"),
            //Order.Parse("PASSWORD   ["password"]"), 
            Order.Parse("PAY        <to-who:character> [amount:amount,zero] [have-left:amount]        time: 0 days    priority: 1"),
            Order.Parse("PILLAGE    [attack-guards:flag]                               time: 7 days    priority: 3"),
            //Order.Parse("PLAYERS "),
            Order.Parse("PLEDGE     <who:character>                                time: 0 days    priority: 1"),
            Order.Parse("POST       <nb-of-lines-of-following-text:amount>       time: 1 day     priority: 3"),
            Order.Parse("PRESS      <nb-of-lines-of-following-text:amount>                 time: 0 days    priority: 1"),
            Order.Parse("PROMOTE    <who:character>                                time: 0 days    priority: 1"),
            //Order.Parse("PUBLIC"),
            Order.Parse("QUARRY     [number-of-stones:amount,zero]  [days:amount]           time: 999 days  priority: 3"),
            Order.Parse("QUEST                                           time: 7 days    priority: 3"),
            //Order.Parse("QUIT"),  //player entity
            Order.Parse("RAZE       [building]                           time: 999 days    priority: 3 "),
            Order.Parse("RECRUIT    [days:amount]                               time: 999 days  priority: 3 "),
            Order.Parse("REPAIR     [days:amount]                               time: 999 days  priority: 3 "),
            Order.Parse("RESEARCH   <skill>                              time: 7 days    priority: 3 "),
            //Order.Parse("//RESEND     [turn]       //only after begin "),
            Order.Parse("RUMOR      <number-of-lines-of-text:amount>                 time: 0 days    priority: 1 "),
            Order.Parse("SAIL        <direction-or-destination:location,direction,sublocation>*  time: 999 days   priority: 4 "),
            Order.Parse("SEEK       <who:character>                                time: 7 days    priority: 3 "),
            Order.Parse("SELL       <item> <qty:amount> <price:amount> [have-left:amount,zero] [hide-seller:flag]     time: 0 days    priority: 1"),
            Order.Parse("STACK      <character>                          time: 0 days    priority: 1 "),
            Order.Parse("STOP time: 0 days priority: 0"),
            Order.Parse("STUDY      <skill>                              time: 7 days    priority: 3 "),
            Order.Parse("SURRENDER  <character>                          time: 0 days    priority: 1 "),
            Order.Parse("TAKE       <who:character> <item> [qty:amount or zero] [have-left:amount]       time: 0 days    priority: 1 "),
            Order.Parse("TERRORIZE  <who:character> <severity:amount>                     time: 7 days    priority: 3 "),
            //Order.Parse("// TIMES   time: 0    priority: 3 only after begin "),
            Order.Parse("TRAIN      <kind:trainable> [days:amount,zero]                        time: 999 days  priority: 3 "),
            Order.Parse("UNGARRISON   [garrison]                         time: 1 day     priority: 3 "),
            Order.Parse("UNLOAD                                          time: 0 days    priority: 3"),
            Order.Parse("UNSTACK    [who:character]                                time: 0 days    priority: 1"),
            Order.Parse("USE        <skill>  [arguments:Any]*              time: 999 days    priority: 3"),
            //Order.Parse("VIS_EMAIL  <new email address for the player list:text> only after begin ty 
            Order.Parse("WAIT      <conditions:waitconditions>                           time: 999 days    priority: 1"),
            Order.Parse("WOOD     [number-of-wood:amount,zero]  [days:amount]           time: 999 days priority: 3")
       };

        public static IDictionary<string, Order> ByName =
            All.ToDictionary(order => order.Name);
    }
}

