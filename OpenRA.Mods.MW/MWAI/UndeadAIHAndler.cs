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
        public readonly World World;
        public Player Player { get; private set; }
        public HackyMWAIInfo hackyAIInfo;
        public HackyMWAI hackyAI;

        // Undead variables

        public HashSet<Actor> AcolyteBuilder = new HashSet<Actor>();
        public HashSet<Actor> AcolyteHarvester = new HashSet<Actor>();

        public UndeadAIHAndler(World w, HackyMWAIInfo hackinfo, HackyMWAI hai, Player player)
        {
            World = w;
            hackyAIInfo = hackinfo;
            hackyAI = hai;
            Player = player;
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
            var housactors = World.ActorsHavingTrait<Building>()
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
            var DeerStands = World.ActorsHavingTrait<ISeedableResource>().Where(a => hackyAIInfo.UndeadCommonNames.PrayableDeerStands.Contains(a.Info.Name)).ToList();

            if (!DeerStands.Any())
                return null;

            var ClosestDeerStands = DeerStands.OrderBy(a => (a.CenterPosition - Player.World.Map.CenterOfCell(hackyAI.initialBaseCenter)).LengthSquared);

            foreach (var stand in ClosestDeerStands)
            {
                var prayer = World.FindActorsInCircle(stand.CenterPosition, WDist.FromCells(4)).Where(a => a.Owner == Player && hackyAIInfo.UndeadCommonNames.PrayableDeerStands.Contains(a.Info.Name)).Count();

                if (prayer >= 3)
                    continue;

                return stand;
            }
            return null;
        }

        public void ManageBuildrAcolytes()
        {
            var pentagrams = World.ActorsHavingTrait<Building>().Where(a => a.Owner == Player && a.Info.Name.Contains(".penta"));

            if (AcolyteBuilder.Any() && pentagrams.Any())
            {
                var BuilderDoNothing = AcolyteBuilder.Where(a => a.IsIdle); //getting an Idle Farmer Acolyte

                if (!BuilderDoNothing.Any())
                    return;

                var preytarget = pentagrams.First();
                var IdlePreyer = BuilderDoNothing.First();

                //TODO: Acolytes start building
                if (preytarget != null)
                    hackyAI.QueueOrder(new Order("PreyBuild", IdlePreyer, Target.FromActor(preytarget), false));
            }
        }

        public void ManageFarmerAcolytes()
        {
            if (AcolyteHarvester.Any())
            {
                var FarmerDoNothing = AcolyteHarvester.Where(a => a.IsIdle); //getting an Idle Farmer Acolyte

                if (!FarmerDoNothing.Any())
                    return;

                var IdleFarmer = FarmerDoNothing.First();

                var ZigguratLv2 = World.ActorsHavingTrait<Building>().Where(a => a.Owner == Player && (hackyAIInfo.UndeadCommonNames.ZigguratLv3.Contains(a.Info.Name) || hackyAIInfo.UndeadCommonNames.ZigguratLv2.Contains(a.Info.Name)));
                var FarmFields = World.ActorsHavingTrait<ISeedableResource>().Where(a => (hackyAIInfo.UndeadCommonNames.PrayableCrops.Contains(a.Info.Name))).ToHashSet();
                var IronFields = World.ActorsHavingTrait<ISeedableResource>().Where(a => (hackyAIInfo.UndeadCommonNames.PrayableIron.Contains(a.Info.Name)));
                var DeerStands = World.ActorsHavingTrait<ISeedableResource>().Where(a => (hackyAIInfo.UndeadCommonNames.PrayableDeerStands.Contains(a.Info.Name)));



                if (ZigguratLv2 != null && ZigguratLv2.Any())
                    foreach (var aco in IronFields)
                    {
                        FarmFields.Add(aco);
                    }

                foreach (var stand in DeerStands)
                {
                    foreach (var seed in stand.TraitsImplementing<ISeedableResource>())
                    {
                        if (seed.IsTraitEnabled())
                        {
                            FarmFields.Add(stand);
                        }
                    }
                }

                // Sort the fields
                if (FarmFields != null && FarmFields.Any())
                {
                    var fields = FarmFields.ToList(); // get list of closest possible Fields
                    var closestfields = fields.OrderBy(a => (hackyAIInfo.UndeadCommonNames.PrayableIron.Contains(a.Info.Name) ? ((a.CenterPosition - Player.World.Map.CenterOfCell(hackyAI.initialBaseCenter)).LengthSquared) / 2 : (a.CenterPosition - Player.World.Map.CenterOfCell(hackyAI.initialBaseCenter)).LengthSquared));

                    Actor preytarget = null;

                    foreach (var scarecrow in closestfields)
                    {

                        var surroundingEnemies = Player.World.FindActorsInCircle(scarecrow.CenterPosition, hackyAIInfo.AcolytePrayProximity)
                            .Count(e => e.Info.HasTraitInfo<BuildingInfo>() && Player.Stances[e.Owner] == Stance.Enemy);

                        if (surroundingEnemies > 0)
                            continue;

                        var OccupyCount = 0;


                        // Do we have enough resources around to prey?

                        var reslay = Player.World.WorldActor.Trait<ResourceLayer>();
                        var getResourceArroundit = Player.World.Map.FindTilesInCircle(scarecrow.Location, 4, true).Where(c => reslay.GetResourceDensity(c) > 0);
                        var NumberOfFullCells = getResourceArroundit.Count();

                        if (NumberOfFullCells < hackyAIInfo.MinimumResourceFields) // We want atleast 5 cells filled with resources
                            continue;

                        // Get the occupy count
                        foreach (var dock in scarecrow.TraitsImplementing<Dock>())
                        {
                            if (dock.IsBlocked || dock.Reserver != null || !dock.CanAccessDock(IdleFarmer))
                                OccupyCount++;
                        }


                        if (OccupyCount < 5)
                        {
                            preytarget = scarecrow;

                            break;
                        }

                    }
                    if (preytarget != null)
                        hackyAI.QueueOrder(new Order("Prey", IdleFarmer, Target.FromActor(preytarget), false));
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

            var ListedAcolytes = 0;

            ListedAcolytes = AcolyteBuilder.Count() + AcolyteHarvester.Count();

            var findAcolytes = Player.World.ActorsHavingTrait<AcolytePrey>().Where(a => a.Owner == Player && !AcolyteBuilder.Contains(a) && !AcolyteHarvester.Contains(a)).ToHashSet();

            if (ListedAcolytes == 0 && findAcolytes.Any())
            {
                //AcolyteWorkerRatio
                var PotentialWorkerNumber = (int)(decimal)(findAcolytes.Count() * hackyAIInfo.AcolyteWorkerRatio) / 100;

                for (int i = 1; i <= PotentialWorkerNumber; i++)
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
            else if (ListedAcolytes > 0 && findAcolytes.Any())
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

            var FarmFields = World.ActorsHavingTrait<ISeedableResource>().Where(a => (hackyAIInfo.UndeadCommonNames.PrayableCrops.Contains(a.Info.Name) || hackyAIInfo.UndeadCommonNames.PrayableIron.Contains(a.Info.Name) || hackyAIInfo.UndeadCommonNames.PrayableDeerStands.Contains(a.Info.Name))).ToHashSet();
            var reslay = Player.World.WorldActor.Trait<ResourceLayer>();

            foreach (var crow in FarmFields)
            {

                var doihavepreyers = crow.TraitsImplementing<Dock>().Where(d => d.Reserver != null && d.Reserver.Owner == Player);

                if (doihavepreyers.Count() <= hackyAIInfo.MAxAcolytesOnEmptyPatch)
                {

                    continue;
                }
                //BotDebug("AI: Non adequate ammount of acolytes at " + crow.Location + " found, with " + doihavepreyers.Count() + " acolytes");


                var getResourceArroundit = Player.World.Map.FindTilesInCircle(crow.Location, 4, true).Where(c => reslay.GetResourceDensity(c) > 0).Count();

                if (getResourceArroundit >= hackyAIInfo.MinimumResourceFields) // We want atleast MinimumResourceFields cells filled with resources
                {
                    //BotDebug("AI: Enough Cells Found at " + crow.Location + " with " + getResourceArroundit + " cells full resources");
                    continue;
                }

                var AllowedNumber = 0;

                foreach (var dock in crow.TraitsImplementing<Dock>().Where(d => d.Reserver != null))
                {
                    if (AllowedNumber >= hackyAIInfo.MAxAcolytesOnEmptyPatch && dock.Reserver.Owner == Player)
                    {
                        //BotDebug("AI: acolyte " + dock.Reserver + " released from duty");
                        //dock.Reserver.CancelActivity();
                        hackyAI.QueueOrder(new Order("Stop", dock.Reserver, Target.Invalid, false));
                    }

                    AllowedNumber++;
                }
            }
        }

    }
}
