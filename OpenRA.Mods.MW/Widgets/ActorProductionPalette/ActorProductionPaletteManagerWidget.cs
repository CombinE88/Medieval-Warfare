using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.MW.Traits.BuildingTraits;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.MW.Widgets.ActorProductionPalette
{
    public class ActorProductionPaletteManagerWidget : Widget
    {
        public readonly string Background;
        private readonly World world;
        private readonly WorldRenderer worldRenderer;
        private readonly ModData modData;
        private readonly OrderManager orderManager;

        private Dictionary<Actor, ActorProductionPaletteWidget> palettes =
            new Dictionary<Actor, ActorProductionPaletteWidget>();

        [ObjectCreator.UseCtor]
        public ActorProductionPaletteManagerWidget(World world, WorldRenderer worldRenderer, ModData modData,
            OrderManager orderManager)
        {
            Bounds = new Rectangle(Point.Empty, Game.Renderer.Resolution);
            this.world = world;
            this.worldRenderer = worldRenderer;
            this.modData = modData;
            this.orderManager = orderManager;
        }

        public override void Tick()
        {
            var selectedFactoryActors = world.Selection.Actors.Where(actor =>
            {
                if (actor.Owner != actor.World.LocalPlayer || actor.IsDead || !actor.IsInWorld)
                    return false;

                var productionQueue = actor.TraitsImplementing<UpgradeProductionQueue>().FirstOrDefault(p =>
                {
                    var pro = actor.TraitOrDefault<Production>();
                    if (pro == null)
                        return false;

                    if (!pro.Info.Produces.Contains(p.Info.Type))
                        return false;

                    if (pro.IsTraitDisabled)
                        return false;

                    return true;
                });

                return productionQueue != null;
            }).ToArray();

            var previouslySelectedActors = palettes.Keys.ToArray();

            foreach (var previouslySelectedActor in previouslySelectedActors)
            {
                if (selectedFactoryActors.Contains(previouslySelectedActor))
                    continue;

                RemoveChild(palettes[previouslySelectedActor]);
                palettes.Remove(previouslySelectedActor);
            }

            foreach (var selectedFactoryActor in selectedFactoryActors)
            {
                if (palettes.ContainsKey(selectedFactoryActor))
                    continue;

                var productionQueue = selectedFactoryActor.TraitsImplementing<UpgradeProductionQueue>().FirstOrDefault(
                    p =>
                    {
                        var pro = selectedFactoryActor.TraitOrDefault<Production>();
                        if (pro == null)
                            return false;

                        if (!pro.Info.Produces.Contains(p.Info.Type))
                            return false;

                        if (pro.IsTraitDisabled)
                            return false;

                        return true;
                    });

                var palette = new ActorProductionPaletteWidget(modData, orderManager, this, selectedFactoryActor,
                    worldRenderer, world, productionQueue);
                AddChild(palette);
                palettes.Add(selectedFactoryActor, palette);
            }
        }
    }
}