using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits
{
    [Desc("How much the unit is worth in Peasants.")]
    public class LootableBodyInfo : ITraitInfo
    {
        [FieldLoader.Require]
        [Desc("Lootable Type")]
        public readonly HashSet<string> LootTypes = new HashSet<string>();

        public object Create(ActorInitializer init) { return new LootableBody(this); }
    }

    public class LootableBody
    {
        public Actor Hunter;

        public LootableBody(LootableBodyInfo lootableBodyInfo)
        {
        }
    }
}
