using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits
{
    [Desc("Is Unit a Peasant (adds a count of 1 to the PlayerCivilisation).")]
    public class GrimTargetInfo : ITraitInfo
    {
        [Desc("Condition grant to itself while when got reanimated")]
        public readonly string GrantCondition = null;

        [Desc("Max distance in Cells wich the puppet can go")]
        public readonly int MaxDistance = 5;

        public readonly int DamageMultiplier = 100;
        public readonly int FirePowerMultiplier = 100;
        public readonly int SpeedMultiplier = 100;
        public readonly int GivesExpMultiplier = 100;

        [Desc("Palette to use when rendering the overlay")]
        [PaletteReference]
        public readonly string Palette = "invuln";

        public object Create(ActorInitializer init) { return new GrimTarget(init.Self, this); }
    }

    public class GrimTarget : ITick, INotifyCreated, ISpeedModifier, IGivesExperienceModifier, IFirepowerModifier, IDamageModifier, INotifyKilled
    {
        private GrimTargetInfo info;
        ConditionManager conditionManager;
        int token = ConditionManager.InvalidConditionToken;

        private Actor grim;
        private bool reanimated;

        public GrimTarget(Actor self, GrimTargetInfo info)
        {
            this.info = info;
        }

        int ISpeedModifier.GetSpeedModifier() { return !reanimated ? 100 : info.SpeedMultiplier; }
        int IGivesExperienceModifier.GetGivesExperienceModifier() { return !reanimated ? 100 : info.GivesExpMultiplier; }
        int IFirepowerModifier.GetFirepowerModifier() { return !reanimated ? 100 : info.FirePowerMultiplier; }
        int IDamageModifier.GetDamageModifier(Actor attacker, Damage damage) { return !reanimated ? 100 : info.DamageMultiplier; }

        public IEnumerable<IRenderable> ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
        {
            if (!reanimated)
                return r;

            return ModifiedRender(self, wr, r);
        }

        IEnumerable<IRenderable> ModifiedRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
        {
            foreach (var a in r)
            {
                yield return a;

                if (!a.IsDecoration)
                    yield return a.WithPalette(wr.Palette(info.Palette))
                        .WithZOffset(a.ZOffset + 1)
                        .AsDecoration();
            }
        }

        void ITick.Tick(Actor self)
        {
            if (!reanimated)
                return;

            if (reanimated && grim == null)
                self.Kill(self);
            else if (reanimated && grim != null && (grim.IsDead || !grim.IsInWorld))
                self.Kill(self);
        }

        public void Killed(Actor self, AttackInfo e)
        {
            if (!reanimated && e.Attacker.Info.HasTraitInfo<GrimReanimationInfo>() && e.Attacker.IsInWorld && !e.Attacker.IsDead)
            {
                var grimTrait = e.Attacker.Trait<GrimReanimation>();
                if (grimTrait.Actor == null)
                {
                    var unit = self.World.CreateActor(true, self.Info.Name, new TypeDictionary
                    {
                        new LocationInit(self.Location),
                        new OwnerInit(e.Attacker.Owner),
                        new FacingInit(self.Trait<IFacing>().Facing)
                    });
                    unit.Trait<GrimTarget>().reanimated = true;
                    unit.Trait<GrimTarget>().grim = e.Attacker;
                    grimTrait.Actor = unit;
                }
            }
        }

        public void GrantCondition(Actor self)
        {
            if (reanimated && self.IsInWorld && !self.IsDead)
            {
                token = conditionManager.GrantCondition(self, info.GrantCondition);
            }
        }

        void INotifyCreated.Created(Actor self)
        {
            conditionManager = self.TraitOrDefault<ConditionManager>();
            if (reanimated && token == ConditionManager.InvalidConditionToken && conditionManager != null)
            {
                GrantCondition(self);
            }

            if (reanimated && grim != null && grim.IsInWorld && !grim.IsDead && self.Info.HasTraitInfo<IMoveInfo>())
            {
                if (!self.IsDead && self.IsInWorld && (self.Location - grim.Location).LengthSquared >
                    WDist.FromCells(info.MaxDistance).LengthSquared)
                {
                    self.CancelActivity();
                    self.QueueActivity(self.Trait<IMove>().MoveTo(grim.Location, 2));
                }
            }
        }
    }
}
