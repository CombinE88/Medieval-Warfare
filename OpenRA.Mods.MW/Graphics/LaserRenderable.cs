#region Copyright & License Information
/*
 * Copyright 2016-2018 The KKnD Developers (see AUTHORS)
 * This file is part of KKnD, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Drawing;
using System.Linq;
using OpenRA.Graphics;

namespace OpenRA.Mods.MW.Graphics
{
	public struct LaserRenderable : IRenderable, IFinalizedRenderable
	{
		readonly WPos[] offsets;
		readonly int zOffset;
		readonly WDist width;
		readonly Color color;

		public LaserRenderable(WPos[] offsets, int zOffset, WDist width, Color color)
		{
			this.offsets = offsets;
			this.zOffset = zOffset;
			this.width = width;
			this.color = color;
		}

		public WPos Pos { get { return new WPos(offsets[0].X, offsets[0].Y, 0); } }
		public PaletteReference Palette { get { return null; } }
		public int ZOffset { get { return zOffset; } }
		public bool IsDecoration { get { return true; } }

		public IRenderable WithPalette(PaletteReference newPalette) { return this; }
		public IRenderable WithZOffset(int newOffset) { return new LaserRenderable(offsets, newOffset, width, color); }
		public IRenderable OffsetBy(WVec vec) { return new LaserRenderable(offsets.Select(offset => offset + vec).ToArray(), zOffset, width, color); }
		public IRenderable AsDecoration() { return this; }

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }
		public void Render(WorldRenderer wr)
		{
			// TODO fix connectSegments - asin smoothen the edge of a break!
			var screenWidth = wr.ScreenVector(new WVec(width, WDist.Zero, WDist.Zero))[0];

			Game.Renderer.WorldRgbaColorRenderer.DrawLine(offsets.Select(offset => wr.Screen3DPosition(offset)), screenWidth, color, true);
		}

		public void RenderDebugGeometry(WorldRenderer wr) { }
		public Rectangle ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }
	}
}