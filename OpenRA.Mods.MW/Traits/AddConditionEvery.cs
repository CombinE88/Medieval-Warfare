using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits
{
    [Desc("Grants a condition while the trait is active.")]
    class AddConditionEveryInfo : ConditionalTraitInfo
    {
        public readonly int Delay = 25;

        public readonly string AddCondition = null;

        public override object Create(ActorInitializer init) { return new AddConditionEvery(init.Self, this); }

    }

    class AddConditionEvery : ConditionalTrait<AddConditionEveryInfo>, ITick
    {
        ConditionManager conditionManager;
        private int tick;
        //int conditionToken = ConditionManager.InvalidConditionToken;

        public AddConditionEvery(Actor self, AddConditionEveryInfo info)
            : base(info)
        {
            tick = Info.Delay;
        }

        public void Tick(Actor self)
        {
            if (IsTraitDisabled)
                return;

            if (--tick < 0)
            {
                conditionManager = self.Trait<ConditionManager>();
                conditionManager.GrantCondition(self, Info.AddCondition);
                tick = Info.Delay;
            }
        }
    }
}