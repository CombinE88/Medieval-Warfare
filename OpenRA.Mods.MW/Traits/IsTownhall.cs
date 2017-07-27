using OpenRA.Traits;

namespace OpenRA.Mods.Mw.Traits
{
    public class IsTownhallInfo : ITraitInfo
    {
        public object Create(ActorInitializer init) { return new IsTownhall(init.Self, this); }
    }

    public class IsTownhall : INotifyCreated, INotifyRemovedFromWorld
    {
        private IsTownhallInfo info;
        readonly Actor self;
		
        public IsTownhall(Actor self, IsTownhallInfo info)
        {
            this.info = info;
            this.self = self;
        }

        public void Created(Actor self)
        {
            if (!self.Owner.NonCombatant && self.Owner.WinState != WinState.Lost && self.Owner.PlayerActor.Info.HasTraitInfo<PlayerCivilizationInfo>())
            {
                self.Owner.PlayerActor.Trait<PlayerCivilization>().HasTownHalls += 1;
            }
        }

        public void RemovedFromWorld(Actor self)
        {
            if (!self.Owner.NonCombatant && self.Owner.WinState != WinState.Lost && self.Owner.PlayerActor.Info.HasTraitInfo<PlayerCivilizationInfo>())
            {
                self.Owner.PlayerActor.Trait<PlayerCivilization>().HasTownHalls -= 1;
            }
        }
    }
}
