using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Mw.Traits
{
    [Desc("Is Unit a Peasant (adds a count of 1 to the PlayerCivilisation).")]
    public class UndeadBuilderInfo : ITraitInfo
    {
        public readonly string ReadyAudio = "ConstructionComplete";
        
        public readonly string SpawnActor = null;
        
        public readonly CVec SpawnOffset = new CVec(0,0);
        
        public readonly int SummoningTime = 100;
        
        public readonly bool SummoningDecay = false;
        
        public readonly int DecayTime = 25;

        public readonly bool SkipMakeAnims = false;

        public readonly bool ForceHealthPercentage = false;

        public readonly int Cost = 1000;
        
        public object Create(ActorInitializer init) { return new UndeadBuilder(init.Self, this); }
    }

    public class UndeadBuilder : ITick
    {
        public UndeadBuilderInfo info;
        readonly Actor self;

        public int hassummoningcount;
        public int decaycounter;

        public int PayPerTick;
        private DeveloperMode devMode;
		
        public UndeadBuilder(Actor self, UndeadBuilderInfo info)
        {
            this.info = info;
            this.self = self;
            decaycounter = info.DecayTime;
            PayPerTick = info.Cost / info.SummoningTime;
        }


        public void Tick(Actor self)
        {
            if (info.SummoningDecay)
            {
                decaycounter--;
                if (decaycounter <= 0 & hassummoningcount>0)
                {
                    hassummoningcount--;
                    decaycounter = info.DecayTime;
                }
            }

            if (hassummoningcount >= info.SummoningTime)
                replaceSelf(self);
            
            devMode = self.Owner.PlayerActor.TraitOrDefault<DeveloperMode>();
            if (devMode != null && devMode.FastBuild)
            {
                replaceSelf(self);
            }
        }

        public void replaceSelf(Actor self)
        {
            
            self.World.AddFrameEndTask(w =>
            {
                if (self.IsDead)
                    return;

                var selected = w.Selection.Contains(self);
                var controlgroup = w.Selection.GetControlGroupForActor(self);
						
				
                var init = new TypeDictionary
                {
                    new LocationInit(self.Location + info.SpawnOffset),
                    new CenterPositionInit(self.CenterPosition),
                    new OwnerInit(self.Owner),
                };

                if (info.SkipMakeAnims)
                    init.Add(new SkipMakeAnimsInit());

                var health = self.TraitOrDefault<Health>();
                if (health != null && info.ForceHealthPercentage)
                {
                    var newHP = (health.HP * 100) / health.MaxHP;
                    init.Add(new HealthInit(newHP));
                }

                var a = w.CreateActor(info.SpawnActor, init);

                if (selected)
                    w.Selection.Add(w, a);
                if (controlgroup.HasValue)
                    w.Selection.AddToControlGroup(a, controlgroup.Value);
                
                Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.ReadyAudio,
                    self.Owner.Faction.InternalName);
				
                self.Dispose();
            });
        }
    }
    
}
