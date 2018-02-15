using OpenRA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.GameRules;
using System;
using System.Linq;

namespace OpenRA.Mods.MW.Traits
{
    public enum ExplosionType { Footprint, CenterPosition }

    [Desc("This actor explodes when killed.")]
    public class WithPermanentExplosionInfo : ConditionalTraitInfo
    {
        [WeaponReference, FieldLoader.Require, Desc("Default weapon to use for explosion if ammo/payload is loaded.")]
        public readonly string Weapon = null;

        [Desc("Chance that this actor will explode at all.")]
        public readonly int Chance = 100;

        [Desc("How Long between Explosions.")]
        public readonly int Delay = 25;

        [Desc("Possible values are CenterPosition (explosion at the actors' center) and ",
            "Footprint (explosion on each occupied cell).")]
        public readonly ExplosionType Type = ExplosionType.CenterPosition;

        public WeaponInfo WeaponInfo { get; private set; }
        public WeaponInfo EmptyWeaponInfo { get; private set; }

        public override object Create(ActorInitializer init) { return new WithPermanentExplosion(this, init.Self); }
        public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
        {
            if (!string.IsNullOrEmpty(Weapon))
            {
                WeaponInfo weapon;
                var weaponToLower = Weapon.ToLowerInvariant();
                if (!rules.Weapons.TryGetValue(weaponToLower, out weapon))
                {
                    throw new YamlException("Weapons Ruleset does not contain an entry '{0}'".F(weaponToLower));
                }

                WeaponInfo = weapon;
            }

            base.RulesetLoaded(rules, ai);
        }
    }
    public class WithPermanentExplosion : ConditionalTrait<WithPermanentExplosionInfo>, INotifyCreated, ITick
    {
        BuildingInfo buildingInfo;
        private int _delay;

        public WithPermanentExplosion(WithPermanentExplosionInfo info, Actor self)
            : base(info)
        {
            _delay = Info.Delay;
        }

        public void Tick(Actor self)
        {
            if (!IsTraitDisabled)
                _delay--;

            if (_delay <= 0)
                ExplosionHappaning(self);
        }

        void INotifyCreated.Created(Actor self)
        {
            buildingInfo = self.Info.TraitInfoOrDefault<BuildingInfo>();
        }

        public void ExplosionHappaning(Actor self)
        {
            _delay = Info.Delay;

            if (self.World.SharedRandom.Next(100) > Info.Chance)
            {
                return;
            }

            var weapon = Info.WeaponInfo;
            if (weapon == null)
                return;

            if (weapon.Report != null && weapon.Report.Any())
                Game.Sound.Play(SoundType.World, weapon.Report.Random(self.World.SharedRandom), self.CenterPosition);

            if (Info.Type == ExplosionType.Footprint && buildingInfo != null)
            {
                var cells = buildingInfo.UnpathableTiles(self.Location);
                foreach (var c in cells)
                    weapon.Impact(Target.FromPos(self.World.Map.CenterOfCell(c)), self, Enumerable.Empty<int>());

                return;
            }

            // Use .FromPos since this actor is killed. Cannot use Target.FromActor
            weapon.Impact(Target.FromPos(self.CenterPosition), self, Enumerable.Empty<int>());
        }

    }
}
