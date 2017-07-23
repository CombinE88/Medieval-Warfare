using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.MW.Traits
{
	public class CritterSpawnerInfo : ConditionalTraitInfo
	{
		[ActorReference]
		public readonly string[] Actors = null;

		public readonly int Chance = 100;

		public readonly int AliveMax = 1;

		public readonly int TicksMin = 1000;

		public readonly int TicksMax = 1000;

		public readonly WDist RadiusMin = new WDist(0);

		public readonly WDist RadiusMax = new WDist(5);

		public readonly bool CheckReachability = false;

		public override object Create(ActorInitializer init) { return new CritterSpawner(init.Self, this); }
	}

	public class CritterSpawner : ConditionalTrait<CritterSpawnerInfo>, ITick, INotifyCreated
	{
		readonly Actor self;
		private readonly List<Actor> alive = new List<Actor>();
		private CritterSpawnerInfo info;
		private int timer;
		private HashSet<CPos> forbiddenCells;

		public CritterSpawner(Actor self, CritterSpawnerInfo info)
			: base(info)
		{
			this.self = self;
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			if (info.CheckReachability)
			{
				checkstartingcells(self);
			}
		}

		void checkstartingcells(Actor self)
		{
			forbiddenCells = new HashSet<CPos>();
			
			var cells = self.World.Map.FindTilesInAnnulus(self.Location, info.RadiusMin.Length, info.RadiusMax.Length)
				.Where(a =>
				{
					if (self.World.Map.Rules.Actors[Info.Actors.First()].HasTraitInfo<IPositionableInfo>())
					{
						var ip = self.World.Map.Rules.Actors[Info.Actors.First()].TraitInfo<IPositionableInfo>();
						if (ip.CanEnterCell(self.World, null, a))
							return true;
					}
	
					else if (self.World.Map.Rules.Actors[Info.Actors.First()].HasTraitInfo<BuildingInfo>())
					{
						var ip2 = self.World.Map.Rules.Actors[Info.Actors.First()].TraitInfo<BuildingInfo>();
						if (self.World.CanPlaceBuilding(Info.Actors.First(), ip2, a, null))
							return true;
					}
					else if (!self.World.Map.Rules.Actors[Info.Actors.First()].HasTraitInfo<BuildingInfo>() && !self.World.Map.Rules.Actors[Info.Actors.First()].HasTraitInfo<IPositionableInfo>())
					{
						return true;
					}
					return false;
				});
			
			var allaround = self.World.Map.FindTilesInCircle(self.Location, info.RadiusMax.Length, true)
				.Where(c =>
				{
					if (cells.Contains(c))
						return false;
					return true;
				});

			var cPoses = allaround as CPos[] ?? allaround.ToArray();
			if (cPoses.Any())
				foreach (var cell in cPoses)
				{
					forbiddenCells.Add(cell);
				}	
			

			HashSet<CPos> checkcells = new HashSet<CPos>();

			if (cells != null && cells.Any())
			{
				foreach (var cell in cells)
				{
				
					List<CPos> path;
					
					using (var ThePath = PathSearch.FromPoint(self.World,
						self.World.Map.Rules.Actors["e4new"].TraitInfo<MobileInfo>(),
						self, self.Location, cell, true))
						path = self.World.WorldActor.Trait<IPathFinder>().FindPath(ThePath);
					if (info.RadiusMax.Length < path.Count || path.Count == 0)
					{
						forbiddenCells.Add(cell);
					}
					else
						checkcells.Add(cell);
					
				}
			}
			if (checkcells != null && checkcells.Any())
			{
				foreach (var cell in cells)
				{
					if (!checkcells.Contains(cell))
						forbiddenCells.Add(cell);
				}
			}
			

		}

		public void Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;
			
			timer++;
			alive.Remove(alive.Find(a => a.IsDead));

			if (alive.Count >= info.AliveMax)
			{
				timer = 0;
				return;
			}

			if (info.TicksMin >= timer)
				return;

			if (info.TicksMax >= timer && self.World.SharedRandom.Next(0, info.Chance) != 0)
				return;

			
			self.World.AddFrameEndTask(w =>
			{
				var cells = self.World.Map.FindTilesInAnnulus(self.Location, info.RadiusMin.Length, info.RadiusMax.Length)
					.Where(c =>
						{
							if (info.CheckReachability && forbiddenCells.Contains(c))
								return false;

							return true;
						});
				var actor = info.Actors[self.World.SharedRandom.Next(0, info.Actors.Length)].ToLowerInvariant();
				IPositionableInfo ip = null;
				BuildingInfo ip2 = null;

				IEnumerable<CPos> validCells = null;
	
				if (self.World.Map.Rules.Actors[actor].HasTraitInfo<IPositionableInfo>())
				{
					ip = self.World.Map.Rules.Actors[actor].TraitInfo<IPositionableInfo>();
					validCells = cells.Where(c => ip.CanEnterCell(self.World, null, c));
				}
				
				else if (self.World.Map.Rules.Actors[actor].HasTraitInfo<BuildingInfo>())
				{
					ip2 = self.World.Map.Rules.Actors[actor].TraitInfo<BuildingInfo>();
					validCells = cells.Where(c => self.World.CanPlaceBuilding(actor,ip2,c,null));
				}


				if (validCells != null && !validCells.Any())
					return;
				
				var randomlocation = validCells.ElementAt(self.World.SharedRandom.Next(validCells.Count()));

				var facong = 0;
				var face = self.TraitOrDefault<IFacing>();
				if (face != null)
					facong = face.Facing;

				var td = new TypeDictionary
				{
					new LocationInit(randomlocation),
					new CenterPositionInit(self.World.Map.CenterOfCell(randomlocation)),
					new FacingInit(facong),
					new OwnerInit(self.Owner),
				};

				var Create = w.CreateActor(actor, td);
				alive.Add(Create);
				});
		timer = 0;
		}
	}
}