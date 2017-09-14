using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits
{
    public class IsAtracterInfo : ITraitInfo
    {
        public object Create(ActorInitializer init) { return new IsAtracter(init.Self, this); }
    }

    public class IsAtracter : INotifyCreated, INotifyRemovedFromWorld
    {
        private IsAtracterInfo info;
        readonly Actor self;
		
        public IsAtracter(Actor self, IsAtracterInfo info)
        {
            this.info = info;
            this.self = self;
        }

        public void Created(Actor self)
        {
            if (!self.Owner.NonCombatant && self.Owner.WinState != WinState.Lost && self.Owner.PlayerActor.Info.HasTraitInfo<PlayerCivilizationInfo>())
            {
                self.Owner.PlayerActor.Trait<PlayerCivilization>().HasAttrackter += 1;
            }
        }

        public void RemovedFromWorld(Actor self)
        {
            if (!self.Owner.NonCombatant && self.Owner.WinState != WinState.Lost && self.Owner.PlayerActor.Info.HasTraitInfo<PlayerCivilizationInfo>())
            {
                self.Owner.PlayerActor.Trait<PlayerCivilization>().HasAttrackter -= 1;
            }
        }
    }
}
