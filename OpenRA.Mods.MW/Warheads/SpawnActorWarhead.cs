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
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.MW.Activities;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Warheads
{
    [Desc("Spawn actors upon explosion.")]
    public class SpawnActorWarhead : WarheadAS
    {
        [Desc("The cell range to try placing the actors within.")]
        public readonly int Range = 10;

        [Desc("Actors to spawn.")]
        public readonly string[] Actors = { };

        [Desc("Try to parachute the actors. When unset, actors will just fall down visually using FallRate."
            + " Requires the Parachutable trait on all actors if set.")]
        public readonly bool Paradrop = false;

        public readonly int FallRate = 130;

        [Desc("Map player to give the actors to. Defaults to the firer.")]
        public readonly string Owner = null;

        public override void DoImpact(Target target, Actor firedBy, IEnumerable<int> damageModifiers)
        {
            var map = firedBy.World.Map;
            var targetCell = map.CellContaining(target.CenterPosition);

            if (!IsValidImpact(target.CenterPosition, firedBy))
                return;

            var targetCells = map.FindTilesInCircle(targetCell, Range);
            var cell = targetCells.GetEnumerator();

            foreach (var a in Actors)
            {
                var placed = false;
                var building = false;
                var spawn = true;
                var td = new TypeDictionary();
                if (Owner == null)
                    td.Add(new OwnerInit(firedBy.Owner));
                else
                    td.Add(new OwnerInit(firedBy.World.Players.First(p => p.InternalName == Owner)));

                if (firedBy.World.Map.Rules.Actors[a].HasTraitInfo<BuildingInfo>())
                {
                    var blng = firedBy.World.Map.Rules.Actors[a].TraitInfo<BuildingInfo>();
                    var cellone = RandomWalk(firedBy.World.Map.CellContaining(target.CenterPosition), firedBy.World.SharedRandom)
                        .Take(Range)
                        .SkipWhile(p => !firedBy.World.CanPlaceBuilding(p , firedBy.World.Map.Rules.Actors[a], blng, null))

                        .Cast<CPos?>().FirstOrDefault();

                    if (cellone == null)
                    {
                        spawn = false;
                    }
                    else
                    {
                        td.Add(new LocationInit(cellone.Value));
                        td.Add(new CenterPositionInit(firedBy.World.Map.CenterOfCell(cellone.Value)));
                    }

                    building = true;
                }

                if (!building)
                {
                    var unit = firedBy.World.CreateActor(false, a.ToLowerInvariant(), td);

                    while (cell.MoveNext())
                    {
                        if (unit.Trait<IPositionable>().CanEnterCell(cell.Current))
                        {
                            var cellpos = firedBy.World.Map.CenterOfCell(cell.Current);
                            var pos = cellpos.Z < target.CenterPosition.Z
                                ? new WPos(cellpos.X, cellpos.Y, target.CenterPosition.Z)
                                : cellpos;
                            firedBy.World.AddFrameEndTask(w =>
                            {
                                w.Add(unit);
                                if (Paradrop)
                                    unit.QueueActivity(new Parachute(unit, pos));
                                else
                                    unit.QueueActivity(new FallDown(unit, pos, FallRate));
                            });
                            placed = true;
                            break;
                        }
                    }

                    if (!placed)
                        unit.Dispose();
                }
                else if (spawn)
                {
                    firedBy.World.AddFrameEndTask(w =>
                    {
                        firedBy.World.CreateActor(true, a.ToLowerInvariant(), td);
                    });
                }
            }
        }

        public static IEnumerable<CPos> RandomWalk(CPos p, MersenneTwister r)
        {
            for (;;)
            {
                var dx = r.Next(-1, 2);
                var dy = r.Next(-1, 2);

                if (dx == 0 && dy == 0)
                    continue;

                p += new CVec(dx, dy);
                yield return p;
            }
        }
    }
}
