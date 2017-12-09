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
	public class DebugTransformOnConditionInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		public readonly string ReadyAudio = "ConstructionComplete";
		[Desc("The Actors name you want to transform to.")]
		public readonly string IntoActor = null;
		public readonly bool SkipMakeAnims = true;
		public readonly CVec Offset = CVec.Zero;
		public readonly int Facing = -1;
		public readonly bool ForceHealthPercentage = true;


		public override object Create(ActorInitializer init) { return new DebugTransformOnCondition(init, this); }
	}

	public class DebugTransformOnCondition : ConditionalTrait<DebugTransformOnConditionInfo>, ITick
	{
		readonly DebugTransformOnConditionInfo info;
		readonly string faction;

		
		
		public DebugTransformOnCondition(ActorInitializer init, DebugTransformOnConditionInfo info)
			: base(info)
		{
			this.info = info;
			faction = init.Contains<FactionInit>() ? init.Get<FactionInit, string>() : init.Self.Owner.Faction.InternalName;
		}

        void Transform(Actor self, String transact)
        {
            self.World.AddFrameEndTask(w =>
            {
                if (self.IsDead)
                    return;

                foreach (var nt in self.TraitsImplementing<INotifyTransform>())
                    nt.OnTransform(self);

                var selected = w.Selection.Contains(self);
                var controlgroup = w.Selection.GetControlGroupForActor(self);

                var facong = info.Facing;

                if (info.Facing == -1)
                {
                    var face = self.TraitOrDefault<IFacing>();
                    if (face != null && info.Facing < 0)
                        facong = face.Facing;
                }


                var init = new TypeDictionary
                    {
                    new LocationInit(self.Location + info.Offset),
                    new OwnerInit(self.Owner),
                    new FacingInit(facong),
                    };

                var health = self.TraitOrDefault<Health>();


                var cargo = self.TraitOrDefault<Cargo>();

                self.Dispose();

                if (info.SkipMakeAnims)
                    init.Add(new SkipMakeAnimsInit());

                if (faction != null)
                    init.Add(new FactionInit(faction));

                if (health != null && info.ForceHealthPercentage)
                {
                    var newHP = (health.HP * 100) / health.MaxHP;
                    init.Add(new HealthInit(newHP));
                }

                if (cargo != null)
                    init.Add(new RuntimeCargoInit(cargo.Passengers.ToArray()));


                Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.ReadyAudio,
                    self.Owner.Faction.InternalName);

                var a = w.CreateActor(transact, init);
                foreach (var nt in self.TraitsImplementing<INotifyTransform>())
                    nt.AfterTransform(a);

                if (selected)
                    w.Selection.Add(w, a);
                if (controlgroup.HasValue)
                    w.Selection.AddToControlGroup(a, controlgroup.Value);

            });
        }


		void ITick.Tick(Actor self)
		{

            var devMode = self.Owner.PlayerActor.TraitOrDefault<DeveloperMode>();

            if (!IsTraitDisabled || (devMode != null && devMode.FastBuild))
            {
                Transform(self, info.IntoActor);
            }
            else
            {
                return;
            }
		}

	}
}
