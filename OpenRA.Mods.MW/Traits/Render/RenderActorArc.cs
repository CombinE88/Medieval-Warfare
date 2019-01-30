#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.MW.Graphics;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits
{
    [Desc("Draws an arc between a mindcontroller actor and all its victims",
        "or an actively mindcontrolled actor and it's controller.")]
    public class RenderActorArcInfo : ConditionalTraitInfo, IPlaceBuildingDecorationInfo
    {
        [Desc("Color of the arc.")]
        public readonly Color Color = Color.Red;

        public readonly bool UsePlayerColor = false;

        public readonly bool PlacementOnly = false;

        public readonly int Transparency = 255;

        public readonly HashSet<string> Actors;

        [Desc("Relative offset from the actor's center position where the arc should start.")]
        public readonly WVec Offset = new WVec(0, 0, 0);

        [Desc("Relative offset from the arget actor's center position where the arc should end.")]
        public readonly WVec TargetOffset = new WVec(0, 0, 0);

        [Desc("The angle of the arc.")]
        public readonly WAngle Angle = new WAngle(64);

        [Desc("The width of the arc.")]
        public readonly WDist Width = new WDist(43);

        [Desc("Controls how fine-grained the resulting arc should be.")]
        public readonly int QuantizedSegments = 16;

        [Desc("Equivalent to sequence ZOffset. Controls Z sorting.")]
        public readonly int ZOffset = 0;

        [Desc("How far is the distance to find actors")]
        public readonly WDist Distance = new WDist(5);

        [Desc("How far is the extrea to find actors out of range wich need a range bonus")]
        public readonly WDist ExtraSearchDistance = new WDist(5);

        [Desc("Stances of players which will be able to see the circle.",
            "Valid values are combinations of `None`, `Ally`, `Enemy` and `Neutral`.")]
        public readonly Stance ValidStances = Stance.Ally;

        public IEnumerable<IRenderable> Render(WorldRenderer wr, World w, ActorInfo ai, WPos centerPosition)
        {
            var findactors = w.FindActorsInCircle(centerPosition, Distance + ExtraSearchDistance)
            .Where(a =>
            {
                if (a.IsDead || !a.IsInWorld)
                    return false;

                if (!Actors.Contains(a.Info.Name))
                    return false;

                if (CellDistanceBetweenCenterpositions(a.CenterPosition, centerPosition) <= CellLengthOfDistance(Distance))
                    return true;

                var cells = w.Map.FindTilesInCircle(w.Map.CellContaining(centerPosition), CellLengthOfDistance(ExtraSearchDistance))
                    .Where(c => w.WorldActor.Trait<ResourceLayer>().GetResourceDensity(c) > 0 && w.WorldActor.Trait<ResourceLayer>().GetRenderedResource(c) != null)
                    .Where(ce => ce.X <= Math.Max(w.Map.CellContaining(centerPosition).X, a.Location.X) && ce.X >= Math.Min(w.Map.CellContaining(centerPosition).X, a.Location.X)
                    && ce.Y <= Math.Max(w.Map.CellContaining(centerPosition).Y, a.Location.Y) && ce.Y >= Math.Min(w.Map.CellContaining(centerPosition).Y, a.Location.Y));

                if (cells.Any())
                {
                    var cell = cells.MinByOrDefault(c => (w.Map.CellContaining(centerPosition) - c).LengthSquared);

                    var extradistance = CellDistanceBetweenCenterpositions(w.Map.CenterOfCell(cell), a.CenterPosition);

                    if (CellDistanceBetweenCenterpositions(a.CenterPosition, centerPosition) <= CellLengthOfDistance(Distance) + extradistance)
                        return true;
                }

                return false;
            }).ToList();

            if (findactors.Any())
            {
                foreach (var act in findactors)
                {
                    yield return new ArcRenderable(
                        centerPosition + Offset,
                        act.CenterPosition + TargetOffset,
                        ZOffset, Angle, Color, Width, QuantizedSegments);
                }
            }
        }

        public int CellDistanceBetweenCenterpositions(WPos c1, WPos c2)
        {
            return (int)Math.Round((c1 - c2).Length / 1024.0);
        }

        public int CellLengthOfDistance(WDist w1)
        {
            return (int)Math.Round(w1.Length / 1024.0);
        }

        public override object Create(ActorInitializer init) { return new RenderActorArc(this); }
    }

    public class RenderActorArc
        : ConditionalTrait<RenderActorArcInfo>, INotifyRemovedFromWorld, IRenderAboveShroud, INotifySold, INotifyActorDisposing, INotifyCreated
    {
        readonly RenderActorArcInfo info;

        List<Actor> arcvalids = new List<Actor>();

        public RenderActorArc(RenderActorArcInfo info) : base(info)
        {
            this.info = info;
        }

        public List<Actor> FindActorsAround(Actor self)
        {
            return self.World.FindActorsInCircle(self.CenterPosition, info.Distance + Info.ExtraSearchDistance)
            .Where(a =>
            {
                if (a.IsDead || !a.IsInWorld)
                    return false;

                if (!Info.Actors.Contains(a.Info.Name))
                    return false;

                if (CellDistanceBetweenCenterpositions(a.CenterPosition, self.CenterPosition) <= (CellLengthOfDistance(Info.Distance) + 1))
                    return true;

                var cells = self.World.Map.FindTilesInCircle(self.World.Map.CellContaining(self.CenterPosition), CellLengthOfDistance(Info.ExtraSearchDistance))
                    .Where(c => self.World.WorldActor.Trait<ResourceLayer>().GetResourceDensity(c) > 0
                    && self.World.WorldActor.Trait<ResourceLayer>().GetRenderedResource(c) != null)
                    .Where(ce => ce.X <= Math.Max(self.Location.X, a.Location.X) && ce.X >= Math.Min(self.Location.X, a.Location.X)
                    && ce.Y <= Math.Max(self.Location.Y, a.Location.Y) && ce.Y >= Math.Min(self.Location.Y, a.Location.Y));

                if (cells.Any())
                {
                    var cell = cells.MinByOrDefault(c => (self.World.Map.CellContaining(self.CenterPosition) - c).LengthSquared);

                    var extradistance = CellDistanceBetweenCenterpositions(self.World.Map.CenterOfCell(cell), a.CenterPosition);
                    var alloweddistance = CellLengthOfDistance(Info.Distance) + extradistance + 1;
                    var distbetweenselfandfound = CellDistanceBetweenCenterpositions(a.CenterPosition, self.CenterPosition);

                    if (distbetweenselfandfound <= alloweddistance)
                        return true;
                }

                return false;
            }).ToList();
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

        public int CellDistanceBetweenCenterpositions(WPos c1, WPos c2)
        {
            return (int)Math.Round((c1 - c2).Length / 1024.0);
        }

        public int CellLengthOfDistance(WDist w1)
        {
            return (int)Math.Round(w1.Length / 1024.0);
        }

        void INotifyCreated.Created(Actor self)
        {
            if (Info.PlacementOnly)
                return;
            arcvalids = FindActorsAround(self);
        }

        // TODO reimplement me using conditions
        /*void INotifyBuildComplete.BuildingComplete(Actor self)
        {
            if (Info.PlacementOnly)
                return;

            if (arcvalids.Any())
            {
                foreach (var act in arcvalids)
                {
                    if (act.Info.HasTraitInfo<ValidPreyTargetInfo>())
                        act.TraitOrDefault<ValidPreyTarget>().AddSelf(self);
                }
            }
        }*/

        IEnumerable<IRenderable> IRenderAboveShroud.RenderAboveShroud(Actor self, WorldRenderer wr)
        {
            if (IsTraitDisabled || Info.PlacementOnly || !self.Owner.IsAlliedWith(self.World.RenderPlayer))
                yield break;

            var color = Color.FromArgb(info.Transparency, info.UsePlayerColor ? self.Owner.Color.RGB : info.Color);

            if (arcvalids.Any() && self.IsInWorld && !self.IsDead)
            {
                foreach (var s in arcvalids)
                    yield return new ArcRenderable(
                        self.CenterPosition + info.Offset,
                        s.CenterPosition + info.TargetOffset,
                        info.ZOffset, info.Angle, color, info.Width, info.QuantizedSegments);
            }

            yield break;
        }

        void Removeself(Actor self)
        {
            foreach (var act in arcvalids)
            {
                if (act.Info.HasTraitInfo<ValidPreyTargetInfo>() && act.TraitOrDefault<ValidPreyTarget>().Actors.Contains(self))
                    act.TraitOrDefault<ValidPreyTarget>().RemoveSelf(self);
            }
        }

        void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
        {
            if (Info.PlacementOnly)
                return;
            foreach (var act in arcvalids)
                Removeself(self);
        }

        void INotifyActorDisposing.Disposing(Actor self)
        {
            if (Info.PlacementOnly)
                return;
            foreach (var act in arcvalids)
                Removeself(self);
        }

        void INotifySold.Selling(Actor self)
        {
            if (Info.PlacementOnly)
                return;
            foreach (var act in arcvalids)
                Removeself(self);
        }

        void INotifySold.Sold(Actor self)
        {
            if (Info.PlacementOnly)
                return;
            foreach (var act in arcvalids)
                Removeself(self);
        }

        bool IRenderAboveShroud.SpatiallyPartitionable { get { return false; } }
    }
}