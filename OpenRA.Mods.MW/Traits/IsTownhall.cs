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
        PlayerCivilization PlayerCiv;
        bool Enabled = false;

        public IsTownhall(Actor self, IsTownhallInfo info) : base(info)
        {
            PlayerCiv = self.Owner.PlayerActor.Trait<PlayerCivilization>();
        }

        void INotifyCreated.Created(Actor self)
        {
            if (!self.Owner.NonCombatant && self.Owner.WinState != WinState.Lost && self.Owner.PlayerActor.Info.HasTraitInfo<PlayerCivilizationInfo>())
            {
                if (!IsTraitDisabled && !Enabled)
                {
                    PlayerCiv.PercentageModifier += Info.Percent;
                    Enabled = true;
                }
            }
        }

        void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
        {
            if (!self.Owner.NonCombatant && self.Owner.WinState != WinState.Lost && self.Owner.PlayerActor.Info.HasTraitInfo<PlayerCivilizationInfo>())
            {
                if (!IsTraitDisabled && Enabled)
                {
                    PlayerCiv.PercentageModifier -= Info.Percent;
                    Enabled = false;
                }
            }
        }

        protected override void TraitEnabled(Actor self)
        {
            if (!IsTraitDisabled && !Enabled)
            {
                PlayerCiv.PercentageModifier += Info.Percent;
                Enabled = true;
            }
        }

        protected override void TraitDisabled(Actor self)
        {
            if (!IsTraitDisabled && Enabled)
            {
                PlayerCiv.PercentageModifier -= Info.Percent;
                Enabled = false;
            }
        }
    }
}
