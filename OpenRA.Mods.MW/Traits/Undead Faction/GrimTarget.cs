using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
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
        public readonly int maxDistance = 5;

        public readonly int DamageMultiplier = 100;
        public readonly int FirePowerMultiplier = 100;
        public readonly int SpeedMultiplier = 100;
        public readonly int GivesExpMultiplier = 100;

        [Desc("Palette to use when rendering the overlay")]
        [PaletteReference]
        public readonly string Palette = "invuln";

        public object Create(ActorInitializer init) { return new GrimTarget(init.Self, this); }
    }

    public class GrimTarget : ITick, INotifyKilled, INotifyCreated, ISpeedModifier, IGivesExperienceModifier, IFirepowerModifier, IDamageModifier, IRenderModifier
    {
        public GrimTargetInfo info;
        //private Actor self;
        ConditionManager conditionManager;
        int token = ConditionManager.InvalidConditionToken;

        public Actor Grim;
        public bool Reanimated;

        public GrimTarget(Actor self, GrimTargetInfo info)
        {
            this.info = info;
            //this.self = self;

        }

        int ISpeedModifier.GetSpeedModifier() { return !Reanimated ? 100 : info.SpeedMultiplier; }
        int IGivesExperienceModifier.GetGivesExperienceModifier() { return !Reanimated ? 100 : info.GivesExpMultiplier; }
        int IFirepowerModifier.GetFirepowerModifier() { return !Reanimated ? 100 : info.FirePowerMultiplier; }
        int IDamageModifier.GetDamageModifier(Actor attacker, Damage damage) { return !Reanimated ? 100 : info.DamageMultiplier; }

        public IEnumerable<IRenderable> ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
        {
            if (!Reanimated)
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
            if (!Reanimated)
                return;

            if (Reanimated && Grim == null)
                self.Kill(self);
            else if (Reanimated && Grim != null && (Grim.IsDead || !Grim.IsInWorld))
                self.Kill(self);
        }

        public void Killed(Actor self, AttackInfo e)
        {

            if (!Reanimated && e.Attacker.Info.HasTraitInfo<GrimReanimationInfo>() && e.Attacker.IsInWorld && !e.Attacker.IsDead)
            {
                var GrimTrait = e.Attacker.Trait<GrimReanimation>();
                if (GrimTrait.Actor == null)
                {
                    var unit = self.World.CreateActor(true, self.Info.Name, new TypeDictionary
                    {
                        new LocationInit(self.Location),
                        new OwnerInit(e.Attacker.Owner),
                        new FacingInit(self.Trait<IFacing>().Facing)
                    });
                    unit.Trait<GrimTarget>().Reanimated = true;
                    unit.Trait<GrimTarget>().Grim = e.Attacker;
                    GrimTrait.Actor = unit;
                }
            }
        }

        public void GrantCondition(Actor self)
        {
            if (Reanimated && self.IsInWorld && !self.IsDead)
            {
                token = conditionManager.GrantCondition(self, info.GrantCondition);
            }
        }

        void INotifyCreated.Created(Actor self)
        {
            conditionManager = self.TraitOrDefault<ConditionManager>();
            if (Reanimated && token == ConditionManager.InvalidConditionToken && conditionManager != null)
            {
                GrantCondition(self);
            }

            if (Reanimated && Grim != null && Grim.IsInWorld && !Grim.IsDead && self.Info.HasTraitInfo<IMoveInfo>())
            {
                if (!self.IsDead && self.IsInWorld && (self.Location - Grim.Location).LengthSquared >
                    WDist.FromCells(info.maxDistance).LengthSquared)
                {
                    self.CancelActivity();
                    self.QueueActivity(self.Trait<IMove>().MoveTo(Grim.Location, 2));
                }
            }

        }

        IEnumerable<Rectangle> IRenderModifier.ModifyScreenBounds(Actor self, WorldRenderer wr, IEnumerable<Rectangle> bounds)
        {
            return bounds;
        }
    }
}
