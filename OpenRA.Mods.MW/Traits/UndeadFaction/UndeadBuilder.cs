using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.MW.Effects;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits
{
    [Desc("Is Unit a Peasant (adds a count of 1 to the PlayerCivilisation).")]
    public class UndeadBuilderInfo : ITraitInfo
    {
        public readonly string ReadyAudio = "ConstructionComplete";

        public readonly string SpawnActor = null;

        public readonly CVec SpawnOffset = new CVec(0, 0);

        public readonly int SummoningTime = 100;

        public readonly bool SummoningDecay = false;

        public readonly int DecayTime = 25;

        public readonly bool SkipMakeAnims = false;

        public readonly bool ForceHealthPercentage = false;

        public readonly int Cost = 1000;

        public readonly bool Selfbuilds = false;

        public readonly int SelfBuildSteps = 1;

        public readonly int SelfBuildDelay = 25;

        public object Create(ActorInitializer init) { return new UndeadBuilder(this); }
    }

    public class UndeadBuilder : ITick
    {
        public UndeadBuilderInfo Info;
        public int Hassummoningcount;
        private int decaycounter;

        public int PayPerTick;
        private DeveloperMode devMode;

        private int selfBuildCounter;

        public UndeadBuilder(UndeadBuilderInfo info)
        {
            this.Info = info;
            decaycounter = info.DecayTime;
            PayPerTick = info.Cost / info.SummoningTime;
            selfBuildCounter = info.SelfBuildDelay;
        }

        void ITick.Tick(Actor self)
        {
            if (Info.SummoningDecay)
            {
                decaycounter--;
                if (decaycounter <= 0 & Hassummoningcount > 0)
                {
                    Hassummoningcount--;
                    decaycounter = Info.DecayTime;
                }
            }

            if (Hassummoningcount >= Info.SummoningTime)
                ReplaceSelf(self);

            devMode = self.Owner.PlayerActor.TraitOrDefault<DeveloperMode>();
            if (devMode != null && devMode.FastBuild)
            {
                ReplaceSelf(self);
            }

            if (!Info.Selfbuilds)
                return;

            if (Info.Selfbuilds)
            {
                if (selfBuildCounter-- <= 0)
                {
                    selfBuildCounter = Info.SelfBuildDelay;
                    if (self.Owner.PlayerActor.Trait<PlayerResources>().TakeCash(PayPerTick, true))
                    {
                        Hassummoningcount += 1;
                        var floattest = PayPerTick.ToString();
                        floattest = "- " + floattest + " Essence";
                        if (self.Owner.IsAlliedWith(self.World.RenderPlayer))
                            self.World.AddFrameEndTask(w => w.Add(new FloatingTextBackwards(self.CenterPosition,
                                self.Owner.Color.RGB, floattest, 30)));
                    }
                }
            }
        }

        public void ReplaceSelf(Actor self)
        {
            self.World.AddFrameEndTask(w =>
            {
                if (self.IsDead)
                    return;

                var selected = w.Selection.Contains(self);
                var controlgroup = w.Selection.GetControlGroupForActor(self);

                var init = new TypeDictionary
                {
                    new LocationInit(self.Location + Info.SpawnOffset),
                    new CenterPositionInit(self.CenterPosition),
                    new OwnerInit(self.Owner),
                };

                if (Info.SkipMakeAnims)
                    init.Add(new SkipMakeAnimsInit());

                var health = self.TraitOrDefault<Health>();
                if (health != null && Info.ForceHealthPercentage)
                {
                    var newHP = (health.HP * 100) / health.MaxHP;
                    init.Add(new HealthInit(newHP));
                }

                var a = w.CreateActor(Info.SpawnActor, init);

                if (selected)
                    w.Selection.Add(a);
                if (controlgroup.HasValue)
                    w.Selection.AddToControlGroup(a, controlgroup.Value);

                Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", Info.ReadyAudio,
                    self.Owner.Faction.InternalName);

                self.Dispose();
            });
        }
    }
}
