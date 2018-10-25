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
    }
}
