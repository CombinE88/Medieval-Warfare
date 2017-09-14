using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits
{
	public class ResourceProviderInfo : TraitInfo<ResourceProvider>
	{
		[FieldLoader.Require]
		[Desc("Wich types of resources is provided")]
		public readonly HashSet<string> Types = new HashSet<string>();
	}

	
	
	
	public class ResourceProvider { }
}
