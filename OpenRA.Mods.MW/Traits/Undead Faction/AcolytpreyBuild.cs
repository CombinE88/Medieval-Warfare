using System;
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
	class AcolytePreyBuildInfo : ITraitInfo
	{
		
		[Desc("Prey Animation when docking.")]
		[SequenceReference] public readonly string PreySequence = "prey";
		
		public readonly HashSet<string> TargetActors = new HashSet<string>();

		[Desc("Voice string when planting explosive charges.")]
		[VoiceReference] public readonly string Voice = "Action";

		public readonly string Cursor = "preycursor";

		public int Buildinterval = 25;
		
		public int Buildpower = 1;

		public string SelfEnabledCondition = null;
		
		public object Create(ActorInitializer init) { return new AcolytePreyBuild(this); }
	}

	class AcolytePreyBuild : IIssueOrder, IResolveOrder, IOrderVoice
	{
		readonly AcolytePreyBuildInfo info;
		public Actor forceactor;

		public AcolytePreyBuild(AcolytePreyBuildInfo info)
		{
			this.info = info;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new PreyBuildOrderTargeter("PreyBuild", 5,
				act => info.TargetActors.Contains(act.Info.Name)); }
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "PreyBuild")
			{
				forceactor = target.Actor;
				return new Order(order.OrderID, self, queued);
			}

			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "PreyBuild")
				return;
			
			if (!order.Queued)
				self.CancelActivity();

			
			if (forceactor != null)
				self.SetTargetLine(Target.FromCell(self.World, forceactor.Location), Color.Green);
			self.QueueActivity(new PreyBuild(self, forceactor));

		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "PreyBuild" ? info.Voice : null;
		}


		class PreyBuildOrderTargeter : UnitOrderTargeter
		{
			readonly Func<Actor, bool> canTarget;
			
			public PreyBuildOrderTargeter(string order, int priority,
				Func<Actor, bool> canTarget)
				: base(order, priority, "preycursor", false, true)
			{
				this.canTarget = canTarget;
			}

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{

				if (!self.Owner.IsAlliedWith(target.Owner) || !canTarget(target))
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
