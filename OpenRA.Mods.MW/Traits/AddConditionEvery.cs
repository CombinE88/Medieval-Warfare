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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits
{
    [Desc("Grants a condition while the trait is active.")]
    class AddConditionEveryInfo : ConditionalTraitInfo
    {
        [FieldLoader.Require]
        [GrantedConditionReference]
        public readonly int Delay = 25;

        public readonly string AddCondition = null;

        public override object Create(ActorInitializer init) { return new AddConditionEvery(this); }

    }

    class AddConditionEvery : ConditionalTrait<AddConditionEveryInfo>, ITick
    {
        ConditionManager conditionManager;
        private int tick;

        public AddConditionEvery(AddConditionEveryInfo info)
            : base(info) { }

        public void Tick(Actor self)
        {
            if (!IsTraitDisabled)
            {
                if (--tick < 0)
                {
                    conditionManager.GrantCondition(self, Info.AddCondition);
                    tick = Info.Delay;
                }
            }
        }

        protected override void Created(Actor self)
        {
            conditionManager = self.Trait<ConditionManager>();
            tick = Info.Delay;
            base.Created(self);
        }

    }
}