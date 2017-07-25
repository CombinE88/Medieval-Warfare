
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Graphics;
using OpenRA.Mods.Cnc.Traits;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Activities
{

    // Assumes you have Minelayer on that unit
    public class PreyActivity : Activity
    {
        readonly AcolytePreyInfo info;
        readonly WithSpriteBody wsb;
        
        ConditionManager conditionManager;
        int token = ConditionManager.InvalidConditionToken;

        private Actor dockactor;
        private bool lockfacing;
        bool playanim;
        private Dock _d;

        public PreyActivity(Actor self,Actor dockact,bool facingDock,Dock d)
        {
            info = self.Info.TraitInfo<AcolytePreyInfo>();
            wsb = self.Trait<WithSpriteBody>();
            dockactor = dockact;
            lockfacing = facingDock;
            playanim = true;
            _d = d;
            conditionManager = dockact.Trait<ConditionManager>();

        }

        public override Activity Tick(Actor self)
        {
            if (IsCanceled)
            {
                wsb.PlayCustomAnimationRepeating(self,wsb.Info.Sequence);
                playanim = true;

                if (token != ConditionManager.InvalidConditionToken)
                {
                    token = conditionManager.RevokeCondition(dockactor, token);
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
                }));
            }
            
            
            if (token == ConditionManager.InvalidConditionToken)
            {
                token = conditionManager.GrantCondition(dockactor, self.Info.TraitInfo<AcolytePreyInfo>().GrantsCondition);
            }


            return this;
        }
    }
}