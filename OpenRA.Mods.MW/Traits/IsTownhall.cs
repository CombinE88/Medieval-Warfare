using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits
{
    public class IsTownhallInfo : ConditionalTraitInfo
    {
        [Desc("Number in Percent 0-100 How much the total spawnrate will be reduced.")]
        public readonly int Percent = 50;

        public override object Create(ActorInitializer init) { return new IsTownhall(init.Self, this); }
    }

    public class IsTownhall : ConditionalTrait<IsTownhallInfo>, INotifyCreated, INotifyRemovedFromWorld
    {
        PlayerCivilization playerCiv;
        bool enabled;

        public IsTownhall(Actor self, IsTownhallInfo info) : base(info)
        {
            playerCiv = self.Owner.PlayerActor.Trait<PlayerCivilization>();
        }

        void INotifyCreated.Created(Actor self)
        {
            if (!self.Owner.NonCombatant && self.Owner.WinState != WinState.Lost && self.Owner.PlayerActor.Info.HasTraitInfo<PlayerCivilizationInfo>())
            {
                if (!IsTraitDisabled && !enabled)
                {
                    playerCiv.PercentageModifier += Info.Percent;
                    enabled = true;
                }
            }
        }

        void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
        {
            if (!self.Owner.NonCombatant && self.Owner.WinState != WinState.Lost && self.Owner.PlayerActor.Info.HasTraitInfo<PlayerCivilizationInfo>())
            {
                if (!IsTraitDisabled && enabled)
                {
                    playerCiv.PercentageModifier -= Info.Percent;
                    enabled = false;
                }
            }
        }

        protected override void TraitEnabled(Actor self)
        {
            if (!IsTraitDisabled && !enabled)
            {
                playerCiv.PercentageModifier += Info.Percent;
                enabled = true;
            }
        }

        protected override void TraitDisabled(Actor self)
        {
            if (!IsTraitDisabled && enabled)
            {
                playerCiv.PercentageModifier -= Info.Percent;
                enabled = false;
            }
        }
    }
}
