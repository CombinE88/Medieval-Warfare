using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.MW.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits
{
    class AcolytePreyBuildInfo : ITraitInfo, IRulesetLoaded
    {
        [Desc("Prey Animation when docking.")]
        [SequenceReference]
        public readonly string PreySequence = "prey";

        [Desc("Which sprite body to modify.")] public readonly string Body = "body";

        public readonly HashSet<string> TargetActors = new HashSet<string>();

        [Desc("Voice string when planting explosive charges.")]
        [VoiceReference]
        public readonly string Voice = "Action";

        public readonly string Cursor = "preycursor";

        public readonly int Buildinterval = 25;

        public readonly string SelfEnabledCondition = null;

        public object Create(ActorInitializer init) { return new AcolytePreyBuild(this); }

        public void RulesetLoaded(Ruleset rules, ActorInfo ai)
        {
            var matches = ai.TraitInfos<WithSpriteBodyInfo>().Count(w => w.Name == Body);
            if (matches != 1)
                throw new YamlException("WithMoveAnimation needs exactly one sprite body with matching name.");
        }
    }

    class AcolytePreyBuild : IIssueOrder, IResolveOrder, IOrderVoice, ITick
    {
        readonly AcolytePreyBuildInfo info;
        private bool smartPrey;
        private int smarPreyWait;

        public AcolytePreyBuild(AcolytePreyBuildInfo info)
        {
            this.info = info;
            smarPreyWait = 20;
        }

        void ITick.Tick(Actor self)
        {
            if (smartPrey && self.IsIdle)
            {
                --smarPreyWait;
                if (smarPreyWait <= 0)
                {
                    self.QueueActivity(new PreyBuild(self, null));
                    smarPreyWait = 20;
                }
            }
        }

        public IEnumerable<IOrderTargeter> Orders
        {
            get { yield return new PreyOrderBuildTargeter(info.Cursor); }
        }

        public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
        {
            if (order.OrderID == "PreyBuild")
            {
                return new Order(order.OrderID, self, Target.FromActor(target.Actor), queued) { }; // TargetActor = target.Actor };
            }

            return null;
        }

        public void ResolveOrder(Actor self, Order order)
        {
            if (order.OrderString == "Stop" || order.OrderString == "Move")
            {
                // Turn off idle smarts to obey the stop/move:
                smartPrey = false;
            }

            if (order.OrderString != "PreyBuild")
            {
                return;
            }

            if (!order.Queued)
                self.CancelActivity();

            var forceactor = order.Target.Actor;
            smartPrey = true;
            self.QueueActivity(new PreyBuild(self, forceactor));
        }

        public string VoicePhraseForOrder(Actor self, Order order)
        {
            return order.OrderString == "PreyBuild" ? info.Voice : null;
        }

        class PreyOrderBuildTargeter : UnitOrderTargeter
        {
            public PreyOrderBuildTargeter(string cursor)
                : base("PreyBuild", 6, cursor, true, true) { }

            public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
            {
                return self.Info.TraitInfo<AcolytePreyBuildInfo>().TargetActors.Contains(target.Info.Name);
            }

            public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
            {
                return self.Info.TraitInfo<AcolytePreyBuildInfo>().TargetActors.Contains(target.Info.Name);
            }
        }
    }
}
