#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits
{
    [Desc("Grants a condition while the trait is active.")]
    class GrantPermanentConditionInfo : ConditionalTraitInfo
    {
        [FieldLoader.Require]
        [GrantedConditionReference]
        [Desc("Condition to grant.")]
        public readonly string Condition = null;

        public override object Create(ActorInitializer init) { return new GrantPermanentCondition(this); }
    }

    class GrantPermanentCondition : ConditionalTrait<GrantPermanentConditionInfo>
    {
        ConditionManager conditionManager;
        int conditionToken = ConditionManager.InvalidConditionToken;
        bool enabled = false;

        public GrantPermanentCondition(GrantPermanentConditionInfo info)
            : base(info) { }

        protected override void Created(Actor self)
        {
            conditionManager = self.Trait<ConditionManager>();

            base.Created(self);
        }

        protected override void TraitEnabled(Actor self)
        {
            if (!enabled && conditionToken == ConditionManager.InvalidConditionToken)
            {
                conditionToken = conditionManager.GrantCondition(self, Info.Condition);
                enabled = true;
            }
        }
    }
}