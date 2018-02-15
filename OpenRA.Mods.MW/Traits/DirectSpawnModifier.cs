using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits
{
    public class DirectSpawnModifierInfo : ConditionalTraitInfo
    {

        [Desc("Number in ticks wich reduce spawntime.")]
        public readonly int Ticks = 25;

        public override object Create(ActorInitializer init) { return new DirectSpawnModifier(init.Self, this); }
    }

    public class DirectSpawnModifier : ConditionalTrait<DirectSpawnModifierInfo>, INotifyCreated, INotifyRemovedFromWorld
    {
        PlayerCivilization PlayerCiv;
        bool Enabled = false;

        public DirectSpawnModifier(Actor self, DirectSpawnModifierInfo info) : base(info)
        {
            PlayerCiv = self.Owner.PlayerActor.Trait<PlayerCivilization>();
        }

        protected override void Created(Actor self)
        {
            if (!self.Owner.NonCombatant && self.Owner.WinState != WinState.Lost && self.Owner.PlayerActor.Info.HasTraitInfo<PlayerCivilizationInfo>())
            {
               if (!IsTraitDisabled && !Enabled)
                {
                    PlayerCiv.DirectModifier += Info.Ticks;
                    Enabled = true;
                }
            }
        }

        public void RemovedFromWorld(Actor self)
        {
            if (!self.Owner.NonCombatant && self.Owner.WinState != WinState.Lost && self.Owner.PlayerActor.Info.HasTraitInfo<PlayerCivilizationInfo>())
            {
                if (!IsTraitDisabled && Enabled)
                {
                    PlayerCiv.DirectModifier -= Info.Ticks;
                    Enabled = false;
                }
            }
        }

        protected override void TraitEnabled(Actor self)
        {
            if (!IsTraitDisabled && !Enabled)
            {
                PlayerCiv.DirectModifier += Info.Ticks;
                Enabled = true;
            }
        }

        protected override void TraitDisabled(Actor self)
        {
            if (!IsTraitDisabled && Enabled)
            {
                PlayerCiv.DirectModifier -= Info.Ticks;
                Enabled = false;
            }
        }
    }
}
