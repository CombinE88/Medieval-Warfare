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
using OpenRA.Mods.MW.Traits;
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
        public readonly WDist Distance = new WDist(0);

        [Desc("Stances of players which will be able to see the circle.",
            "Valid values are combinations of `None`, `Ally`, `Enemy` and `Neutral`.")]
        public readonly Stance ValidStances = Stance.Ally;

        public IEnumerable<IRenderable> Render(WorldRenderer wr, World w, ActorInfo ai, WPos centerPosition)
        {
            var findactors = w.FindActorsInCircle(centerPosition, Distance)
            .Where(a =>
            {
                if (a.IsDead || !a.IsInWorld)
                    return false;

                if (Actors.Contains(a.Info.Name))
                    return true;

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

        public override object Create(ActorInitializer init) { return new RenderActorArc(init.Self, this); }
    }

    public class RenderActorArc : ConditionalTrait<RenderActorArcInfo>, INotifyRemovedFromWorld, IRenderAboveShroud, INotifyBuildComplete, INotifySold, INotifyActorDisposing
    {
        readonly RenderActorArcInfo info;
        readonly Actor self;

        List<Actor> arcvalids = new List<Actor>();

        public RenderActorArc(Actor self, RenderActorArcInfo info) : base(info)
        {
            this.info = info;
            this.self = self;
        }

        public List<Actor> FindActorsAround(Actor self)
        {
            return self.World.FindActorsInCircle(self.CenterPosition, info.Distance)
            .Where(a =>
            {
                if (a.IsDead || !a.IsInWorld)
                    return false;

                if (Info.Actors.Contains(a.Info.Name))
                    return true;

                return false;
            }).ToList();
        }

        void INotifyBuildComplete.BuildingComplete(Actor self)
        {
            if (Info.PlacementOnly)
                return;
            arcvalids = FindActorsAround(self);

            if (arcvalids.Any())
            {
                foreach (var act in arcvalids)
                {
                    if (act.Info.HasTraitInfo<ValidPreyTargetInfo>())
                        act.TraitOrDefault<ValidPreyTarget>().Actors.Add(self);
                }
            }
        }

        bool Visible
        {
            get
            {
                var p = self.World.RenderPlayer;
                return p == null || Info.ValidStances.HasStance(self.Owner.Stances[p]) || (p.Spectating && !p.NonCombatant);
            }
        }

        IEnumerable<IRenderable> IRenderAboveShroud.RenderAboveShroud(Actor self, WorldRenderer wr)
        {
            if (IsTraitDisabled || Info.PlacementOnly)
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
                    act.TraitOrDefault<ValidPreyTarget>().Actors.Remove(self);
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