using System;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.MW.Effects;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits.UndeadFaction
{
    [Desc("Is Unit a Peasant (adds a count of 1 to the PlayerCivilisation).")]
    public class UndeadBuilderInfo : ITraitInfo, IRulesetLoaded, Requires<WithSpriteBodyInfo>
    {
        public readonly string ReadyAudio = "ConstructionComplete";

        public readonly string SpawnFlashAnimation = "spawnlightning";

        public readonly string SpawnPaleete = "ra";

        [Desc("Name of the Animations shown while increasing the build. add '-make' to have the merge animation")]
        public readonly string[] Animations = { };

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

        [Desc("Which sprite body to modify.")] public readonly string Body = "body";

        public object Create(ActorInitializer init)
        {
            return new UndeadBuilder(init, this);
        }

        public void RulesetLoaded(Ruleset rules, ActorInfo info)
        {
            var matches = info.TraitInfos<WithSpriteBodyInfo>().Count(w => w.Name == Body);
            if (matches != 1)
                throw new YamlException("WithAttackAnimation needs exactly one sprite body with matching name.");
        }
    }

    public class UndeadBuilder : ITick
    {
        public UndeadBuilderInfo Info;
        public int HasSummoningCount { get; set; }
        public int PayPerTick { get; private set; }
        public int SelfBuildCounter { get; private set; }
        public int CurrentAnimationFrame = -1;
        private DeveloperMode devMode;

        private int decaycounter;

        private WithSpriteBody wsb;

        public bool Cancled;

        public UndeadBuilder(ActorInitializer init, UndeadBuilderInfo info)
        {
            this.Info = info;
            decaycounter = info.DecayTime;
            PayPerTick = info.Cost / info.SummoningTime;
            SelfBuildCounter = info.SelfBuildDelay;

            wsb = init.Self.TraitsImplementing<WithSpriteBody>().Single(w => w.Info.Name == Info.Body);
        }

        public void Cancle()
        {
            Cancled = true;
        }

        void ITick.Tick(Actor self)
        {
            if (Cancled)
            {
                HasSummoningCount -= Info.SelfBuildSteps * 2;
            }

            if (Info.SummoningDecay)
            {
                decaycounter--;
                if (decaycounter <= 0 & HasSummoningCount > 0)
                {
                    HasSummoningCount--;
                    decaycounter = Info.DecayTime;
                }
            }

            if (HasSummoningCount >= Info.SummoningTime)
                ReplaceSelf(self);

            devMode = self.Owner.PlayerActor.TraitOrDefault<DeveloperMode>();
            if (devMode != null && devMode.FastBuild)
            {
                ReplaceSelf(self);
            }

            if (Info.Selfbuilds)
            {
                if (SelfBuildCounter-- <= 0)
                {
                    SelfBuildCounter = Info.SelfBuildDelay;
                    if (self.Owner.PlayerActor.Trait<PlayerResources>().TakeCash(PayPerTick, true))
                    {
                        HasSummoningCount += +Math.Min(Info.SelfBuildSteps, Info.SummoningTime - HasSummoningCount);
                        var floattest = PayPerTick.ToString();
                        floattest = "- " + floattest + " Essence";
                        if (self.Owner.IsAlliedWith(self.World.RenderPlayer))
                            self.World.AddFrameEndTask(w => w.Add(new FloatingTextBackwards(self.CenterPosition,
                                self.Owner.Color.RGB, floattest, 30)));
                    }
                }
            }

            var newFrameCounter = Info.Animations.Length * HasSummoningCount / Info.SummoningTime;

            if (newFrameCounter == Info.Animations.Length)
                return;

            if (Info.Animations.Any() && newFrameCounter != CurrentAnimationFrame)
            {
                wsb.PlayCustomAnimation(self, Info.Animations[newFrameCounter] + "-make",
                    () => { wsb.PlayCustomAnimationRepeating(self, Info.Animations[newFrameCounter]); });

                if (newFrameCounter == 0)
                    self.World.AddFrameEndTask(w =>
                    {
                        w.Add(new SpriteEffect(self.CenterPosition,
                            self.World,
                            Info.SpawnFlashAnimation,
                            Info.SpawnFlashAnimation,
                            Info.SpawnPaleete));
                    });
            }

            CurrentAnimationFrame = newFrameCounter;
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