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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits.BuildingTraits
{
    public enum SpawnType
    {
        PlaceBuilding,
        Deploy,
        Other
    }

    public class SelfConstructingInfo : WithMakeAnimationInfo, ITraitInfo, Requires<ConditionManagerInfo>,
        Requires<HealthInfo>, Requires<RenderSpritesInfo>
    {
        [Desc("Number of make sequences.")] public readonly int Steps = 3;

        [Desc("Apply to sprite bodies with these names.")]
        public new readonly string[] BodyNames = { "body" };

        public readonly string Scaffolds = "scaffolds";

        public readonly bool UseScaffolds = true;

        public readonly bool ShowPercentage = true;

        public readonly bool ShowQueuePosition = true;

        public readonly string Font = "MediumBold";

        public readonly string SmallFont = "Regular";

        [Desc("The Z offset to apply when rendering this decoration.")]
        public readonly int ZOffset = 12048;

        public readonly int[] Offset = { 0, 0 };

        [PaletteReference] public readonly string ScaffoldsPalette = "mwcivilian";

        public new object Create(ActorInitializer init)
        {
            return new SelfConstructing(init, this);
        }
    }

    public class SelfConstructing : WithMakeAnimation, ITick, INotifyRemovedFromWorld, INotifyCreated,
        INotifyDamageStateChanged, INotifyKilled, IRender
    {
        public readonly SelfConstructingInfo Info;

        private readonly WithSpriteBody[] wsbs;

        private readonly ConditionManager conditionManager;

        private readonly Dictionary<CVec, int> scaffolds = new Dictionary<CVec, int>();
        private readonly SpriteFont font;
        private readonly SpriteFont smallFont;
        private readonly IDecorationBounds[] decorationBounds;

        private int token = ConditionManager.InvalidConditionToken;

        private ProductionItem productionItem;

        private List<int> healthSteps;
        private Health health;
        private int step;
        private SpawnType spawnType;
        private RenderSpritesInfo renderSprites;
        private SelfConstructingProductionQueue queue;

        public SelfConstructing(ActorInitializer init, SelfConstructingInfo info) : base(init, info)
        {
            this.Info = info;
            wsbs = init.Self.TraitsImplementing<WithSpriteBody>().Where(w => info.BodyNames.Contains(w.Info.Name)).ToArray();
            conditionManager = init.Self.Trait<ConditionManager>();
            font = Game.Renderer.Fonts[info.Font];
            smallFont = Game.Renderer.Fonts[info.SmallFont];
            decorationBounds = init.Self.TraitsImplementing<IDecorationBounds>().ToArray();

            if (!string.IsNullOrEmpty(this.Info.Condition) && token == ConditionManager.InvalidConditionToken)
                token = conditionManager.GrantCondition(init.Self, this.Info.Condition);

            spawnType = init.Contains<PlaceBuildingInit>() ? SpawnType.PlaceBuilding :
                init.Contains<SpawnedByMapInit>() ? SpawnType.Other : SpawnType.Deploy;

            var footPrintInfo = init.Self.Info.TraitInfoOrDefault<BuildingInfo>();
            if (footPrintInfo == null)
                return;

            var occupiedCells = footPrintInfo.Footprint.Where(c => c.Value == FootprintCellType.Occupied)
                .Select(kv => kv.Key).ToArray();

            foreach (var cell in occupiedCells)
                scaffolds.Add(cell, CheckSorroundings(occupiedCells, cell));

            renderSprites = init.Self.Info.TraitInfo<RenderSpritesInfo>();
        }

        int CheckSorroundings(CVec[] cells, CVec cell)
        {
            var sides = 0;
            sides |= (cells.Contains(cell + new CVec(0, -1)) ? 1 : 0) << 0;
            sides |= (cells.Contains(cell + new CVec(-1, 0)) ? 1 : 0) << 1;
            sides |= (cells.Contains(cell + new CVec(0, 1)) ? 1 : 0) << 2;
            sides |= (cells.Contains(cell + new CVec(1, 0)) ? 1 : 0) << 3;
            return sides;
        }

        IEnumerable<IRenderable> IRender.Render(Actor self, WorldRenderer worldRenderer)
        {
            if (token == ConditionManager.InvalidConditionToken || wsbs.FirstEnabledTraitOrDefault() == null)
                yield break;

            if (Info.UseScaffolds)
                foreach (var entry in scaffolds)
                {
                    for (var i = 0; i < 4; i++)
                    {
                        if (((entry.Value >> i) & 1) == 1)
                            continue;

                        var sequence = self.World.Map.Rules.Sequences.GetSequence(renderSprites.Image ?? self.Info.Name,
                            Info.Scaffolds + i);

                        yield return
                            new SpriteRenderable(
                                sequence.GetSprite(step),
                                self.World.Map.CenterOfCell(self.Location + entry.Key),
                                WVec.Zero,
                                sequence.ZOffset,
                                worldRenderer.Palette(Info.ScaffoldsPalette),
                                1,
                                false);
                    }
                }

            if (Info.ShowPercentage)
            {
                var bounds = decorationBounds.Select(b => b.DecorationBounds(self, worldRenderer))
                    .FirstOrDefault(b => !b.IsEmpty);
                var spaceBuffer = (int)(10 / worldRenderer.Viewport.Zoom);
                var effectPos = worldRenderer.ProjectedPosition(new int2(
                    (bounds.Left + bounds.Right) / 2 + Info.Offset[0],
                    (bounds.Top + bounds.Bottom) / 2 + Info.Offset[1] - spaceBuffer));
                var allQueued = queue.AllActuallyQueued().ToArray();

                if (worldRenderer.World.RenderPlayer != null &&
                    worldRenderer.World.RenderPlayer.IsAlliedWith(self.Owner))
                {
                    yield return
                        new TextRenderable(font, effectPos + new WVec(0, -350, 0), Info.ZOffset, Color.White,
                            allQueued.IndexOf(productionItem) == 0 ? "Building" : allQueued.IndexOf(productionItem).ToString());
                }

                if (Info.ShowQueuePosition)
                    yield return
                        new TextRenderable(
                            smallFont,
                            effectPos + new WVec(0, 350, 0),
                            Info.ZOffset,
                            Color.White,
                            (int)Math.Round(100.0 / productionItem.TotalTime * (productionItem.TotalTime - productionItem.RemainingTime), 0) + "%");
            }
        }

        public bool IsActive()
        {
            return wsbs.FirstEnabledTraitOrDefault() != null;
        }

        public IEnumerable<Rectangle> ScreenBounds(Actor self, WorldRenderer wr)
        {
            if (!self.IsDead && self.IsInWorld && wsbs.FirstEnabledTraitOrDefault() != null)
                return self.Trait<RenderSprites>().ScreenBounds(self, wr);

            return new Rectangle[] { };
        }

        void INotifyCreated.Created(Actor self)
        {
            if (wsbs.FirstEnabledTraitOrDefault() == null)
            {
                OnComplete(self);
                return;
            }

            if (spawnType == SpawnType.PlaceBuilding)
            {
                var productionActor = self.World.Actors.FirstOrDefault(a =>
                    a.Owner == self.Owner && a.TraitsImplementing<SelfConstructingProductionQueue>()
                        .Any(q => q.AllItems().Contains(self.Info)));
                queue = productionActor.TraitsImplementing<SelfConstructingProductionQueue>()
                    .First(q => q.AllItems().Contains(self.Info));
                var valued = self.Info.TraitInfoOrDefault<ValuedInfo>();
                productionItem = new SelfConstructingProductionItem(queue, self,
                    valued == null ? 0 : valued.Cost, null, null);
                queue.BeginProduction(productionItem, false);

                health = self.Trait<Health>();

                healthSteps = new List<int>();
                for (var i = 0; i <= Info.Steps; i++)
                    healthSteps.Add(health.MaxHP * (i + 1) / (Info.Steps + 1));

                health.InflictDamage(self, self, new Damage(health.MaxHP - healthSteps[0]), true);

                var wsb = wsbs.FirstEnabledTraitOrDefault();
                wsb.CancelCustomAnimation(self);
                wsb.PlayCustomAnimationRepeating(self, Info.Sequence + 0);
            }
            else if (spawnType == SpawnType.Deploy)
            {
                var wsb = wsbs.FirstEnabledTraitOrDefault();
                wsb.CancelCustomAnimation(self);
                wsb.PlayCustomAnimation(self, "deploy", () => OnComplete(self));
            }
            else
                OnComplete(self);
        }

        private void OnComplete(Actor self)
        {
            if (token != ConditionManager.InvalidConditionToken)
                token = conditionManager.RevokeCondition(self, token);
        }

        void ITick.Tick(Actor self)
        {
            if (wsbs.FirstEnabledTraitOrDefault() == null)
                return;

            if (productionItem == null)
                return;

            if (productionItem.Done)
            {
                productionItem.Queue.EndProduction(productionItem);
                productionItem = null;
                var wsb = wsbs.FirstEnabledTraitOrDefault();
                wsb.CancelCustomAnimation(self);

                while (step < Info.Steps)
                    health.InflictDamage(self, self, new Damage(healthSteps[step] - healthSteps[++step]), true);

                OnComplete(self);
                return;
            }

            var progress = Math.Max(0,
                Math.Min(Info.Steps * (productionItem.TotalTime - productionItem.RemainingTime) / Math.Max(1, productionItem.TotalTime), Info.Steps - 1));

            if (progress != step)
            {
                while (step < progress)
                    health.InflictDamage(self, self, new Damage(healthSteps[step] - healthSteps[++step]), true);

                var wsb = wsbs.FirstEnabledTraitOrDefault();
                wsb.PlayCustomAnimationRepeating(self, Info.Sequence + step);
            }
        }

        void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
        {
            if (wsbs.FirstEnabledTraitOrDefault() == null)
                return;

            if (productionItem != null)
                productionItem.Queue.EndProduction(productionItem);
        }

        public ProductionItem TryAbort(Actor self)
        {
            if (wsbs.FirstEnabledTraitOrDefault() == null)
                return null;

            if (productionItem == null)
                return null;

            var item = productionItem;

            productionItem.Queue.EndProduction(productionItem);
            productionItem = null;
            OnComplete(self);

            return item;
        }

        void INotifyDamageStateChanged.DamageStateChanged(Actor self, AttackInfo e)
        {
            if (wsbs.FirstEnabledTraitOrDefault() == null)
                return;

            if (productionItem == null)
                return;

            var wsb = wsbs.FirstEnabledTraitOrDefault();
            wsb.PlayCustomAnimationRepeating(self, Info.Sequence + step);
        }

        void INotifyKilled.Killed(Actor self, AttackInfo e)
        {
            if (wsbs.FirstEnabledTraitOrDefault() == null)
                return;

            if (productionItem != null)
                productionItem.Queue.EndProduction(productionItem);
        }
    }
}