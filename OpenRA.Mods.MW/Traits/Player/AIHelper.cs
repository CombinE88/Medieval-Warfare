using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits
{
    public class AIHelperInfo : ITraitInfo
    {
        public readonly int CashDelay = 250;

        public readonly Dictionary<string, int> Cash = new Dictionary<string, int>();

        public object Create(ActorInitializer init) { return new AIHelper(init.Self, this); }
    }

    public class AIHelper : ITick
    {
        readonly AIHelperInfo info;
        private int delay;
        private PlayerResources pr;

        public AIHelper(Actor self, AIHelperInfo info)
        {
            this.info = info;
            delay = info.CashDelay;
            pr = self.Trait<PlayerResources>();
        }

        void ITick.Tick(Actor self)
        {
            if (!self.Owner.IsBot)
                return;

            if (info.Cash.ContainsKey(self.Owner.BotType) && delay-- <= 0)
            {
                pr.GiveResources(info.Cash[self.Owner.BotType]);
                delay = info.CashDelay;
            }
        }
    }
}