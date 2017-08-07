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
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits
{
	[Desc("Actor becomes a specified actor type when this trait is triggered.")]
	public class SkipTransformsInfo : ITraitInfo
	{
		[Desc("Actor to transform into."), ActorReference, FieldLoader.Require]
		public readonly string IntoActor = null;

		[Desc("Offset to spawn the transformed actor relative to the current cell.")]
		public readonly CVec Offset = CVec.Zero;

		[Desc("Facing that the actor must face before transforming.")]
		public readonly int Facing = 96;

		[Desc("Sounds to play when transforming.")]
		public readonly string[] TransformSounds = { };

		[Desc("Sounds to play when the transformation is blocked.")]
		public readonly string[] NoTransformSounds = { };

		[Desc("Notification to play when transforming.")]
		public readonly string TransformNotification = null;

		[Desc("Notification to play when the transformation is blocked.")]
		public readonly string NoTransformNotification = null;

		[Desc("Cursor to display when able to (un)deploy the actor.")]
		public readonly string DeployCursor = "deploy";

		[Desc("Cursor to display when unable to (un)deploy the actor.")]
		public readonly string DeployBlockedCursor = "deploy-blocked";
		
		public readonly bool SkipMakeAnimation = false;
		
		public readonly bool SkipUndoAnimation = false;

		[VoiceReference] public readonly string Voice = "Action";

		public virtual object Create(ActorInitializer init) { return new SkipTransforms(init, this); }
	}

	public class SkipTransforms : IIssueOrder, IResolveOrder, IOrderVoice, IIssueDeployOrder
	{
		readonly Actor self;
		readonly SkipTransformsInfo info;
		readonly BuildingInfo buildingInfo;
		readonly string faction;

		public SkipTransforms(ActorInitializer init, SkipTransformsInfo info)
		{
			self = init.Self;
			this.info = info;
			buildingInfo = self.World.Map.Rules.Actors[info.IntoActor].TraitInfoOrDefault<BuildingInfo>();
			faction = init.Contains<FactionInit>() ? init.Get<FactionInit, string>() : self.Owner.Faction.InternalName;
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return (order.OrderString == "DeployTransform") ? info.Voice : null;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new DeployOrderTargeter("DeployTransform", 5,
				() => info.DeployCursor); }
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "DeployTransform")
				return new Order(order.OrderID, self, queued);

			return null;
		}

		Order IIssueDeployOrder.IssueDeployOrder(Actor self)
		{
			return new Order("DeployTransform", self, false);
		}

		public void DeployTransform(bool queued)
		{
			if (!queued)
				self.CancelActivity();

			self.QueueActivity(new InstantTransform(self, info.IntoActor)
			{
				Offset = info.Offset,
				Facing = info.Facing,
				Sounds = info.TransformSounds,
				Notification = info.TransformNotification,
				Faction = faction,
				SkipMakeAnims = info.SkipMakeAnimation,
				SkipSelfMakeAnims = info.SkipUndoAnimation
			});
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "DeployTransform")
				DeployTransform(order.Queued);
		}
	}
}
