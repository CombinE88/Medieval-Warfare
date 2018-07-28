using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.MW.Traits;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Activities
{
    // Assumes you have Minelayer on that unit
    public class PreyActivity : Activity, ITechTreePrerequisiteInfo
    {
        readonly AcolytePreyInfo info;
        readonly WithSpriteBody wsb;

        private Actor dockactor;
        private bool lockfacing;
        bool playanim;
        private bool endqueueonce;
        private Dock d;

        private int ticks;
        private ResourceLayer resLayer;

        private ConditionManager conditionManager;
        private int token;
        private string condtion;

        private Dictionary<string, string> resourceTypesPreres;

        public PreyActivity(Actor self, Actor dockact, bool facingDock, Dock d)
        {
            info = self.Info.TraitInfo<AcolytePreyInfo>();
            wsb = self.Trait<WithSpriteBody>();
            dockactor = dockact;
            lockfacing = facingDock;
            playanim = true;
            this.d = d;

            ticks = self.Info.TraitInfo<AcolytePreyInfo>().Leechinterval;
            resLayer = self.World.WorldActor.Trait<ResourceLayer>();

            conditionManager = self.Trait<ConditionManager>();
            token = ConditionManager.InvalidConditionToken;
            condtion = self.Info.TraitInfo<AcolytePreyInfo>().SelfEnabledCondition;

            resourceTypesPreres = self.Info.TraitInfo<AcolytePreyInfo>().ResourceTypesPreres;
        }

        public override Activity Tick(Actor self)
        {
            if (IsCanceled)
            {
                wsb.PlayCustomAnimationRepeating(self, wsb.Info.Sequence);
                playanim = true;
                if (condtion != null && token != ConditionManager.InvalidConditionToken)
                {
                    token = conditionManager.RevokeCondition(self, token);
                    token = ConditionManager.InvalidConditionToken;
                }

                if (endqueueonce)
                {
                    endqueueonce = false;
                    QueueChild(self.Trait<IMove>().VisualMove(self, self.CenterPosition,
                        self.World.Map.CenterOfCell(self.World.Map.CellContaining(self.CenterPosition))));
                    QueueChild(new CallFunc(() =>
                    {
                        self.Trait<IPositionable>()
                            .SetPosition(self, self.World.Map.CellContaining(self.CenterPosition));
                    }));
                }

                if (ChildActivity == null)
                {
                    return NextActivity;
                }
            }

            if (ChildActivity != null)
            {
                ActivityUtils.RunActivity(self, ChildActivity);
                return this;
            }

            if (playanim)
            {
                playanim = false;
                endqueueonce = true;
                QueueChild(self.Trait<IMove>().VisualMove(self, self.CenterPosition, d.CenterPosition));
                QueueChild(new CallFunc(() =>
               {
                   var facing = self.Trait<IFacing>();
                   if (dockactor != null && facing != null && lockfacing)
                   {
                       var desiredFacing =
                           (dockactor.CenterPosition - self.CenterPosition).HorizontalLengthSquared != 0
                               ? (dockactor.CenterPosition - self.CenterPosition).Yaw.Facing
                               : facing.Facing;
                       facing.Facing = desiredFacing;
                   }

                   wsb.PlayCustomAnimationRepeating(self, info.PreySequence);
                   if (condtion != null)
                       token = conditionManager.GrantCondition(self, condtion);
               }));
            }

            if (self.Info.TraitInfo<AcolytePreyInfo>().LeechesResources && --ticks <= 0)
            {
                var amm = Leech(self);
                ticks = self.Info.TraitInfo<AcolytePreyInfo>().Leechinterval + amm;
            }

            return this;
        }

        public int Leech(Actor self)
        {
            var validRes = new HashSet<string>();

            foreach (var restype in resourceTypesPreres)
            {
                var hash = new List<string>();
                hash.Add(restype.Value);

                if (restype.Value == "NONE")
                {
                    validRes.Add(restype.Key);
                }
                else if (self.Owner.PlayerActor.TraitOrDefault<TechTree>().HasPrerequisites(hash))
                {
                    validRes.Add(restype.Key);
                }
            }

            CPos cell = CPos.Zero;
            var cells = self.World.Map.FindTilesInCircle(self.World.Map.CellContaining(self.CenterPosition), 6, true)
                .Where(c =>
                {
                    if (!self.World.Map.Contains(c))
                        return false;
                    if (resLayer.GetResource(c) == null)
                        return false;
                    if (resLayer.GetResourceDensity(c) == 0)
                        return false;
                    if (validRes.Contains(resLayer.GetResource(c).Info.Type))
                        return true;

                    return false;
                });
            if (cells != null && cells.Any())
                cell = self.ClosestCell(cells);

            if (cell != CPos.Zero && resLayer.GetResourceDensity(cell) > 0)
            {
                var type = resLayer.GetResource(cell);
                var ammount = type.Info.ValuePerUnit;

                if ((self.Owner.PlayerActor.Trait<PlayerResources>().Resources + ammount) <= self.Owner.PlayerActor.Trait<PlayerResources>().ResourceCapacity)
                {
                    var playerResources = self.Owner.PlayerActor.Trait<PlayerResources>();
                    playerResources.GiveResources(ammount);

                    if (ammount > 0 && self.IsInWorld && !self.IsDead)
                    {
                        var floattest = ammount.ToString();

                        floattest = "+ " + floattest + " Essence";

                        if (self.Owner.IsAlliedWith(self.World.RenderPlayer))
                            self.World.AddFrameEndTask(w => w.Add(new FloatingText(self.CenterPosition,
                                self.Owner.Color.RGB, floattest, 30)));
                    }

                    resLayer.Harvest(cell);
                    if (resLayer.GetResourceDensity(cell) <= 0)
                        resLayer.Destroy(cell);

                    return ammount;
                }
            }

            return 0;
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

        public object Create(ActorInitializer init)
        {
            throw new NotImplementedException();
        }
    }
}