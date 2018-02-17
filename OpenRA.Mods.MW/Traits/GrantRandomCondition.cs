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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits
{
    [Desc("Grants a condition while the trait is active.")]
    class GrantRandomConditionInfo : ConditionalTraitInfo
    {
        [FieldLoader.Require]
        [GrantedConditionReference]
        [Desc("Condition to grant when no RandomConditions are set.")]
        public readonly string FallbackCondition = null;
        [Desc("Random condition to grant out of a set of strings.")]
        public readonly HashSet<string> RandomConditions = new HashSet<string>();

        public override object Create(ActorInitializer init) { return new GrantRandomCondition(this); }
    }

    class GrantRandomCondition : ConditionalTrait<GrantRandomConditionInfo>
    {
        ConditionManager conditionManager;
        int conditionToken = ConditionManager.InvalidConditionToken;
        private string PickedCondition;

        public GrantRandomCondition(GrantRandomConditionInfo info)
            : base(info) { }

        protected override void Created(Actor self)
        {
            conditionManager = self.Trait<ConditionManager>();
            if (Info.RandomConditions.Count == 0)
            {
                PickedCondition = Info.FallbackCondition;
            }
            else
            {
                PickedCondition = Info.RandomConditions.ElementAt(self.World.SharedRandom.Next(Info.RandomConditions.Count));
            }
            base.Created(self);
        }

        protected override void TraitEnabled(Actor self)
        {
            if (conditionToken == ConditionManager.InvalidConditionToken)
                conditionToken = conditionManager.GrantCondition(self, PickedCondition);
        }

        protected override void TraitDisabled(Actor self)
        {
            if (conditionToken == ConditionManager.InvalidConditionToken)
                return;

            conditionToken = conditionManager.RevokeCondition(self, conditionToken);
        }
    }
}