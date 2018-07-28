using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.MW.Effects;
using OpenRA.Mods.MW.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Activities
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
        private Dock d;

        private int ticks;
        private UndeadBuilder undeadBuilder;

        private ConditionManager conditionManager;
        private int token;
        private string condtion;

        private PlayerResources pr;

        public PreyBuildActivity(Actor self, Actor dockact, bool facingDock, Dock d)
        {
            info = self.Info.TraitInfo<AcolytePreyBuildInfo>();
            wsb = self.Trait<WithSpriteBody>();
            dockactor = dockact;
            lockfacing = facingDock;
            playanim = true;
            this.d = d;

            ticks = self.Info.TraitInfo<AcolytePreyBuildInfo>().Buildinterval;
            undeadBuilder = dockactor.TraitsImplementing<UndeadBuilder>().FirstOrDefault();

            conditionManager = self.Trait<ConditionManager>();
            token = ConditionManager.InvalidConditionToken;
            condtion = self.Info.TraitInfo<AcolytePreyBuildInfo>().SelfEnabledCondition;
            pr = self.Owner.PlayerActor.Trait<PlayerResources>();
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

            BuildPrey(self);

            return this;
        }

        void BuildPrey(Actor self)
        {
            if (undeadBuilder.Hassummoningcount >= undeadBuilder.Info.SummoningTime)
                Cancel(self, true);
            else if (undeadBuilder != null && --ticks <= 0)
            {
                ticks = self.Info.TraitInfo<AcolytePreyBuildInfo>().Buildinterval;
                if (pr.TakeCash(undeadBuilder.PayPerTick, true))
                {
                    undeadBuilder.Hassummoningcount += 1;
                    var floattest = undeadBuilder.PayPerTick.ToString();
                    floattest = "- " + floattest + " Essence";
                    if (self.Owner.IsAlliedWith(self.World.RenderPlayer))
                        self.World.AddFrameEndTask(w => w.Add(new FloatingTextBackwards(self.CenterPosition,
                            self.Owner.Color.RGB, floattest, 30)));
                }
            }
            else if (dockactor == null || dockactor.IsDead || !dockactor.IsInWorld)
                Cancel(self, true);
        }
    }
}