using System;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits
{
	[Desc("Reloads an AmmoPool trait externally based on the upgrade criteria.")]
	public class TransformOnConditionInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("The Actors name you want to transform to.")]
		public readonly string IntoActor = null;
		public readonly bool SkipMakeAnims = true;
		public readonly CVec Offset = CVec.Zero;
		public readonly int Facing = -1;
		public readonly bool ForceHealthPercentage = true;


		public override object Create(ActorInitializer init) { return new TransformOnCondition(init, this); }
	}

	public class TransformOnCondition : ConditionalTrait<TransformOnConditionInfo>, ITick
	{
		readonly TransformOnConditionInfo info;
		readonly string faction;

		
		
		public TransformOnCondition(ActorInitializer init, TransformOnConditionInfo info)
			: base(info)
		{
			this.info = info;
			faction = init.Contains<FactionInit>() ? init.Get<FactionInit, string>() : init.Self.Owner.Faction.InternalName;
		}


		void ITick.Tick(Actor self)
		{
			

			if (IsTraitDisabled)
				return;
			
			self.World.AddFrameEndTask(w =>
			{
				if (self.IsDead)
					return;

				foreach (var nt in self.TraitsImplementing<INotifyTransform>())
					nt.OnTransform(self);

				var selected = w.Selection.Contains(self);
				var controlgroup = w.Selection.GetControlGroupForActor(self);

				int facong;
				var face = self.TraitOrDefault<IFacing>();
				if (face != null && info.Facing < 0)
					facong = face.Facing;
				facong = info.Facing;
						
				
				var init = new TypeDictionary
				{
					new LocationInit(self.Location + info.Offset),
					new CenterPositionInit(self.CenterPosition),
					new OwnerInit(self.Owner),
					new FacingInit(facong),
				};

				if (info.SkipMakeAnims)
					init.Add(new SkipMakeAnimsInit());

				if (faction != null)
					init.Add(new FactionInit(faction));

				var health = self.TraitOrDefault<Health>();
				if (health != null && info.ForceHealthPercentage)
				{
					var newHP = (health.HP * 100) / health.MaxHP;
					init.Add(new HealthInit(newHP));
				}

				var cargo = self.TraitOrDefault<Cargo>();
				if (cargo != null)
					init.Add(new RuntimeCargoInit(cargo.Passengers.ToArray()));

				var a = w.CreateActor(info.IntoActor, init);
				foreach (var nt in self.TraitsImplementing<INotifyTransform>())
					nt.AfterTransform(a);

				if (selected)
					w.Selection.Add(w, a);
				if (controlgroup.HasValue)
					w.Selection.AddToControlGroup(a, controlgroup.Value);
				
				self.Dispose();
			});
		}

	}
}
