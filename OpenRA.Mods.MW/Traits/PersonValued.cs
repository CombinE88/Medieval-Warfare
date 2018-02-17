using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits
{
    [Desc("How much the unit is worth in Peasants.")]
    public class PersonValuedInfo : ITraitInfo
    {
        [FieldLoader.Require]
        [Desc("Used in production. Set 0 to not need an actor to be converted")]
        public readonly int ActorCount = 1;

        public readonly HashSet<string> ConvertingActors = new HashSet<string>();

        public object Create(ActorInitializer init) { return new PersonValued(init.Self, this); }
    }

    public class PersonValued : INotifyCreated, INotifyRemovedFromWorld
    {

        private PersonValuedInfo info;

        public PersonValued(Actor self, PersonValuedInfo info)
        {
            this.info = info;
        }

        void INotifyCreated.Created(Actor self)
        {
            if (!self.Owner.NonCombatant && self.Owner.WinState != WinState.Lost && self.Owner.PlayerActor.Info.HasTraitInfo<PlayerCivilizationInfo>())
            {
                for (int i = 1; i <= info.ActorCount; i++)
                {
                    self.Owner.PlayerActor.Trait<PlayerCivilization>().WorkerPopulationvar += info.ActorCount;
                    self.Owner.PlayerActor.Trait<PlayerCivilization>().Recalculate();
                }
            }
        }

        void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
        {
            if (!self.Owner.NonCombatant && self.Owner.WinState != WinState.Lost && self.Owner.PlayerActor.Info.HasTraitInfo<PlayerCivilizationInfo>())
            {
                for (int i = 1; i <= info.ActorCount; i++)
                {
                    self.Owner.PlayerActor.Trait<PlayerCivilization>().WorkerPopulationvar -= info.ActorCount;
                    self.Owner.PlayerActor.Trait<PlayerCivilization>().Recalculate();
                }
            }
        }
    }
}
