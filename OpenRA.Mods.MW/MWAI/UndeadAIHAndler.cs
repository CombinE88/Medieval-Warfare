using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.AI;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.MW.Activities;
using OpenRA.Mods.MW.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.MWAI
{
    public class UndeadAIHAndler
    {
        private readonly World world;
        private Player Player { get; set; }
        private HackyMWAIInfo hackyAIInfo;
        private HackyMWAI hackyAI;

        // Undead variables
        public HashSet<Actor> AcolyteBuilder = new HashSet<Actor>();
        public HashSet<Actor> AcolyteHarvester = new HashSet<Actor>();

        public UndeadAIHAndler(World w, HackyMWAIInfo hackinfo, HackyMWAI hai, Player player)
        {
            world = w;
            hackyAIInfo = hackinfo;
            hackyAI = hai;
            this.Player = player;
        }

        public int CountPeasants()
        {
            return Player.PlayerActor.Trait<PlayerCivilization>().Peasantpopulationvar;
        }

        public int CountPossiblePopulation()
        {
            return Player.PlayerActor.Trait<PlayerCivilization>().FreePopulation + Player.PlayerActor.Trait<PlayerCivilization>().Peasantpopulationvar;
        }

        public int CountPotentialFreeBeds()
        {
            var housactors = world.ActorsHavingTrait<Building>()
                .Where(a => a.Owner == Player && hackyAIInfo.BuildingCommonNames.Houses.Contains(a.Info.Name));

            var potentialpop = 0;

            if (housactors.Any() && housactors != null)
            {
                foreach (var hous in housactors)
                {
                    var preserv = Player.World.Map.Rules.Actors[hous.Info.Name.Replace(".scaff", string.Empty).ToLower()];

                    if (preserv != null && preserv.HasTraitInfo<ProvidesLivingspaceInfo>())
                        potentialpop += preserv.TraitInfo<ProvidesLivingspaceInfo>().Ammount;

                    if (hous != null && hous.Info.HasTraitInfo<ProvidesLivingspaceInfo>())
                        potentialpop += hous.Info.TraitInfo<ProvidesLivingspaceInfo>().Ammount;
                }
            }

            return potentialpop;
        }

        public Actor FindNewDeerstand()
        {
            var deerStands = world.ActorsHavingTrait<ISeedableResource>().Where(a => hackyAIInfo.UndeadCommonNames.PrayableDeerStands.Contains(a.Info.Name)).ToList();

            if (!deerStands.Any())
                return null;

            var closestDeerStands = deerStands.OrderBy(a => (a.CenterPosition - Player.World.Map.CenterOfCell(hackyAI.InitialBaseCenter)).LengthSquared);

            foreach (var stand in closestDeerStands)
            {
                var prayer = world.FindActorsInCircle(stand.CenterPosition, WDist.FromCells(4)).Where(a =>
                {
                    return a.Owner == Player && hackyAIInfo.UndeadCommonNames.PrayableDeerStands.Contains(a.Info.Name);
                }).Count();

                if (prayer >= 3)
                    continue;

                return stand;
            }

            return null;
        }

        public void ManageBuildrAcolytes()
        {
            var pentagrams = world.ActorsHavingTrait<Building>().Where(a => a.Owner == Player && a.Info.Name.Contains(".penta"));

            if (AcolyteBuilder.Any() && pentagrams.Any())
            {
                var builderDoNothing = AcolyteBuilder.Where(a => a.IsIdle);

                if (!builderDoNothing.Any())
                    return;

                var preytarget = pentagrams.First();
                var idlePreyer = builderDoNothing.First();

                // TODO: Acolytes start building
                if (preytarget != null)
                    hackyAI.QueueOrder(new Order("PreyBuild", idlePreyer, Target.FromActor(preytarget), false));
            }
        }

        public void ManageFarmerAcolytes()
        {
            if (AcolyteHarvester.Any())
            {
                var farmerDoNothing = AcolyteHarvester.Where(a => a.IsIdle);

                if (!farmerDoNothing.Any())
                    return;

                var idleFarmer = farmerDoNothing.First();

                var zigguratLv2 = world.ActorsHavingTrait<Building>().Where(a =>
                {
                    if (a.Owner != Player)
                        return false;

                    var names = hackyAIInfo.UndeadCommonNames;
                    return names.ZigguratLv3.Contains(a.Info.Name) || names.ZigguratLv2.Contains(a.Info.Name);
                });
                var farmFields = world.ActorsHavingTrait<ISeedableResource>().Where(a => hackyAIInfo.UndeadCommonNames.PrayableCrops.Contains(a.Info.Name)).ToHashSet();
                var ironFields = world.ActorsHavingTrait<ISeedableResource>().Where(a => hackyAIInfo.UndeadCommonNames.PrayableIron.Contains(a.Info.Name));
                var deerStands = world.ActorsHavingTrait<ISeedableResource>().Where(a => hackyAIInfo.UndeadCommonNames.PrayableDeerStands.Contains(a.Info.Name));

                if (zigguratLv2 != null && zigguratLv2.Any())
                    foreach (var aco in ironFields)
                    {
                        farmFields.Add(aco);
                    }

                foreach (var stand in deerStands)
                {
                    foreach (var seed in stand.TraitsImplementing<ISeedableResource>())
                    {
                        if (seed.IsTraitEnabled())
                        {
                            farmFields.Add(stand);
                        }
                    }
                }

                // Sort the fields
                if (farmFields != null && farmFields.Any())
                {
                    var fields = farmFields.ToList(); // get list of closest possible Fields
                    var closestfields = fields.OrderBy(a =>
                    {
                        var center = Player.World.Map.CenterOfCell(hackyAI.InitialBaseCenter);
                        if (hackyAIInfo.UndeadCommonNames.PrayableIron.Contains(a.Info.Name))
                            return (a.CenterPosition - center).LengthSquared;

                        return (a.CenterPosition - center).LengthSquared;
                    });

                    Actor preytarget = null;

                    foreach (var scarecrow in closestfields)
                    {
                        var surroundingEnemies = Player.World.FindActorsInCircle(scarecrow.CenterPosition, hackyAIInfo.AcolytePrayProximity)
                            .Count(e => e.Info.HasTraitInfo<BuildingInfo>() && Player.Stances[e.Owner] == Stance.Enemy);

                        if (surroundingEnemies > 0)
                            continue;

                        var occupyCount = 0;

                        // Do we have enough resources around to prey?
                        var reslay = Player.World.WorldActor.Trait<ResourceLayer>();
                        var getResourceArroundit = Player.World.Map.FindTilesInCircle(scarecrow.Location, 4, true).Where(c => reslay.GetResourceDensity(c) > 0);
                        var numberOfFullCells = getResourceArroundit.Count();

                        if (numberOfFullCells < hackyAIInfo.MinimumResourceFields) // We want atleast 5 cells filled with resources
                            continue;

                        // Get the occupy count
                        foreach (var dock in scarecrow.TraitsImplementing<Dock>())
                        {
                            if (dock.IsBlocked || dock.Reserver != null || !dock.CanAccessDock(idleFarmer))
                                occupyCount++;
                        }

                        if (occupyCount < 5)
                        {
                            preytarget = scarecrow;

                            break;
                        }
                    }

                    if (preytarget != null)
                        hackyAI.QueueOrder(new Order("Prey", idleFarmer, Target.FromActor(preytarget), false));
                }
            }
        }

        public void ManageDeadAcolytes()
        {
            var deseasedActors = new HashSet<Actor>();

            foreach (var aco in AcolyteBuilder)
            {
                if (aco.IsDead || !aco.IsInWorld)
                    deseasedActors.Add(aco);
            }

            foreach (var aco in AcolyteHarvester)
            {
                if (aco.IsDead || !aco.IsInWorld)
                    deseasedActors.Add(aco);
            }

            foreach (var aco in deseasedActors)
            {
                if (AcolyteHarvester.Contains(aco))
                    AcolyteHarvester.Remove(aco);
                if (AcolyteBuilder.Contains(aco))
                    AcolyteBuilder.Remove(aco);
            }
        }

        public void ManageEmptyAcolytes()
        {
            var listedAcolytes = 0;
            listedAcolytes = AcolyteBuilder.Count() + AcolyteHarvester.Count();
            var findAcolytes = Player.World.ActorsHavingTrait<AcolytePrey>().Where(a =>
            {
                return a.Owner == Player && !AcolyteBuilder.Contains(a) && !AcolyteHarvester.Contains(a);
            }).ToHashSet();

            if (listedAcolytes == 0 && findAcolytes.Any())
            {
                var potentialWorkerNumber = (int)(decimal)(findAcolytes.Count() * hackyAIInfo.AcolyteWorkerRatio) / 100;

                for (int i = 1; i <= potentialWorkerNumber; i++)
                {
                    var lyte = findAcolytes.First();
                    if (lyte != null && !lyte.IsDead && lyte.IsInWorld)
                    {
                        AcolyteBuilder.Add(lyte);
                        findAcolytes.Remove(lyte);
                    }
                }

                if (findAcolytes.Any())
                {
                    foreach (var aco in findAcolytes)
                    {
                        if (aco != null && !aco.IsDead && aco.IsInWorld)
                            AcolyteHarvester.Add(aco);
                    }
                }
            }
            else if (listedAcolytes > 0 && findAcolytes.Any())
            {
                foreach (var aco in findAcolytes)
                {
                    if (aco != null && !aco.IsDead && aco.IsInWorld)
                    {
                        var calculatedratio = (int)(decimal)(100 / (AcolyteBuilder.Count() + AcolyteHarvester.Count())) * AcolyteBuilder.Count();
                        if (calculatedratio >= hackyAIInfo.AcolyteWorkerRatio)
                        {
                            AcolyteHarvester.Add(aco);
                        }
                        else
                        {
                            AcolyteBuilder.Add(aco);
                        }
                    }
                }
            }
        }

        public void CheckAllPatchesForProfit()
        {
            // Find Empty Patches and retreat Acolytes from them:
            var farmFields = world.ActorsHavingTrait<ISeedableResource>().Where(a =>
                hackyAIInfo.UndeadCommonNames.PrayableCrops.Contains(a.Info.Name) ||
                hackyAIInfo.UndeadCommonNames.PrayableIron.Contains(a.Info.Name) ||
                hackyAIInfo.UndeadCommonNames.PrayableDeerStands.Contains(a.Info.Name)).ToHashSet();
            var reslay = Player.World.WorldActor.Trait<ResourceLayer>();

            foreach (var crow in farmFields)
            {
                var doihavepreyers = crow.TraitsImplementing<Dock>().Where(d => d.Reserver != null && d.Reserver.Owner == Player);

                if (doihavepreyers.Count() <= hackyAIInfo.MAxAcolytesOnEmptyPatch)
                    continue;

                var getResourceArroundit = Player.World.Map.FindTilesInCircle(crow.Location, 4, true).Where(c => reslay.GetResourceDensity(c) > 0).Count();

                // We want atleast MinimumResourceFields cells filled with resources
                if (getResourceArroundit >= hackyAIInfo.MinimumResourceFields)
                {
                    continue;
                }

                var allowedNumber = 0;

                foreach (var dock in crow.TraitsImplementing<Dock>().Where(d => d.Reserver != null))
                {
                    if (allowedNumber >= hackyAIInfo.MAxAcolytesOnEmptyPatch && dock.Reserver.Owner == Player)
                    {
                        hackyAI.QueueOrder(new Order("Stop", dock.Reserver, Target.Invalid, false));
                    }

                    allowedNumber++;
                }
            }
        }
    }
}
