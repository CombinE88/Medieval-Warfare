using System;
using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using System.Linq;
using OpenRA.Mods.Common.Activities;


namespace OpenRA.Mods.MW.Traits
{
	public class FormationCoreInfo : ITraitInfo
	{


		public object Create(ActorInitializer init) { return new FormationCore(init, this); }
	}

	public class FormationCore : IAutoSelectionSize, ITick
	{

		public Actor FormationCoreActor;
		public HashSet<Actor> MeeleActors = new HashSet<Actor>();
		public HashSet<Actor> SecondRowActors = new HashSet<Actor>();
		public HashSet<Actor> RangedActors = new HashSet<Actor>();
		public HashSet<Actor> SiegeActors = new HashSet<Actor>();
		
		public HashSet<Actor> AllActors = new HashSet<Actor>();
		
		public int2 Bounds;
		public Actor self;
		private readonly FormationCoreInfo info;
		


		public FormationCore(ActorInitializer init, FormationCoreInfo info)
		{
			self = init.Self;
			this.info = info;
		}

		public int2 SelectionSize(Actor self)
		{
			return Bounds;
		}
		
		public HashSet<Actor> JoinHash(HashSet<Actor> hash1, HashSet<Actor> hash2)
		{
			if (hash1.Any())
			{
				foreach (var n in hash1)
				{
					hash2.Add(n);
				}
				return hash2;
			}
			return hash2;
		}

		public int2 FindBoundsFromDistance(HashSet<Actor> hash, Actor self)
		{
			if (hash.Any())
			{
				var distancesX = new List<int>();
				var distancesY = new List<int>();
				foreach (var vari in hash)
				{
					distancesX.Add((vari.CenterPosition - self.CenterPosition).HorizontalLength );
					distancesY.Add((vari.CenterPosition - self.CenterPosition).VerticalLength );
				}
				return new int2(distancesX.Max()*2,distancesY.Max()*2);

			}
			
			
			return new int2(24,24);
		}

		void ITick.Tick(Actor self)
		{
			FindBoundsFromDistance(AllActors, self);

		}
		
	}
}