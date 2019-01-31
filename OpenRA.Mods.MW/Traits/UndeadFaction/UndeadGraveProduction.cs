#region Copyright & License Information
/*
 * Copyright 2017-2017 The MW Developers)
 * This file is part of Medieval Warfare, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Drawing;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits
{
    [Desc("A actor has to enter the building before the unit spawns.")]
    public class UndeadGraveProductionInfo : ProductionInfo, Requires<ExitInfo>
    {
        public readonly string ReadyAudio = "UnitReady";

        public override object Create(ActorInitializer init) { return new UndeadGraveProduction(init, this); }
    }

    class UndeadGraveProduction : Production
    {
        readonly UndeadGraveProductionInfo info;
        readonly Lazy<RallyPoint> rp;

        public UndeadGraveProduction(ActorInitializer init, UndeadGraveProductionInfo info)
            : base(init, info)
        {
            rp = Exts.Lazy(() => init.Self.IsDead ? null : init.Self.TraitOrDefault<RallyPoint>());
            this.info = info;
        }

        public override bool Produce(Actor self, ActorInfo producee, string productionType, TypeDictionary inits)
        {
            var newexit = self.Info.TraitInfos<ExitInfo>().FirstOrDefault();

            var devMode = self.Owner.PlayerActor.TraitOrDefault<DeveloperMode>();

            if (devMode != null && devMode.FastBuild)
            {
                self.World.AddFrameEndTask(ww => DoProduction(self, producee, newexit, productionType, inits));
                Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.ReadyAudio,
                    self.Owner.Faction.InternalName);
                return true;
            }

            self.World.AddFrameEndTask(w => DoProduction(self, producee, newexit, productionType, inits));
            Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.ReadyAudio,
                self.Owner.Faction.InternalName);

            return true;
        }

        public override void DoProduction(Actor self, ActorInfo producee, ExitInfo exitinfo, string factionVariant, TypeDictionary init)
        {
            var exit = CPos.Zero;
            var exitLocation = CPos.Zero;
            var target = Target.Invalid;

            var bi = producee.TraitInfoOrDefault<BuildableInfo>();
            if (bi != null && bi.ForceFaction != null)
                factionVariant = bi.ForceFaction;

            var td = new TypeDictionary
            {
                new OwnerInit(self.Owner),
            };

            if (self.OccupiesSpace != null)
            {
                exit = self.Location + exitinfo.ExitCell;
                var spawn = self.CenterPosition + exitinfo.SpawnOffset;
                var to = self.World.Map.CenterOfCell(exit);

                if (producee.HasTraitInfo<IPositionableInfo>())
                {
                    var cell = Util.RandomWalk(self.Location, self.World.SharedRandom)
                        .Take(2)
                        .SkipWhile(p => !producee.TraitInfo<IPositionableInfo>().CanEnterCell(self.World, self, p))
                        .Cast<CPos?>().FirstOrDefault();

                    if (cell != null)
                        spawn = self.World.Map.CenterOfCell(cell.Value);
                }
                else if (producee.HasTraitInfo<MobileInfo>())
                {
                    var cell = Util.RandomWalk(self.Location, self.World.SharedRandom)
                        .Take(2)
                        .SkipWhile(p => !producee.TraitInfo<MobileInfo>().CanEnterCell(self.World, self, p))
                        .Cast<CPos?>().FirstOrDefault();

                    if (cell != null)
                        spawn = self.World.Map.CenterOfCell(cell.Value);
                }

                var initialFacing = exitinfo.Facing;
                if (exitinfo.Facing < 0)
                {
                    var delta = to - spawn;
                    if (delta.HorizontalLengthSquared == 0)
                    {
                        var fi = producee.TraitInfoOrDefault<IFacingInfo>();
                        initialFacing = fi != null ? fi.GetInitialFacing() : 0;
                    }
                    else
                        initialFacing = delta.Yaw.Facing;
                }

                exitLocation = rp.Value != null ? rp.Value.Location : exit;
                target = Target.FromCell(self.World, exitLocation);

                td.Add(new LocationInit(exit));
                td.Add(new CenterPositionInit(spawn));
                td.Add(new FacingInit(initialFacing));
            }

            self.World.AddFrameEndTask(w =>
            {
                if (factionVariant != null)
                    td.Add(new FactionInit(factionVariant));

                var newUnit = self.World.CreateActor(producee.Name, td);

                if (newUnit.Info.HasTraitInfo<WithMakeAnimationInfo>())
                {
                    var move = newUnit.TraitOrDefault<IMove>();
                    if (move != null)
                    {
                        if (exitinfo.MoveIntoWorld)
                        {
                            if (exitinfo.ExitDelay > 0)
                                newUnit.QueueActivity(new Wait(exitinfo.ExitDelay, false));

                            newUnit.Trait<WithMakeAnimation>().Forward(newUnit, () =>
                            {
                                newUnit.QueueActivity(move.MoveIntoWorld(newUnit, exit));
                                newUnit.QueueActivity(new AttackMoveActivity(
                                    newUnit, move.MoveTo(exitLocation, 1)));

                                newUnit.SetTargetLine(target, rp.Value != null ? Color.Red : Color.Green, false);

                                if (!self.IsDead)
                                    foreach (var t in self.TraitsImplementing<INotifyProduction>())
                                        t.UnitProduced(self, newUnit, exit);

                                var notifyOthers = self.World.ActorsWithTrait<INotifyOtherProduction>();
                                foreach (var notify in notifyOthers)
                                    notify.Trait.UnitProducedByOther(notify.Actor, self, newUnit, producee.Name, init);
                            });
                        }
                    }
                }
                else
                {
                    var move = newUnit.TraitOrDefault<IMove>();
                    if (move != null)
                    {
                        if (exitinfo.MoveIntoWorld)
                        {
                            if (exitinfo.ExitDelay > 0)
                                newUnit.QueueActivity(new Wait(exitinfo.ExitDelay, false));

                            newUnit.Trait<WithMakeAnimation>().Forward(newUnit, () =>
                            {
                                newUnit.QueueActivity(move.MoveIntoWorld(newUnit, exit));
                                newUnit.QueueActivity(new AttackMoveActivity(
                                    newUnit, move.MoveTo(exitLocation, 1)));
                            });
                        }
                    }

                    newUnit.SetTargetLine(target, rp.Value != null ? Color.Red : Color.Green, false);

                    if (!self.IsDead)
                        foreach (var t in self.TraitsImplementing<INotifyProduction>())
                            t.UnitProduced(self, newUnit, exit);

                    var notifyOthers = self.World.ActorsWithTrait<INotifyOtherProduction>();
                    foreach (var notify in notifyOthers)
                        notify.Trait.UnitProducedByOther(notify.Actor, self, newUnit, producee.Name, init);
                }
            });
        }
    }
}