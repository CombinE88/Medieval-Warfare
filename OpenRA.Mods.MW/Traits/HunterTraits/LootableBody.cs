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
		
		public object Create(ActorInitializer init) { return new LootableBody(init, this); }
	}

	public class LootableBody
	{
		
		readonly LootableBodyInfo info;
		
		public Actor Hunter;

		public LootableBody(ActorInitializer init, LootableBodyInfo info)
		{
			this.info = info;
		}

	}
}
