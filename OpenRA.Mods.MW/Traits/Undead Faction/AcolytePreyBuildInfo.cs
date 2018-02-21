using System;
using System.Collections.Generic;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.MW.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits
{
    class AcolytePreyBuildInfo : ITraitInfo
    {

        [Desc("Prey Animation when docking.")]
        [SequenceReference]
        public readonly string PreySequence = "prey";

        public readonly HashSet<string> TargetActors = new HashSet<string>();

        [Desc("Voice string when planting explosive charges.")]
        [VoiceReference]
        public readonly string Voice = "Action";

        public int Buildinterval = 25;

        public string SelfEnabledCondition = null;

        public object Create(ActorInitializer init) { return new AcolytePreyBuild(this); }
    }

    class AcolytePreyBuild : IIssueOrder, IResolveOrder, IOrderVoice, ITick
    {
        readonly AcolytePreyBuildInfo info;
        public Actor forceactor;
        public bool SmartPrey;
        public int smarPreyWait;


        public AcolytePreyBuild(AcolytePreyBuildInfo info)
        {
            this.info = info;
            smarPreyWait = 20;
        }

        void ITick.Tick(Actor self)
        {
            if (SmartPrey && self.IsIdle)
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
            get
            {
                yield return new PreyBuildOrderTargeter("PreyBuild", 5,
              act => info.TargetActors.Contains(act.Info.Name), info.TargetActors);
            }
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
                SmartPrey = false;
            }
            if (order.OrderString != "PreyBuild")
            {
                return;
            }

            if (!order.Queued)
                self.CancelActivity();

            forceactor = order.TargetActor;
            SmartPrey = true;
            self.QueueActivity(new PreyBuild(self, forceactor));



        }

        public string VoicePhraseForOrder(Actor self, Order order)
        {
            return order.OrderString == "PreyBuild" ? info.Voice : null;
        }


        class PreyBuildOrderTargeter : UnitOrderTargeter
        {
            private HashSet<String> Validnames;

            public PreyBuildOrderTargeter(string order, int priority,
                Func<Actor, bool> able, HashSet<string> ValidActors)
                : base(order, priority, "preycursor", false, true)
            {
                Validnames = ValidActors;

            }

            public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
            {

                if (!self.Owner.IsAlliedWith(target.Owner) || !Validnames.Contains(target.Info.Name))
                    return false;

                cursor = self.Info.TraitInfo<AcolytePreyInfo>().Cursor;
                return true;
            }

            public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
            {
                return false;
            }
        }


    }
}
