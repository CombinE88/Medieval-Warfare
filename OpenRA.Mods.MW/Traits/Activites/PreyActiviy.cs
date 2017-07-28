
using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Activities
{

    // Assumes you have Minelayer on that unit
    public class PreyActivity : Activity
    {
        readonly AcolytePreyInfo info;
        readonly WithSpriteBody wsb;
        
        ExternalCondition externalCondition;
        int token = ConditionManager.InvalidConditionToken;

        private Actor dockactor;
        private bool lockfacing;
        bool playanim;
        private Dock _d;

        private int ticks;
        private ResourceLayer resLayer;

        public PreyActivity(Actor self,Actor dockact,bool facingDock,Dock d)
        {
            info = self.Info.TraitInfo<AcolytePreyInfo>();
            wsb = self.Trait<WithSpriteBody>();
            dockactor = dockact;
            lockfacing = facingDock;
            playanim = true;
            _d = d;
            
            ticks = self.Info.TraitInfo<AcolytePreyInfo>().leechinterval;
            resLayer = self.World.WorldActor.Trait<ResourceLayer>();
                
            externalCondition =  dockact.TraitsImplementing<ExternalCondition>()
                .FirstOrDefault(t =>
                    t.Info.Condition == self.Info.TraitInfo<AcolytePreyInfo>().GrantsCondition &&
                    t.CanGrantCondition(dockactor, self));
            

        }

        public override Activity Tick(Actor self)
        {
            if (IsCanceled)
            {
                wsb.PlayCustomAnimationRepeating(self,wsb.Info.Sequence);
                playanim = true;

                if (externalCondition != null)
                {
                    Game.Debug("revoke: " + token);
                    externalCondition.TryRevokeCondition(self, dockactor, token);
                }
                return NextActivity;
                
            }
            
            if (ChildActivity != null)
            {
                ActivityUtils.RunActivity(self, ChildActivity);
                return this;
            }
           
            if (playanim)
            {
                playanim = false;
                QueueChild(self.Trait<IMove>().VisualMove(self, self.CenterPosition, _d.CenterPosition));
                QueueChild(new CallFunc (() =>
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
                    
                    Game.Debug("Manager: " + externalCondition);
                    if (externalCondition != null)
                    {
                        token = externalCondition.GrantCondition(dockactor, self);
                    }
                }));
            }
            
            if (self.Info.TraitInfo<AcolytePreyInfo>().LeechesResources && --ticks <= 0)
            {
                Leech(self);
                ticks = self.Info.TraitInfo<AcolytePreyInfo>().leechinterval;
            }
            
            
            return this;
        }

        public void Leech(Actor self)
        {
            CPos cell = CPos.Zero;
            var cells = self.World.Map.FindTilesInCircle(self.World.Map.CellContaining(self.CenterPosition), 6, true)
                .Where(c =>
                {
                    if (!self.World.Map.Contains(c))
                        return false;
                    if (resLayer.GetResource(c) != null)
                        return false;
                    return true;
                });
            if (cells != null && cells.Any())
                cell = self.ClosestCell(cells);

            if (cell != CPos.Zero && resLayer.GetResourceDensity(cell) > 0)
            {
                var ammount = resLayer.GetResource(cell).Info.ValuePerUnit;

                if ((self.Owner.PlayerActor.Trait<PlayerResources>().Resources + ammount) <= self.Owner.PlayerActor.Trait<PlayerResources>().ResourceCapacity)
                {

                    var playerResources = self.Owner.PlayerActor.Trait<PlayerResources>();
                    playerResources.GiveResources(ammount);

                    if (ammount > 0 && self.IsInWorld && !self.IsDead)
                    {
                        if (self.Owner.IsAlliedWith(self.World.RenderPlayer))
                            self.World.AddFrameEndTask(w => w.Add(new FloatingText(self.CenterPosition,
                                self.Owner.Color.RGB, FloatingText.FormatCashTick(ammount), 30)));
                    }
                    resLayer.Harvest(cell);
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