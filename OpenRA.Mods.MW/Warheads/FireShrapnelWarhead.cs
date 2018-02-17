#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Warheads
{
    public class FireShrapnelWarhead : WarheadAS, IRulesetLoaded<WeaponInfo>
    {
        [WeaponReference, FieldLoader.Require]
        [Desc("Has to be defined in weapons.yaml as well.")]
        public readonly string Weapon = null;

        [Desc("Amount of shrapnels thrown.")]
        public readonly int[] Amount = { 1 };

        [Desc("The percentage of aiming this shrapnel to a suitable target actor.")]
        public readonly int AimChance = 0;

        [Desc("What diplomatic stances can be targeted by the shrapnel.")]
        public readonly Stance AimTargetStances = Stance.Ally | Stance.Neutral | Stance.Enemy;

        [Desc("Allow this shrapnel to be thrown randomly when no targets found.")]
        public readonly bool ThrowWithoutTarget = true;

        [Desc("Should the shrapnel hit the direct target?")]
        public readonly bool AllowDirectHit = false;

        WeaponInfo weapon;

        public void RulesetLoaded(Ruleset rules, WeaponInfo info)
        {
            if (!rules.Weapons.TryGetValue(Weapon.ToLowerInvariant(), out weapon))
                throw new YamlException("Weapons Ruleset does not contain an entry '{0}'".F(Weapon.ToLowerInvariant()));
        }

        public override void DoImpact(Target target, Actor firedBy, IEnumerable<int> damageModifiers)
        {

            if (!IsValidImpact(target.CenterPosition, firedBy))
                return;

            var directActors = firedBy.World.FindActorsInCircle(target.CenterPosition, TargetSearchRadius);

            var availableTargetActors = firedBy.World.FindActorsInCircle(target.CenterPosition, weapon.Range)
                .Where(x => (AllowDirectHit || !directActors.Contains(x))
                    && weapon.IsValidAgainst(Target.FromActor(x), firedBy.World, firedBy)
                    && AimTargetStances.HasStance(firedBy.Owner.Stances[x.Owner]))
                .Shuffle(firedBy.World.SharedRandom);

            var targetActor = availableTargetActors.GetEnumerator();

            var amount = Amount.Length == 2
                    ? firedBy.World.SharedRandom.Next(Amount[0], Amount[1])
                    : Amount[0];

            for (var i = 0; i < amount; i++)
            {
                Target shrapnelTarget = Target.Invalid;

                if (firedBy.World.SharedRandom.Next(100) < AimChance && targetActor.MoveNext())
                    shrapnelTarget = Target.FromActor(targetActor.Current);

                if (ThrowWithoutTarget && shrapnelTarget.Type == TargetType.Invalid)
                {
                    var rotation = WRot.FromFacing(firedBy.World.SharedRandom.Next(1024));
                    var range = firedBy.World.SharedRandom.Next(weapon.MinRange.Length, weapon.Range.Length);
                    var targetpos = target.CenterPosition + new WVec(range, 0, 0).Rotate(rotation);
                    var tpos = Target.FromPos(new WPos(targetpos.X, targetpos.Y, firedBy.World.Map.CenterOfCell(firedBy.World.Map.CellContaining(targetpos)).Z));
                    if (weapon.IsValidAgainst(tpos, firedBy.World, firedBy))
                        shrapnelTarget = tpos;
                }

                if (shrapnelTarget.Type == TargetType.Invalid)
                    continue;

                var args = new ProjectileArgs
                {
                    Weapon = weapon,
                    Facing = (shrapnelTarget.CenterPosition - target.CenterPosition).Yaw.Facing,

                    DamageModifiers = !firedBy.IsDead ? firedBy.TraitsImplementing<IFirepowerModifier>()
                        .Select(a => a.GetFirepowerModifier()).ToArray() : new int[0],

                    InaccuracyModifiers = !firedBy.IsDead ? firedBy.TraitsImplementing<IInaccuracyModifier>()
                        .Select(a => a.GetInaccuracyModifier()).ToArray() : new int[0],

                    RangeModifiers = !firedBy.IsDead ? firedBy.TraitsImplementing<IRangeModifier>()
                        .Select(a => a.GetRangeModifier()).ToArray() : new int[0],

                    Source = target.CenterPosition,
                    SourceActor = firedBy,
                    GuidedTarget = shrapnelTarget,
                    PassiveTarget = shrapnelTarget.CenterPosition
                };

                if (args.Weapon.Projectile != null)
                {
                    var projectile = args.Weapon.Projectile.Create(args);
                    if (projectile != null)
                        firedBy.World.AddFrameEndTask(w => w.Add(projectile));

                    if (args.Weapon.Report != null && args.Weapon.Report.Any())
                        Game.Sound.Play(SoundType.World, args.Weapon.Report.Random(firedBy.World.SharedRandom), target.CenterPosition);
                }
            }
        }
    }
}
