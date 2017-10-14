using System.Collections.Generic;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits
{
    [Desc("Is Unit a Peasant (adds a count of 1 to the PlayerCivilisation).")]
    public class StartAndIdleInfo : ITraitInfo
    {
        public object Create(ActorInitializer init) { return new StartAndIdle(init.Self, this); }
    }

    public class StartAndIdle : INotifyCreated, ITick
    {
        private StartAndIdleInfo info;
        readonly Actor self;

        private int ticker;
        private Activity move;
        bool triggered;
		
        public StartAndIdle(Actor self, StartAndIdleInfo info)
        {
            this.info = info;
            this.self = self;
            ticker = 1;
        }

        public void Created(Actor self)
        {
            move = self.TraitOrDefault<IMove>().MoveTo(self.Location, 0);
            self.QueueActivity(move);
            
        }

        public void Tick(Actor self)
        {
            if ( !triggered && --ticker != 0)
            {
                triggered = true;
                self.CancelActivity();
            }
        }
    }
}
