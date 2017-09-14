using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits.ArmyOfTheDeath
{
	[Desc("A actor has to enter the building before the unit spawns.")]
	public class DeadArmyHandlerInfo : ITraitInfo
	{
		
		[Desc("The range in cells where should be looked for possible Soldiers.")]
		public readonly WDist SearhRadius = WDist.FromCells(15);
		
		[Desc("Size of the Army.")]
		public readonly int ArmySize = 3;
		
		[Desc("Size of the Commanding Units.")]
		public readonly int CommandoWaveAdder = 4;
		public readonly bool NeedsCommando = false;
		
		[Desc("Size of the tlity Units.")]
		public readonly int UtlityWaveAdder = 3;
		public readonly bool Needstlity = false;
		
		[Desc("Percent that need to be in the Army to start the attackcountdown.")]
		public readonly int Fullablity = 80;
		
		[Desc("Delay, when the Army is assambled to attack.")]
		public readonly int Countdown = 5000;

		public readonly int Counttdownreducer = 100;
		
		[Desc("Tick between adding an ArmyMember.")]
		public readonly int SearchTick = 15;
		
		public object Create(ActorInitializer init) { return new DeadArmyHandler(init, this); }
	}

	class DeadArmyHandler : ITick, IRenderAboveWorld, IRenderAboveShroud
	{
		public HashSet<Actor> Army = new HashSet<Actor>();
		public HashSet<Actor> Commanders = new HashSet<Actor>();
		public HashSet<Actor> Utilities = new HashSet<Actor>();
		
		public Player AttackPlayer;
		public Actor AttackLocation;
		public CPos GatherLocation;
		
		public int CountdownTimer;
		public int SearchTickTimer;
		
		public bool armyassamble;
		public bool armyonwords;
		public bool gatherup;
		
		private int NewArmySize;
		private int newUtlitySize;
		private int newCommandoSize;
		private int reducer;

		private string message;
		private WPos messageposition;
		
		
		private readonly DeadArmyHandlerInfo info;

		public DeadArmyHandler(ActorInitializer init, DeadArmyHandlerInfo info)
		{
			this.info = info;
			CountdownTimer = info.Countdown;
			SearchTickTimer = info.SearchTick;
			
			armyassamble = true;
			gatherup = false;
			armyonwords = false;
			
			NewArmySize = info.ArmySize;
			newUtlitySize = 0;
			newCommandoSize = 0;
			reducer = info.Counttdownreducer;

			GatherLocation = CPos.Zero;
			
			message = "Wait for Army";
			messageposition = init.Self.CenterPosition;
		}
		
		IEnumerable<IRenderable> IRenderAboveShroud.RenderAboveShroud(Actor self, WorldRenderer wr)
		{
			return RenderText(self, wr);
		}
		
		IEnumerable<IRenderable> RenderText(Actor self, WorldRenderer wr)
		{
			if (self.World.RenderPlayer != null)
			{
				if (self.Owner != self.World.RenderPlayer)
					return Enumerable.Empty<IRenderable>();
			}
			
			var font = Game.Renderer.Fonts["Infestregular"];
			var screenPos = wr.ScreenPxPosition(messageposition);
			var rend = new IRenderable[] { new TextRenderable(font, wr.ProjectedPosition(screenPos)+new WVec(0,-512,1024), -1024, Color.Bisque, message) };
			return rend;
		}
		
		//Rendering

		IEnumerable<IRenderable> RenderInner(Actor self, WorldRenderer wr)
		{
			if (self.World.RenderPlayer != null)
			{
				if (self.Owner != self.World.RenderPlayer)
					return Enumerable.Empty<IRenderable>();
			}

			var render = new HashSet<IRenderable>();

			var iz2 = 1 / wr.Viewport.Zoom;
			var hc2 = Color.LightGray;
			var ha2 = wr.Screen3DPosition(messageposition);
			var hb2 = wr.Screen3DPosition(messageposition+new WVec(0,-512,924));
			
			Game.Renderer.WorldRgbaColorRenderer.DrawLine(ha2, hb2, iz2, hc2);
			if (GatherLocation != CPos.Zero)
			{
				var ha = wr.Screen3DPosition(messageposition);
				var hb = wr.Screen3DPosition(self.World.Map.CenterOfCell(GatherLocation));
				Game.Renderer.WorldRgbaColorRenderer.DrawLine(ha, hb, iz2, Color.Crimson);
			}
			if (AttackLocation != null)
			{
				var ha = wr.Screen3DPosition(messageposition);
				var hb = wr.Screen3DPosition(AttackLocation.CenterPosition);
				Game.Renderer.WorldRgbaColorRenderer.DrawLine(ha, hb, iz2, Color.Crimson);
			}

			if (Army.Any() || Commanders.Any() || Utilities.Any())
			{
				var helperx = 0;
				var helpery = 0;
				var living = new HashSet<Actor>();
				if (Army.Any())
				{
					foreach (var actr in Army)
					{
						if (actr != null && !actr.IsDead && actr.IsInWorld)
						{
							helperx += actr.CenterPosition.X;
							helpery += actr.CenterPosition.Y;
							living.Add(actr);
						}
					}
				}

				if (Commanders.Any())
				{
					foreach (var actr in Commanders)
					{
						if (actr != null && !actr.IsDead && actr.IsInWorld)
						{
							helperx += actr.CenterPosition.X;
							helpery += actr.CenterPosition.Y;
							living.Add(actr);
						}
					}
				}
				
				if (Utilities.Any())
				{
					foreach (var actr in Utilities)
					{
						if (actr != null && !actr.IsDead && actr.IsInWorld)
						{
							helperx += actr.CenterPosition.X;
							helpery += actr.CenterPosition.Y;
							living.Add(actr);
						}
					}
				}
				
				if (living.Any())
				{
					helperx /= living.Count;
					helpery /= living.Count;
				}

				var wcr = Game.Renderer.WorldRgbaColorRenderer;
				
				foreach (var actr in living)
				{
					if (actr != null && !actr.IsDead && actr.IsInWorld)
					{
						var iz = 1 / wr.Viewport.Zoom;
						var hc = Color.LightGray;
						var ha = wr.Screen3DPosition(actr.CenterPosition);
						var hb = wr.Screen3DPosition(new WPos(helperx, helpery, 20));
						
						wcr.DrawLine(ha, hb, iz, hc);

						messageposition = new WPos(helperx, helpery, 20);
					}
				}
				
				
				return render;
			}
			
			return SpriteRenderable.None;
			
		}
		
		void IRenderAboveWorld.RenderAboveWorld(Actor self, WorldRenderer wr)
		{
			
			if (!RenderInner(self, wr).Any())
			{
				RenderInner(self, wr);
			}
		}
		
		//Army handling

		bool IsNotAlreadyInArmy(Actor self,Actor actor)
		{
			var list = self.World.ActorsHavingTrait<DeadArmyHandler>();
			foreach (var vari in list)
			{
				if (vari.Trait<DeadArmyHandler>().Army.Contains(actor))
					return true;
			}
			return false;
		}
		
		Actor FindActorArmy(Actor self)
		{

			var actors = self.World.FindActorsInCircle(self.CenterPosition,info.SearhRadius)
				.Where(a =>
				{
					if (a.Owner != self.Owner)
						return false;

					if (IsNotAlreadyInArmy(self,a))
						return false;
					
					if (a.Info.HasTraitInfo<IsDeathArmyMobInfo>() && a.Info.TraitInfo<IsDeathArmyMobInfo>().Role == "Fighter")
						return true;
					
					return false;
				});
			var enumerable = actors as Actor[] ?? actors.ToArray();
			if (enumerable.Any())
				return enumerable.ClosestTo(self);
			
			return null;
		}
		
		bool IsNotAlreadyInUtility(Actor self,Actor actor)
		{
			var list = self.World.ActorsHavingTrait<DeadArmyHandler>();
			foreach (var vari in list)
			{
				if (vari.Trait<DeadArmyHandler>().Utilities.Contains(actor))
					return true;
			}
			return false;
		}
		
		Actor FindActorUtility(Actor self)
		{

			var actors = self.World.FindActorsInCircle(self.CenterPosition,info.SearhRadius)
				.Where(a =>
				{
					if (a.Owner != self.Owner)
						return false;

					if (IsNotAlreadyInUtility(self,a))
						return false;
					
					if (a.Info.HasTraitInfo<IsDeathArmyMobInfo>() && a.Info.TraitInfo<IsDeathArmyMobInfo>().Role == "Utility")
						return true;
					
					return false;
				});
			var enumerable = actors as Actor[] ?? actors.ToArray();
			if (enumerable.Any())
				return enumerable.ClosestTo(self);
			
			return null;
		}
		
		bool IsNotAlreadyInCommander(Actor self,Actor actor)
		{
			var list = self.World.ActorsHavingTrait<DeadArmyHandler>();
			foreach (var vari in list)
			{
				if (vari.Trait<DeadArmyHandler>().Commanders.Contains(actor))
					return true;
			}
			return false;
		}
		
		Actor FindActorCommander(Actor self)
		{

			var actors = self.World.FindActorsInCircle(self.CenterPosition,info.SearhRadius)
				.Where(a =>
				{
					if (a.Owner != self.Owner)
						return false;

					if (IsNotAlreadyInCommander(self,a))
						return false;
					
					if (a.Info.HasTraitInfo<IsDeathArmyMobInfo>() && a.Info.TraitInfo<IsDeathArmyMobInfo>().Role == "Commander")
						return true;
					
					return false;
				});
			var enumerable = actors as Actor[] ?? actors.ToArray();
			if (enumerable.Any())
				return enumerable.ClosestTo(self);
			
			return null;
		}

		Player Randomplayer(Actor self)
		{

			Player targetplayer = null;
			var playerarray = new Dictionary<Player, int>();
			var allplayer = self.World.Players;

			if (allplayer.Any())
				foreach (var playerr in allplayer)
				{
					if (!playerr.NonCombatant && playerr.WinState != WinState.Lost)
					{
						playerarray.Add(playerr, playerr.PlayerActor.Trait<AttackedByDead>().AttackCount);
					}
				}

			if (playerarray.Any())
			{
				targetplayer = playerarray.MinBy(kvp => kvp.Value).Key;
				targetplayer.PlayerActor.Trait<AttackedByDead>().AttackCount += 1;
			}



			return targetplayer;
		}


		Actor Location(Actor self,Player player,CPos target)
		{
			var possis = self.World.ActorsHavingTrait<Building>()
				.Where(a =>
				{
					if (a.IsDead && !a.IsInWorld)
						return false;
					
					if (a.Owner != player)
						return false;

					return true;
				});

			List<Actor> enumerable = possis.ToList();
				
			if (!enumerable.Any())
				return null;
			
			Repeat:
			if (enumerable.Any())
			{
				
				
				var targetcell = enumerable.MinByOrDefault(a => (a.CenterPosition - self.World.Map.CenterOfCell(target)).LengthSquared);
				Actor fromhere = null;
				
				foreach (var actor in Army)
				{
					if (actor != null && actor.IsInWorld && !actor.IsDead)
					{
						fromhere = actor;
						break;
					}

				}

				if (fromhere != null)
				{
					
					var ip = fromhere.Info.TraitInfo<IPositionableInfo>();
					var validcells = self.World.Map.FindTilesInCircle(target, 2, true).Where(c => ip.CanEnterCell(self.World, null, c));
					var cPoses = validcells as CPos[] ?? validcells.ToArray();

					if (cPoses.Any())
					{
						var pickClosestCell =
							cPoses.MinByOrDefault(c => (self.World.Map.CenterOfCell(c) - fromhere.CenterPosition).LengthSquared);

						List<CPos> path;

						using (var thePath = PathSearch.FromPoint(self.World, fromhere.Info.TraitInfo<MobileInfo>(),
							self, fromhere.Location, pickClosestCell, true))
							path = self.World.WorldActor.Trait<IPathFinder>().FindPath(thePath);

						if (path.Count > 0)
							return targetcell;

						enumerable.Remove(targetcell);
					}
					
					goto Repeat;
				}
			}
			return null;
		}

		CPos MeetupPoint(Actor self, HashSet<Actor> hash,CPos target)
		{;

			if (!hash.Any())
				return CPos.Zero;

			
			
			var fromhere = hash.ElementAt(self.World.SharedRandom.Next(hash.Count));
			HashSet<CPos> alllocations = new HashSet<CPos>();
			
			foreach (var acto in hash)
			{
				if (acto != null && !acto.IsDead && acto.IsInWorld)
					alllocations.Add(acto.Location);
			}
			if (!alllocations.Any())
				return CPos.Zero;

			if (fromhere != null && !fromhere.IsDead && fromhere.IsInWorld && fromhere.Info.HasTraitInfo<MobileInfo>())
			{
				var ip = fromhere.Info.TraitInfo<IPositionableInfo>();
				var validcells = self.World.Map.FindTilesInCircle(target, 3, true).Where(c => ip.CanEnterCell(self.World, null, c));
				var cPoses = validcells as CPos[] ?? validcells.ToArray();
				if (!cPoses.Any())
					return CPos.Zero;

				var pickClosestCell = cPoses.MinByOrDefault(c => (self.World.Map.CenterOfCell(c) - fromhere.CenterPosition).LengthSquared);
				

				List<CPos> path;
				
				using (var thePath = PathSearch.FromPoint(self.World, fromhere.Info.TraitInfo<MobileInfo>(),
					self, fromhere.Location, pickClosestCell, true))
					path = self.World.WorldActor.Trait<IPathFinder>().FindPath(thePath);


				if (path.Any())
				{
					foreach (var loco in path)
					{
						var owner = self.Owner;
						var position = owner.World.Map.CenterOfCell(loco);
						var beacon = self.Owner.PlayerActor.Info.TraitInfo<PlaceBeaconInfo>();
						var playerBeacon = new Beacon(self.Owner, position, 20 * 25, beacon.Palette,
							beacon.IsPlayerPalette, beacon.BeaconImage, beacon.ArrowSequence, beacon.CircleSequence);
						self.Owner.PlayerActor.World.AddFrameEndTask(w => w.Add(playerBeacon));
					}
					if (path.Count > 11)
					{
						return path.ElementAt(10);
					}

					return path.Last();
				}
			}
			return CPos.Zero;
		}
		
		void ITick.Tick(Actor self)
		{
			
			if (!Army.Any() && !Commanders.Any() && !Utilities.Any() && CountdownTimer <= 0)
			{
				EndWarOnEmpty(self);
				return;
			}
			
			if (armyassamble)
			{
				if (SearchTickTimer > 0)
					SearchTickTimer--;
				
				if (SearchTickTimer <= 0)
				{
					BuildTheArmy(self);
					SearchTickTimer = info.SearchTick;
				}
			}
			
			if ((Army.Any() || Commanders.Any()|| Utilities.Any()) && !armyonwords && !gatherup && CountdownTimer > 0)
			{
				CountdownTimer--;
				message = "Attack in " + CountdownTimer/25 + " seconds";
				return;
			}
			
			if ((Army.Any() || Commanders.Any()|| Utilities.Any()) && !armyonwords && !gatherup && CountdownTimer <= 0)
			{
				AttackPlayer = FetchforPlayer(self);
				
				if (AttackPlayer == null)
					AttackPlayer = Randomplayer(self);
				else if( AttackPlayer.WinState == WinState.Lost)
					AttackPlayer = Randomplayer(self);
				
				message = "Attacking Player " + AttackPlayer.PlayerName;

				if (AttackPlayer != null)
				{
					armyassamble = false;
					gatherup = true;
					
					if(reducer > 1000)
						reducer += info.Counttdownreducer;
					
					if (GatherLocation == CPos.Zero)
					{
						var loc = Location(self, AttackPlayer, self.Location).Location;
						GatherLocation = MeetupPoint(self, Army, loc);
						if (GatherLocation == CPos.Zero)
							return;
					}
					
					if (Army.Any())
						ForceArmyGather(Army, GatherLocation);

					if (Commanders.Any())
						ForceArmyGather(Commanders, GatherLocation);

					if (Utilities.Any())
						ForceArmyGather(Utilities, GatherLocation);
					
				}
			}

			if (gatherup)
			{
				if (SearchTickTimer > 0)
					SearchTickTimer--;

				if (SearchTickTimer <= 0)
				{
					message = "Gather up at  " + GatherLocation.ToString();
					
					SearchTickTimer = 100;
					
					Army = CheckForLiving(Army);
					Commanders = CheckForLiving(Commanders);
					Utilities = CheckForLiving(Utilities);

					if (!Army.Any() && !Commanders.Any() && !Utilities.Any())
					{
						EndWarOnEmpty(self);
						return;
					}

					if (AttackPlayer == null)
						AttackPlayer = Randomplayer(self);
					else if( AttackPlayer.WinState == WinState.Lost)
						AttackPlayer = Randomplayer(self);

					if (GatherLocation == CPos.Zero)
					{
						var loc = Location(self, AttackPlayer, self.Location).Location;
						GatherLocation = MeetupPoint(self, Army, loc);
						if (GatherLocation == CPos.Zero)
							return;
					}
					
	
					bool true1 = IdleArmyGather(Army, self,GatherLocation,5);
					bool true2 = IdleArmyGather(Commanders, self,GatherLocation,5);
					bool true3 = IdleArmyGather(Utilities, self,GatherLocation,5);

					//Log.Write("debug","Check Army ready: " + true1);
					
								if (true1 && true2 && true3)
					{
						if(AttackPlayer != null && AttackPlayer.WinState != WinState.Lost)
							AttackLocation = Location(self, AttackPlayer, GatherLocation);
						else if (AttackPlayer == null)
							AttackPlayer = Randomplayer(self);
						else if( AttackPlayer.WinState == WinState.Lost)
							AttackPlayer = Randomplayer(self);
					
						GatherLocation = CPos.Zero;
						
						armyonwords = true;
						gatherup = false;
					}
				}

			}
			
			if (armyonwords)
			{
				if (SearchTickTimer > 0)
					SearchTickTimer--;

				if (SearchTickTimer <= 0)
				{
					if (AttackLocation != null && AttackLocation.Info.HasTraitInfo<TooltipInfo>())
					message = "Attack  " + AttackLocation.Info.TraitInfo<TooltipInfo>().Name;
					SearchTickTimer = 50;

					Army = CheckForLiving(Army);
					Commanders = CheckForLiving(Commanders);
					Utilities = CheckForLiving(Utilities);

					if (!Army.Any() && !Commanders.Any() && !Utilities.Any())
					{
						EndWarOnEmpty(self);
						return;
					}

					if (AttackPlayer.WinState == WinState.Lost)
					{
						armyonwords = false;
						gatherup = false;
						armyassamble = true;

						CountdownTimer = info.Countdown;
						CountdownTimer -= reducer;
						
						SearchTickTimer = info.SearchTick;

						GatherLocation = CPos.Zero;
					}

					if (AttackLocation == null)
						AttackLocation = Location(self, AttackPlayer, GatherLocation);
					else if (AttackLocation.IsDead || !AttackLocation.IsInWorld)
						AttackLocation = Location(self, AttackPlayer, GatherLocation);

					if (Army.Any())
						IdleArmyAttack(Army);

					if (Commanders.Any())
						IdleArmyAttack(Commanders);

					if (Utilities.Any())
						IdleArmyAttack(Utilities);

					

				}

			}

		}

		void EndWarOnEmpty(Actor self)
		{
			message = "Abort Attack!";
			armyonwords = false;
			gatherup = false;
			armyassamble = true;
			
			CountdownTimer = info.Countdown-reducer;
			SearchTickTimer = info.SearchTick;

			GatherLocation = CPos.Zero;
			AttackLocation = null;
			
			
			Army = new HashSet<Actor>();
			Commanders = new HashSet<Actor>();
			Utilities = new HashSet<Actor>();
			
			NewArmySize += info.ArmySize;
			newUtlitySize += 1;
			newCommandoSize += 1;

			messageposition = self.CenterPosition;
		}

		HashSet<Actor> CheckForLiving(HashSet<Actor> army)
		{
			var newhash = new HashSet<Actor>();
			if (army.Any())
				foreach (var vari in army)
				{
					if (vari != null && !vari.IsDead && vari.IsInWorld)
					{
						newhash.Add(vari);
					}
				}
			return newhash;
		}

		void IdleArmyAttack(HashSet<Actor> army)
		{
			foreach (var vari in army)
			{
				if (vari != null && !vari.IsDead && vari.IsInWorld && vari.IsIdle)
				{
					var moveto = vari.TraitOrDefault<IMove>();
					if (AttackLocation != null && AttackLocation.IsInWorld && !AttackLocation.IsDead)
					{
						vari.CancelActivity();
						vari.QueueActivity(new AttackMoveActivity(vari, moveto.MoveTo(AttackLocation.Location, 5)));
					}
				}
			}
		}
		
		void ForceArmyGather(HashSet<Actor> army,CPos location)
		{
			foreach (var vari in army)
			{
				if (vari != null && !vari.IsDead && vari.IsInWorld)
				{
					var moveto = vari.TraitOrDefault<IMove>();
					if (location != CPos.Zero)
					{
						vari.CancelActivity();
						vari.QueueActivity(new AttackMoveActivity(vari, moveto.MoveTo(location, 3)));
					}
				}
			}
		}
		
		bool IdleArmyGather(HashSet<Actor> army,Actor self,CPos location,int locationRad)
		{
			var check = true;
			
			if (!army.Any())
				return true;
			if(location == CPos.Zero)
				return false;
			
			foreach (var vari in army)
			{
				if (vari != null && !vari.IsDead && vari.IsInWorld && vari.IsIdle &&
				   (vari.CenterPosition - self.World.Map.CenterOfCell(location)).LengthSquared >
				    WDist.FromCells(locationRad).LengthSquared && vari.Info.HasTraitInfo<IMoveInfo>())
				{
					
					var moveto = vari.TraitOrDefault<IMove>();

					var randloc = new CVec(self.World.SharedRandom.Next(locationRad/2, locationRad/2), self.World.SharedRandom.Next(locationRad/2, locationRad/2));
					vari.CancelActivity();
					vari.QueueActivity(new AttackMoveActivity(vari, moveto.MoveTo(location + randloc, 4)));
					
					check = false;

				}
				else if (vari != null && (vari.CenterPosition - self.World.Map.CenterOfCell(location)).LengthSquared >
				         WDist.FromCells(locationRad).LengthSquared)
					check = false;
			}
			return check;
		}

		Player FetchforPlayer(Actor self)
		{
			foreach (var actor in Army)
			{
				var agressor = actor.Trait<IsDeathArmyMob>().GetAttackedorAttacking;
				if (agressor != null && !agressor.NonCombatant && agressor.WinState != WinState.Lost)
					return agressor;
			}

			return null;
		}
		
		void BuildTheArmy(Actor self)
		{
			Army = CheckForLiving(Army);
			Commanders = CheckForLiving(Commanders);
			Utilities = CheckForLiving(Utilities);
			
			if (Army.Count < NewArmySize)
			{
				var find = FindActorArmy(self);
				if (find != null && !find.IsDead && find.IsInWorld)
				{
					Army.Add(find);
					var moveto = find.TraitOrDefault<IMove>();
					var randloc = self.Location + new CVec(self.World.SharedRandom.Next(-5, 5), self.World.SharedRandom.Next(-5, 5));
					find.QueueActivity(new AttackMoveActivity(self, moveto.MoveTo(randloc, 5)));
				}
			}
			if (Commanders.Count < Decimal.Floor(newCommandoSize/info.CommandoWaveAdder))
			{
				var find = FindActorCommander(self);
				if (find != null && !find.IsDead && find.IsInWorld)
				{
					Commanders.Add(find);
					var moveto = find.TraitOrDefault<IMove>();
					var randloc = self.Location + new CVec(self.World.SharedRandom.Next(-5, 5), self.World.SharedRandom.Next(-5, 5));
					find.QueueActivity(new AttackMoveActivity(self, moveto.MoveTo(randloc, 5)));
				}
			}
			
			if (Utilities.Count < Decimal.Floor(newUtlitySize/info.UtlityWaveAdder))
			{
				var find = FindActorUtility(self);
				if (find != null && !find.IsDead && find.IsInWorld)
				{
					Utilities.Add(find);
					var moveto = find.TraitOrDefault<IMove>();
					var randloc = self.Location + new CVec(self.World.SharedRandom.Next(-5, 5), self.World.SharedRandom.Next(-5, 5));
					find.QueueActivity(new AttackMoveActivity(self, moveto.MoveTo(randloc, 5)));
				}
			}
		}
	}
}