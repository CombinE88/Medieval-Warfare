using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits
{
    [Desc("Is Unit a Peasant (adds a count of 1 to the PlayerCivilisation).")]
    public class IsPeasantInfo : ITraitInfo
    {
        public object Create(ActorInitializer init) { return new IsPeasant(init.Self); }
    }

    public class IsPeasant : INotifyCreated, INotifyRemovedFromWorld
    {
        readonly Actor self;

        public bool IsWorker;

        public IsPeasant(Actor self)
        {
            this.self = self;
        }

        void INotifyCreated.Created(Actor self)
        {
            if (!self.Owner.NonCombatant && self.Owner.WinState != WinState.Lost && self.Owner.PlayerActor.Info.HasTraitInfo<PlayerCivilizationInfo>())
            {
                self.Owner.PlayerActor.Trait<PlayerCivilization>().Peasantpopulationvar += 1;
                self.Owner.PlayerActor.Trait<PlayerCivilization>().Recalculate();
            }
        }

        void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
        {
            if (!self.Owner.NonCombatant && self.Owner.WinState != WinState.Lost && self.Owner.PlayerActor.Info.HasTraitInfo<PlayerCivilizationInfo>())
            {
                if (IsWorker)
                {
                    self.Owner.PlayerActor.Trait<PlayerCivilization>().WorkerPopulationvar -= 1;
                    self.Owner.PlayerActor.Trait<PlayerCivilization>().Recalculate();
                }
                else
                {
                    self.Owner.PlayerActor.Trait<PlayerCivilization>().Peasantpopulationvar -= 1;
                    self.Owner.PlayerActor.Trait<PlayerCivilization>().Recalculate();
                }
            }
        }

        public void SetWroking()
        {
            if (!IsWorker)
            {
                IsWorker = true;
                self.Owner.PlayerActor.Trait<PlayerCivilization>().WorkerPopulationvar += 1;
                self.Owner.PlayerActor.Trait<PlayerCivilization>().Peasantpopulationvar -= 1;
                self.Owner.PlayerActor.Trait<PlayerCivilization>().Recalculate();
            }
        }

        public void SetPeasant()
        {
            if (IsWorker)
            {
                self.Owner.PlayerActor.Trait<PlayerCivilization>().WorkerPopulationvar -= 1;
                self.Owner.PlayerActor.Trait<PlayerCivilization>().Peasantpopulationvar += 1;
                self.Owner.PlayerActor.Trait<PlayerCivilization>().Recalculate();
                IsWorker = false;
            }
        }
    }
}
