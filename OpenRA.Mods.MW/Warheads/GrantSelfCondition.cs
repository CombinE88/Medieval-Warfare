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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using OpenRA.Mods.Common.Warheads;

namespace OpenRA.Mods.MW.Warheads
{
	public class GrantSelfConditionWarhead : Warhead
	{
		[FieldLoader.Require]
		[Desc("The condition to apply. Must be included in the target actor's ExternalConditions list.")]
		public readonly string Condition = null;

		[Desc("Duration of the condition (in ticks). Set to 0 for a permanent condition.")]
		public readonly int Duration = 0;

		public override void DoImpact(Target target, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			if (!firedBy.IsDead && firedBy.IsInWorld)
			{
				//Log.Write("debug", "GrantSelfConditionWarhead ::: DoImpact");

				if (!IsValidAgainst(firedBy, firedBy) && !firedBy.IsDead && firedBy.IsInWorld)
				{
					//Log.Write("debug", "GrantSelfConditionWarhead ::: !IsValidAgainst");
					return;
				}

				var external = firedBy.TraitsImplementing<ExternalCondition>()
					.FirstOrDefault(t => t.Info.Condition == Condition && t.CanGrantCondition(firedBy, firedBy));

				if (external != null && !firedBy.IsDead && firedBy.IsInWorld)
				{
					//Log.Write("debug", "GrantSelfConditionWarhead ::: external");
					external.GrantCondition(firedBy, firedBy, Duration);
				}
			}
		}
	}
}
