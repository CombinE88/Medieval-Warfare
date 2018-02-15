﻿#region Copyright & License Information
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
using OpenRA.Graphics;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits.Render
{
	[Desc("Renders a decorative animation on units and buildings.")]
	public class WithUndeadBuilderOverlayInfo : PausableConditionalTraitInfo, IRenderActorPreviewSpritesInfo, Requires<RenderSpritesInfo>, Requires<BodyOrientationInfo>
	{
		[Desc("Animation to play when the actor is created.")]
		[SequenceReference] public readonly string StartSequence = null;

		[Desc("Sequence name to use")]
		[SequenceReference] public readonly string Sequence = "idle-overlay";

		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		[Desc("Custom palette name")]
		[PaletteReference("IsPlayerPalette")] public readonly string Palette = null;

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		public override object Create(ActorInitializer init) { return new WithUndeadBuilderOverlay(init.Self, this); }

		public IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, RenderSpritesInfo rs, string image, int facings, PaletteReference p)
		{
			if (!EnabledByDefault)
				yield break;

			if (Palette != null)
				p = init.WorldRenderer.Palette(Palette);

			Func<int> facing;
			if (init.Contains<DynamicFacingInit>())
				facing = init.Get<DynamicFacingInit, Func<int>>();
			else
			{
				var f = init.Contains<FacingInit>() ? init.Get<FacingInit, int>() : 0;
				facing = () => f;
			}

			var anim = new Animation(init.World, image, facing);
			anim.PlayRepeating(RenderSprites.NormalizeSequence(anim, init.GetDamageState(), Sequence));

			var body = init.Actor.TraitInfo<BodyOrientationInfo>();
			Func<WRot> orientation = () => body.QuantizeOrientation(WRot.FromFacing(facing()), facings);
			Func<WVec> offset = () => body.LocalToWorld(Offset.Rotate(orientation()));
			Func<int> zOffset = () =>
			{
				var tmpOffset = offset();
				return tmpOffset.Y + tmpOffset.Z + 1;
			};

			yield return new SpriteActorPreview(anim, offset, zOffset, p, rs.Scale);
		}
	}

	public class WithUndeadBuilderOverlay : PausableConditionalTrait<WithUndeadBuilderOverlayInfo>, INotifyDamageStateChanged, INotifyBuildComplete, INotifySold, INotifyTransform, ITick
	{
		readonly Animation overlay;
        bool buildComplete;
		

		private AnimationWithOffset anim;
		private int animationlength;
		private int anomationframe;
		private UndeadBuilder UD;

		public WithUndeadBuilderOverlay(Actor self, WithUndeadBuilderOverlayInfo info)
			: base(info)
		{
			var rs = self.Trait<RenderSprites>();
			var body = self.Trait<BodyOrientation>();

			UD = self.TraitsImplementing<UndeadBuilder>().FirstOrDefault();

			buildComplete = !self.Info.HasTraitInfo<BuildingInfo>(); // always render instantly for units
			
			overlay = new Animation(self.World, rs.GetImage(self), () => IsTraitPaused || !buildComplete);
			overlay.PlayFetchIndex(info.Sequence, () => anomationframe);

			anim = new AnimationWithOffset(overlay,
				() => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(self, self.Orientation))),
				() => IsTraitDisabled || !buildComplete,
				p => RenderUtils.ZOffsetFromCenter(self, p, 1));

			rs.Add(anim, info.Palette, info.IsPlayerPalette);

			animationlength = anim.Animation.CurrentSequence.Length;
		}

		void INotifyBuildComplete.BuildingComplete(Actor self)
		{
			buildComplete = true;
		}

		void INotifySold.Sold(Actor self) { }
		void INotifySold.Selling(Actor self)
		{
			buildComplete = false;
		}

		void INotifyTransform.BeforeTransform(Actor self)
		{
			buildComplete = false;
		}

		void INotifyTransform.OnTransform(Actor self) { }
		void INotifyTransform.AfterTransform(Actor self) { }

		void INotifyDamageStateChanged.DamageStateChanged(Actor self, AttackInfo e)
		{
			overlay.ReplaceAnim(RenderSprites.NormalizeSequence(overlay, e.DamageState, overlay.CurrentSequence.Name));
		}

		public void Tick(Actor self)
		{
			float bruch = UD.hassummoningcount*100 / UD.info.SummoningTime;
			anomationframe = (int) (animationlength * bruch)/100;
		}
	}
}
