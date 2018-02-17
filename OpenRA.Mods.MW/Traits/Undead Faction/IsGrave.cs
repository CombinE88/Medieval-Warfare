using System;
using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits
{
    [Desc("Is Unit a Grave (adds a count of x to the PlayerCivilisation Peasantpopulation).")]
    public class IsGraveInfo : ITraitInfo
    {

        public readonly int Value = 1;

        public object Create(ActorInitializer init) { return new IsGrave(init.Self, this); }
    }

    public class IsGrave : INotifyCreated, INotifyRemovedFromWorld
    {
        private IsGraveInfo info;

        public IsGrave(Actor self, IsGraveInfo info)
        {
            this.info = info;
        }

        void INotifyCreated.Created(Actor self)
        {
            if (!self.Owner.NonCombatant && self.Owner.WinState != WinState.Lost && self.Owner.PlayerActor.Info.HasTraitInfo<PlayerCivilizationInfo>())
            {
                self.Owner.PlayerActor.Trait<PlayerCivilization>().MaxLivingspacevar += info.Value;
                self.Owner.PlayerActor.Trait<PlayerCivilization>().Recalculate();
            }
        }

        void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
        {
            if (!self.Owner.NonCombatant && self.Owner.WinState != WinState.Lost && self.Owner.PlayerActor.Info.HasTraitInfo<PlayerCivilizationInfo>())
            {
                self.Owner.PlayerActor.Trait<PlayerCivilization>().MaxLivingspacevar -= info.Value;
                self.Owner.PlayerActor.Trait<PlayerCivilization>().Recalculate();
            }
        }
    }
}
