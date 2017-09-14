using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits
{
	[Desc("Is Unit a Peasant (adds a count of 1 to the PlayerCivilisation).")]
	public class IsPeasantInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new IsPeasant(init.Self, this); }
	}

    public class IsPeasant : INotifyCreated, INotifyRemovedFromWorld
    {
        private IsPeasantInfo info;
        readonly Actor self;

        public bool isWorker;

        public IsPeasant(Actor self, IsPeasantInfo info)
        {
            this.info = info;
            this.self = self;
        }

        public void Created(Actor self)
        {
            if (!self.Owner.NonCombatant && self.Owner.WinState != WinState.Lost && self.Owner.PlayerActor.Info.HasTraitInfo<PlayerCivilizationInfo>())
            {
                self.Owner.PlayerActor.Trait<PlayerCivilization>().Peasantpopulationvar += 1;
            }
        }

        public void RemovedFromWorld(Actor self)
        {
            if (!self.Owner.NonCombatant && self.Owner.WinState != WinState.Lost && self.Owner.PlayerActor.Info.HasTraitInfo<PlayerCivilizationInfo>())
            {
                if (isWorker)
                {
                    self.Owner.PlayerActor.Trait<PlayerCivilization>().WorkerPopulationvar -= 1;
                }
                else
                {
                    self.Owner.PlayerActor.Trait<PlayerCivilization>().Peasantpopulationvar -= 1;
                }

            }
        }
        public void setWroking()
        {
            if (!isWorker)
            {
                isWorker = true;
                self.Owner.PlayerActor.Trait<PlayerCivilization>().Peasantpopulationvar -= 1;
                self.Owner.PlayerActor.Trait<PlayerCivilization>().WorkerPopulationvar += 1;
            }
        }
        public void setPeasant()
        {
            if (isWorker)
            {
                self.Owner.PlayerActor.Trait<PlayerCivilization>().WorkerPopulationvar -= 1;
                self.Owner.PlayerActor.Trait<PlayerCivilization>().Peasantpopulationvar += 1;
                isWorker = false;
            }
        }
    }
}
