using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets.Logic;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits
{
	public class AutoTargetFilterInfo : ConditionalTraitInfo
	{


		[FieldLoader.Require] [Desc("Target Actors can ahve conditions.")] 
		public readonly string Condition = null;
		
		public override object Create(ActorInitializer init)
		{
			return new AutoTargetFilter(init, this);
		}
	}

	public class AutoTargetFilter : ConditionalTrait<AutoTargetFilterInfo>, ITick
	{
		readonly AutoTargetFilterInfo info;
		private AutoTarget AutoTarget;
		private Actor Target;
		private Actor NewTarget;
		private Player Owner;


		public AutoTargetFilter(ActorInitializer init, AutoTargetFilterInfo info)
			: base(info)
		{
			this.info = info;
		}

		protected override void Created(Actor self)
		{
			AutoTarget = self.Trait<AutoTarget>();
			base.Created(self);
			Owner = self.Owner;
		}

		HashSet<Actor> GetAllTargetableActors(Actor self)
		{
			var possibles = self.World.FindActorsInCircle(self.CenterPosition, WDist.FromCells(AutoTarget.Info.ScanRadius))
				.Where(a =>
				{
					var condition = a.TraitsImplementing<ExternalCondition>()
						.FirstOrDefault(t => t.Info.Condition == info.Condition && t.CanGrantCondition(self, a));

					if (a.Owner.IsAlliedWith(Owner) && condition != null)
						return true;
					
					return false;
				});
			
			return possibles.ToHashSet();
		}

		void ITick.Tick(Actor self)
		{
			Target = AutoTarget.TargetedActor;
			if (Target != NewTarget)
			{
				if (GetAllTargetableActors(self).Contains(Target))
				{
					
					NewTarget = Target;
				}
				

			}
			
			
		}
	}
}