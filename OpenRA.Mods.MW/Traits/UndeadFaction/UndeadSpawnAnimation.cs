#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Replaces the sprite during construction/deploy/undeploy.")]
	public class UndeadSpawnAnimationInfo : ITraitInfo, Requires<WithSpriteBodyInfo>
	{
		[Desc("Sequence name to use.")]
		[SequenceReference] public readonly string Sequence = "make";

		[GrantedConditionReference]
		[Desc("The condition to grant to self while the make animation is playing.")]
		public readonly string Condition = null;
		
		[Desc("Apply to sprite bodies with these names.")]
		public readonly string[] BodyNames = { "body" };

		public object Create(ActorInitializer init) { return new UndeadSpawnAnimation(init, this); }
	}

	public class UndeadSpawnAnimation : INotifyCreated
	{
		readonly UndeadSpawnAnimationInfo info;
		readonly WithSpriteBody[] wsbs;

		ConditionManager conditionManager;
		int token = ConditionManager.InvalidConditionToken;

		public UndeadSpawnAnimation(ActorInitializer init, UndeadSpawnAnimationInfo info)
		{
			this.info = info;
			var self = init.Self;
			wsbs = self.TraitsImplementing<WithSpriteBody>().Where(w => info.BodyNames.Contains(w.Info.Name)).ToArray();
		}

		void INotifyCreated.Created(Actor self)
		{
			conditionManager = self.TraitOrDefault<ConditionManager>();
		}

		public void Forward(Actor self, Action onComplete)
		{
			if (conditionManager != null && !string.IsNullOrEmpty(info.Condition) && token == ConditionManager.InvalidConditionToken)
				token = conditionManager.GrantCondition(self, info.Condition);

			var wsb = wsbs.FirstEnabledTraitOrDefault();

			if (wsb == null)
				return;

			wsb.PlayCustomAnimation(self, info.Sequence, () =>
			{
				self.World.AddFrameEndTask(w =>
				{
					if (token != ConditionManager.InvalidConditionToken)
						token = conditionManager.RevokeCondition(self, token);

					// TODO: Rewrite this to use a trait notification for save game support
					onComplete();
				});
			});
		}
	}
}
