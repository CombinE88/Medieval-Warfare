using System.Collections.Generic;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.MW.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits
{
    class AcolytePreyInfo : ConditionalTraitInfo
    {

        [Desc("Prey Animation when docking.")]
        [SequenceReference]
        public readonly string PreySequence = "prey";

        public readonly HashSet<string> TargetActors = new HashSet<string>();

        [Desc("Voice string when planting explosive charges.")]
        [VoiceReference]
        public readonly string Voice = "Action";

        public readonly string Cursor = "preycursor";

        public bool LeechesResources = true;

        public int leechinterval = 100;

        public string SelfEnabledCondition = null;

        [Desc("Resource Type / Prerequisites. NONE for none")]
        public readonly Dictionary<string, string> ResourceTypesPreres = new Dictionary<string, string>();

        public override object Create(ActorInitializer init) { return new AcolytePrey(this); }
    }

    class AcolytePrey : ConditionalTrait<AcolytePreyInfo>, IIssueOrder, IResolveOrder, IOrderVoice
    {
        readonly AcolytePreyInfo info;
        public Actor forceactor;

        public AcolytePrey(AcolytePreyInfo info) : base(info)
        {
            this.info = info;
        }

        public IEnumerable<IOrderTargeter> Orders
        {
            get { yield return new PreyOrderTargeter(info.Cursor); }
        }

        public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
        {
            if (order.OrderID == "Prey")
            {
                return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };
            }

            return null;
        }

        public void ResolveOrder(Actor self, Order order)
        {
            if (order.OrderString != "Prey")
                return;

            if (!order.Queued)
                self.CancelActivity();

            forceactor = order.TargetActor;

            self.QueueActivity(new Prey(self, forceactor));

        }

        public string VoicePhraseForOrder(Actor self, Order order)
        {
            return order.OrderString == "Prey" ? info.Voice : null;
        }

        class PreyOrderTargeter : UnitOrderTargeter
        {
            public PreyOrderTargeter(string cursor)
                : base("Prey", 6, cursor, true, false) { }

            public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
            {
                return self.Info.TraitInfo<AcolytePreyInfo>().TargetActors.Contains(target.Info.Name);
            }

            public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
            {
                return self.Info.TraitInfo<AcolytePreyInfo>().TargetActors.Contains(target.Info.Name);
            }
        }


    }
}
