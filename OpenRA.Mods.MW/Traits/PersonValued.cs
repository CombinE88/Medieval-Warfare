using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Mw.Traits
{
	[Desc("How much the unit is worth in Peasants.")]
	public class PersonValuedInfo : TraitInfo<PersonValued>
	{
		[FieldLoader.Require]
		[Desc("Used in production. Set 0 to not need an actor to be converted")]
		public readonly int ActorCount = 1;

		public readonly HashSet<string> ConvertingActors = new HashSet<string>();
	}

	public class PersonValued { }
}
