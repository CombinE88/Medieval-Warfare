using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using OpenRA.Mods.Cnc.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	class HaulerInfo : ITraitInfo
	{
		public readonly HashSet<string> ResourceActorsTypes = new HashSet<string>();
		
		public readonly HashSet<string> DeliverActorsTypes = new HashSet<string>();

		public int Pickuptime = 100;
	
		public int Delivertime = 100;
		
		public int Storageamount = 100;
		
		public readonly string Cursor = "enter";
		
		public object Create(ActorInitializer init) { return new Hauler(this); }

		public int SearchRange = 12;
	}

	class Hauler : IIssueOrder, IResolveOrder, IOrderVoice, INotifyIdle
	{
		readonly HaulerInfo info;

		public int material;

		public Actor DeliverActor;
		public Actor Resourceactor;

		public Hauler(HaulerInfo info)
		{
			this.info = info;
		}

		void INotifyIdle.TickIdle(Actor self)
		{
			
			if (material>0)
			{
				self.QueueActivity(new HaulDelRes(self));
				return;
			}

		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new HaulerGrabTargeter(info.Cursor); 
				yield return new HaulerDeliverTargeter(info.Cursor); 
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "GetRes")
			{
				Resourceactor = target.Actor;
				return new Order(order.OrderID, self, queued) {TargetActor = target.Actor};
			}
			
			if (order.OrderID == "DelRes")
			{
				DeliverActor = target.Actor;
				return new Order(order.OrderID, self, queued) {TargetActor = target.Actor};
			}

			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "GetRes" && order.Queued)
				self.QueueActivity(new Prey(self));
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return null;
		}

		class HaulerGrabTargeter : UnitOrderTargeter
		{
			public HaulerGrabTargeter(string cursor)
				: base("GetRes", 6, cursor, true, false) { }

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{

				return target.Info.HasTraitInfo<TargetableInfo>() && target.Trait<Targetable>()
					       .TargetTypes.Intersect(self.Info.TraitInfo<HaulerInfo>().ResourceActorsTypes).Any();
			}

			public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
			{
				return target.Info.HasTraitInfo<TargetableInfo>() && target.Actor.Trait<Targetable>()
					       .TargetTypes.Intersect(self.Info.TraitInfo<HaulerInfo>().ResourceActorsTypes).Any();
			}
		}
		
		class HaulerDeliverTargeter : UnitOrderTargeter
		{
			public HaulerDeliverTargeter(string cursor)
				: base("DelRes", 6, cursor, true, false) { }

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				return target.Info.HasTraitInfo<TargetableInfo>() && target.Trait<Targetable>()
					       .TargetTypes.Intersect(self.Info.TraitInfo<HaulerInfo>().DeliverActorsTypes).Any();
			}

			public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
			{
				return target.Info.HasTraitInfo<TargetableInfo>() && target.Actor.Trait<Targetable>()
					       .TargetTypes.Intersect(self.Info.TraitInfo<HaulerInfo>().DeliverActorsTypes).Any();
			}
		}
	}
}
