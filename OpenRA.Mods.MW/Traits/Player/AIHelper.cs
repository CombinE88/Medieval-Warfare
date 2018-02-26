using System.Collections.Generic;
using OpenRA.Mods.Common.AI;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits.Player
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
        private bool isBot;
        private PlayerResources pr;
        private PlayerCivilization pc;
        private HackyAI hi;

        public AIHelper(Actor self, AIHelperInfo info)
        {
            this.info = info;
            _delay = info.CashDelay;
            _peasantdelay = info.PeasantDelay;
            isBot = false;
            pr = self.Owner.PlayerActor.Trait<PlayerResources>();
            pc = self.Owner.PlayerActor.Trait<PlayerCivilization>();

            if (self.Owner.IsBot)
            {
                isBot = true;
            }
        }

        void ITick.Tick(Actor self)
        {
            if (!isBot)
                return;

            if (!info.Cash.ContainsKey(hi.Info.Type) && _delay-- <= 0)
            {
                pr.GiveResources(info.Cash[hi.Info.Type]);
                _delay = info.CashDelay;
            }
            if (!info.Peasants.ContainsKey(hi.Info.Type) && _peasantdelay-- <= 0)
            {
                for (var i = 0; i < info.Peasants[hi.Info.Type]; i++)
                {
                    pc.Spawnapeasant();
                }
                _delay = info.PeasantDelay;
            }
        }
    }
}