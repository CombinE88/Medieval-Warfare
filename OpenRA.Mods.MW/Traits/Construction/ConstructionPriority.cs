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
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.MW.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits.Render
{
	[Desc("Displays a text overlay relative to the selection box.")]
	public class ConstructionPriorityInfo : ITraitInfo
	{
		public readonly string PriorityCondition = null;
		
		public readonly string Font = "TinyBold";

		[Desc("Display in this color when not using the player color.")]
		public readonly Color Color = Color.White;

		[Desc("Use the player color of the current owner.")]
		public readonly bool UsePlayerColor = false;

		[Desc("Point in the actor's selection box used as reference for offsetting the decoration image. " +
		      "Possible values are combinations of Center, Top, Bottom, Left, Right.")]
		public readonly ReferencePoints ReferencePoint = ReferencePoints.Top | ReferencePoints.Left;

		[Desc("The Z offset to apply when rendering this decoration.")]
		public readonly int ZOffset = 5555;

		[Desc("Player stances who can view the decoration.")]
		public readonly Stance ValidStances = Stance.Ally;

		[Desc("Should this be visible only when selected?")]
		public readonly bool RequiresSelection = false;
		
		public readonly string DeployCursor = "deploy";

		public object Create(ActorInitializer init) { return new ConstructionPriority(init.Self, this); }
	}

	public class ConstructionPriority : IRender, IRenderAboveShroudWhenSelected, INotifyCapture, INotifyCreated, IIssueOrder, IResolveOrder
	{
		readonly SpriteFont font;
		Color color;

		private Actor self;
		private ConstructionPriorityInfo Info;

		public int Priority;
		
		ConditionManager conditionManager;
		public HashSet<int> Conditions = new HashSet<int>();
		

		public ConstructionPriority(Actor self, ConstructionPriorityInfo info)
		{
			this.self = self;
			this.Info = info;
			font = Game.Renderer.Fonts[info.Font];
			color = info.UsePlayerColor ? self.Owner.Color.RGB : info.Color;
		}
		
		public void Created(Actor self)
		{
			conditionManager = self.TraitOrDefault<ConditionManager>();
		}

		public void addCondition()
		{
			if (conditionManager != null && Conditions.Count<5)
			{
				var token = conditionManager.GrantCondition(self, Info.PriorityCondition);
				Conditions.Add(token);
			}
		}

		public void removecondition()
		{
			if (conditionManager != null && Conditions.Any())
			{
				var token = Conditions.Last();
				conditionManager.RevokeCondition(self, token);
				Conditions.Remove(token);
			}

		}

		public virtual bool ShouldRender(Actor self) { return true; }

		IEnumerable<IRenderable> IRender.Render(Actor self, WorldRenderer wr)
		{
			return !Info.RequiresSelection ? RenderInner(self, wr) : SpriteRenderable.None;
		}

		IEnumerable<IRenderable> IRenderAboveShroudWhenSelected.RenderAboveShroud(Actor self, WorldRenderer wr)
		{
			return Info.RequiresSelection ? RenderInner(self, wr) : SpriteRenderable.None;
		}

		IEnumerable<IRenderable> RenderInner(Actor self, WorldRenderer wr)
		{
			if (self.IsDead || !self.IsInWorld)
				return Enumerable.Empty<IRenderable>();

			if (self.World.RenderPlayer != null)
			{
				var stance = self.Owner.Stances[self.World.RenderPlayer];
				if (!Info.ValidStances.HasStance(stance))
					return Enumerable.Empty<IRenderable>();
			}

			if (!ShouldRender(self) || self.World.FogObscures(self))
				return Enumerable.Empty<IRenderable>();

			var bounds = self.VisualBounds;
			var halfSize = font.Measure(Conditions.Count.ToString()) / 2;

			var boundsOffset = new int2(bounds.Left + bounds.Right, bounds.Top + bounds.Bottom) / 2;
			var sizeOffset = new int2();
			if (Info.ReferencePoint.HasFlag(ReferencePoints.Top))
			{
				boundsOffset -= new int2(0, bounds.Height / 2);
				sizeOffset += new int2(0, halfSize.Y);
			}
			else if (Info.ReferencePoint.HasFlag(ReferencePoints.Bottom))
			{
				boundsOffset += new int2(0, bounds.Height / 2);
				sizeOffset -= new int2(0, halfSize.Y);
			}

			if (Info.ReferencePoint.HasFlag(ReferencePoints.Left))
			{
				boundsOffset -= new int2(bounds.Width / 2, 0);
				sizeOffset += new int2(halfSize.X, 0);
			}
			else if (Info.ReferencePoint.HasFlag(ReferencePoints.Right))
			{
				boundsOffset += new int2(bounds.Width / 2, 0);
				sizeOffset -= new int2(halfSize.X, 0);
			}

			var screenPos = wr.ScreenPxPosition(self.CenterPosition) + boundsOffset + sizeOffset;
			return new IRenderable[] { new TextRenderable(font, wr.ProjectedPosition(screenPos), Info.ZOffset, color, Conditions.Count.ToString()) };
		}

		void INotifyCapture.OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
			if (Info.UsePlayerColor)
				color = newOwner.Color.RGB;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new AddPrioTargeter("AddPrio", 5,
					() => "default");
				yield return new RemPrioTargeter("RemPrio", 5,
					() => "default");
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "AddPrio")
			{
				return new Order(order.OrderID, self, false);
			}
			
			if (order.OrderID == "RemPrio")
			{
			return new Order(order.OrderID, self, false);
			}

			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "AddPrio")
			{
				addCondition();
			}
			
			if (order.OrderString == "RemPrio")
			{
				removecondition();
			}
		}

        public IEnumerable<Rectangle> ScreenBounds(Actor self, WorldRenderer wr)
        {
            throw new NotImplementedException();
        }
    }
}



