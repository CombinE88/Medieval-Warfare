using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.AI;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits
{
    public class AIHelperInfo : ITraitInfo
    {
        public readonly int CashDelay = 250;
        public readonly int PeasantDelay = 100;

        public readonly Dictionary<string, int> Cash = new Dictionary<string, int>();
        public readonly Dictionary<string, int> Peasants = new Dictionary<string, int>();

        public object Create(ActorInitializer init) { return new AIHelper(init.Self, this); }
    }


    public class AIHelper : ITick
    {
        readonly AIHelperInfo info;
        private int _delay;
        private int _peasantdelay;
        private PlayerResources pr;

        public AIHelper(Actor self, AIHelperInfo info)
        {
            this.info = info;
            _delay = info.CashDelay;
            _peasantdelay = info.PeasantDelay;
            pr = self.Trait<PlayerResources>();
        }

        void ITick.Tick(Actor self)
        {
            if (!self.Owner.IsBot)
                return;

            if (info.Cash.ContainsKey(self.Owner.BotType) && _delay-- <= 0)
            {
                pr.GiveResources(info.Cash[self.Owner.BotType]);
                _delay = info.CashDelay;
            }
            //if (self.Owner.Faction.InternalName != "ded" && info.Peasants.ContainsKey(self.Owner.BotType) && _peasantdelay-- <= 0)
            //{
            //    for (var i = 0; i < info.Peasants[self.Owner.BotType]; i++)
            //    {
            //        self.Trait<PlayerCivilization>().Spawnapeasant();
            //    }
            //    _peasantdelay = info.PeasantDelay;
            //}
        }
    }
}