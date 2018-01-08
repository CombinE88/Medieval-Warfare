using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using OpenRA.Mods.Cnc.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits
{
	class CorruptDeerstandInfo : ConditionalTraitInfo
	{
		
		[Desc("Prey Animation when docking.")]
		[SequenceReference] public readonly string PreySequence = "prey";
		
		public readonly HashSet<string> TargetActors = new HashSet<string>();

		[Desc("Voice string when planting explosive charges.")]
		[VoiceReference] public readonly string Voice = "Action";

		public readonly string Cursor = "attack";
		
		public override object Create(ActorInitializer init) { return new CorruptDeerstand(this); }
	}

	class CorruptDeerstand : ConditionalTrait<CorruptDeerstandInfo>, IIssueOrder, IResolveOrder, IOrderVoice
	{
		readonly CorruptDeerstandInfo info;
        public Actor TargetStand;

		public CorruptDeerstand(CorruptDeerstandInfo info) : base(info)
		{
			this.info = info;
            TargetStand = null;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new PreyOrderTargeter(info.Cursor); }
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "Corrupt")
			{
				return new Order(order.OrderID, self, queued) {TargetActor = target.Actor};
			}

			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "Corrupt")
				return;

            if (!order.Queued)
            {
                TargetStand = null;
                self.CancelActivity();
            }

            TargetStand = order.TargetActor;
			self.QueueActivity(new Attack(self, Target.FromActor(order.TargetActor), true, true, 100));

		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "Corrupt" ? info.Voice : null;
		}

		class PreyOrderTargeter : UnitOrderTargeter
		{
			public PreyOrderTargeter(string cursor)
				: base("Corrupt", 6, cursor, true, true) { }

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				return self.Info.TraitInfo<CorruptDeerstandInfo>().TargetActors.Contains(target.Info.Name);
			}

			public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
			{
				return self.Info.TraitInfo<CorruptDeerstandInfo>().TargetActors.Contains(target.Info.Name);
			}
		}
		

	}
}
