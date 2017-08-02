
using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.Mw.Traits;
using OpenRA.Mods.MW.Traits;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Activities
{

    // Assumes you have Minelayer on that unit
    public class PreyBuildActivity : Activity
    {
        readonly AcolytePreyBuildInfo info;
        readonly WithSpriteBody wsb;

        private Actor dockactor;
        private bool lockfacing;
        bool playanim;
        private bool endqueueonce;
        private Dock _d;

        private int ticks;
        private int buildpower;
        private UndeadBuilder UndeadBuilder;

        private ConditionManager ConditionManager;
        private int token;
        private string condtion;

        public PreyBuildActivity(Actor self,Actor dockact,bool facingDock,Dock d)
        {
            info = self.Info.TraitInfo<AcolytePreyBuildInfo>();
            wsb = self.Trait<WithSpriteBody>();
            dockactor = dockact;
            lockfacing = facingDock;
            playanim = true;
            _d = d;
            
            ticks = self.Info.TraitInfo<AcolytePreyBuildInfo>().Buildinterval;
            buildpower = self.Info.TraitInfo<AcolytePreyBuildInfo>().Buildpower;
            UndeadBuilder = dockactor.TraitsImplementing<UndeadBuilder>().FirstOrDefault();

            ConditionManager = self.Trait<ConditionManager>();
            token = ConditionManager.InvalidConditionToken;
            condtion = self.Info.TraitInfo<AcolytePreyBuildInfo>().SelfEnabledCondition;

        }

        public override Activity Tick(Actor self)
        {
            if (IsCanceled)
            {
                wsb.PlayCustomAnimationRepeating(self,wsb.Info.Sequence);

                if (condtion != null && token != ConditionManager.InvalidConditionToken)
                {
                    token = ConditionManager.RevokeCondition(self, token);
                    token = ConditionManager.InvalidConditionToken;
                }


                playanim = true;
                
                if( endqueueonce)
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
                    
                    if (condtion != null)
                           token = ConditionManager.GrantCondition(self, condtion);
                }));
            }
            
            if (UndeadBuilder != null && --ticks <= 0)
            {

                ticks = self.Info.TraitInfo<AcolytePreyBuildInfo>().Buildinterval;
                UndeadBuilder.hassummoningcount += 1;
            }
            
            
            return this;
        }

    }
}