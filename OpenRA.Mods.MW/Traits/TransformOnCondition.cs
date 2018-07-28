using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits
{
    [Desc("Reloads an AmmoPool trait externally based on the upgrade criteria.")]
    public class TransformOnConditionInfo : ConditionalTraitInfo
    {
        [FieldLoader.Require]
        public readonly string ReadyAudio = "ConstructionComplete";
        [Desc("The Actors name you want to transform to.")]
        public readonly string IntoActor = null;
        public readonly bool SkipMakeAnims = true;
        public readonly CVec Offset = CVec.Zero;
        public readonly int Facing = -1;
        public readonly string[] Sounds = { };
        public readonly bool ForceHealthPercentage = true;

        public override object Create(ActorInitializer init) { return new TransformOnCondition(init, this); }
    }

    public class TransformOnCondition : ConditionalTrait<TransformOnConditionInfo>, ITick, INotifyRemovedFromWorld
    {
        readonly TransformOnConditionInfo info;
        readonly string faction;
        bool enabled = false;
        bool selected;
        int? controlgroup;
        Health health;
        Cargo cargo;
        int facong;
        TypeDictionary init;

        public TransformOnCondition(ActorInitializer init, TransformOnConditionInfo info)
            : base(info)
        {
            this.info = info;
            faction = init.Contains<FactionInit>() ? init.Get<FactionInit, string>() : init.Self.Owner.Faction.InternalName;
        }

        void ITick.Tick(Actor self)
        {
            var devMode = self.Owner.PlayerActor.TraitOrDefault<DeveloperMode>();

            if (!IsTraitDisabled || (devMode != null && devMode.FastBuild))
            {
                if (self.IsDead)
                    return;

                foreach (var nt in self.TraitsImplementing<INotifyTransform>())
                    nt.OnTransform(self);

                selected = self.World.Selection.Contains(self);
                controlgroup = self.World.Selection.GetControlGroupForActor(self);

                health = self.TraitOrDefault<Health>();
                cargo = self.TraitOrDefault<Cargo>();

                enabled = true;

                facong = info.Facing;

                if (info.Facing == -1)
                {
                    var face = self.TraitOrDefault<IFacing>();
                    if (face != null && info.Facing < 0)
                        facong = face.Facing;
                }

                init = new TypeDictionary
                    {
                    new LocationInit(self.Location + info.Offset),
                    new OwnerInit(self.Owner),
                    new FacingInit(facong),
                    };

                if (info.SkipMakeAnims)
                    init.Add(new SkipMakeAnimsInit());

                if (faction != null)
                    init.Add(new FactionInit(faction));

                if (health != null)
                {
                    var newHP = (health.HP * 100) / health.MaxHP;
                    init.Add(new HealthInit(newHP));
                }

                if (cargo != null)
                    init.Add(new RuntimeCargoInit(cargo.Passengers.ToArray()));

                Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.ReadyAudio,
                    self.Owner.Faction.InternalName);

                self.Dispose();
            }
        }

        void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
        {
            if (enabled)
                self.World.AddFrameEndTask(w =>
                {
                    var a = w.CreateActor(info.IntoActor, init);

                    if (selected)
                        w.Selection.Add(w, a);
                    if (controlgroup.HasValue)
                        w.Selection.AddToControlGroup(a, controlgroup.Value);
                });
        }
    }
}