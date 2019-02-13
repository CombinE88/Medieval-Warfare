using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits.BuildingTraits
{
    public class UpgradeBuildableInfo : BuildableInfo
    {
        [GrantedConditionReference] [Desc("The condition to grant when producing this actor.")]
        public readonly string ConditionWhileBuild = null;

        [GrantedConditionReference] [Desc("The condition to grant when this actor is finished building")]
        public readonly string ConditionOnFinish = null;

        [GrantedConditionReference] [Desc("The type of this addon")]
        public readonly string BuildType = "upgrade";

        [Desc("Damage percentage versus each armortype.")]
        public readonly Dictionary<string, string> TypeRequirements = new Dictionary<string, string>();
    }

    public class UpgradeBuildable
    {
    }
}