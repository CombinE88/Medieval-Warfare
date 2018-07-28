using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Warheads
{
    public class DecayResourceWarhead : Warhead
    {
        [Desc("Size of the area. The resources are decaying within this area.", "Provide 2 values for a ring effect (outer/inner).")]
        public readonly int[] Size = { 0, 0 };
        [Desc("Number of resources are harvested within this area.")]
        public readonly int Amount = 1;

        [Desc("Name of allowed resources wich can decay.")]
        public readonly HashSet<string> ResourceTypes = new HashSet<string>();

        public override void DoImpact(Target target, Actor firedBy, IEnumerable<int> damageModifiers)
        {
            var world = firedBy.World;
            var targetTile = world.Map.CellContaining(target.CenterPosition);
            var resLayer = world.WorldActor.Trait<ResourceLayer>();

            var minRange = (Size.Length > 1 && Size[1] > 0) ? Size[1] : 0;
            var allCells = world.Map.FindTilesInAnnulus(targetTile, minRange, Size[0]);

            foreach (var cell in allCells)
            {
                for (int i = 1; i <= Amount; i++)
                {
                    if (resLayer.GetResourceDensity(cell) >= 1 && ResourceTypes.Contains(resLayer.GetResource(cell).Info.Name))
                    {
                        resLayer.Harvest(cell);
                        if (resLayer.GetResourceDensity(cell) <= 0)
                        {
                            resLayer.Destroy(cell);
                        }
                    }
                }
            }
        }
    }
}