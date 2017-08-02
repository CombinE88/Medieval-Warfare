using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Mw.Traits
{
    [Desc("Is Unit a Peasant (adds a count of 1 to the PlayerCivilisation).")]
    public class BuildUpSoundInfo : ITraitInfo
    {
        
        public readonly string StartSound = null;
        
        public readonly string LoopSound = null;
        
        public readonly string EndSound = null;

        public int Time = 0;
        
        public object Create(ActorInitializer init) { return new BuildUpSound(init.Self, this); }
    }

    public class BuildUpSound :INotifyCreated, ITick, INotifyRemovedFromWorld, INotifyDeployComplete, INotifySold, INotifyBuildComplete
    {
        public BuildUpSoundInfo info;
        private bool isitend;
        private int remove;
        private bool disable;
        
        private ISound currentSound;
        HashSet<ISound> currentSounds = new HashSet<ISound>();
		
        public BuildUpSound(Actor self, BuildUpSoundInfo info)
        {
            this.info = info;
            isitend = false;
            remove = info.Time;

        }
        public void Created(Actor self)
        {
            isitend = false;
            currentSound = Game.Sound.Play(SoundType.World, info.StartSound, self.CenterPosition);
            currentSounds.Add(currentSound);
        }

        
        void StopSound()
        {
            Game.Sound.StopSound(currentSound);
            foreach (var s in currentSounds)
                Game.Sound.StopSound(s);

            currentSounds.Clear();
        }

        void INotifyRemovedFromWorld.RemovedFromWorld(Actor self) { StopSound(); }
        
        public void Tick(Actor self)
        {
            if (remove>0)
                remove--;
            if (currentSound.Complete && !isitend && remove >0)
            {
                isitend = true;
                currentSound = Game.Sound.PlayLooped(SoundType.World, info.LoopSound, self.CenterPosition);
                currentSounds.Add(currentSound);
            }
            if (!disable && remove <= 0)
            {
                disable = true;
                StopSound();
                currentSound = Game.Sound.Play(SoundType.World, info.EndSound, self.CenterPosition);
                currentSounds.Add(currentSound);
            }
        }

        public void FinishedDeploy(Actor self)
        {
            isitend = true;
            StopSound();
            currentSound = Game.Sound.Play(SoundType.World, info.EndSound, self.CenterPosition);
            currentSounds.Add(currentSound);
        }

        public void FinishedUndeploy(Actor self)
        {
            isitend = true;
            StopSound();
            currentSound = Game.Sound.Play(SoundType.World, info.EndSound, self.CenterPosition);
            currentSounds.Add(currentSound);
        }

        public void Selling(Actor self)
        {
            isitend = true;
            StopSound();
            currentSound = Game.Sound.Play(SoundType.World, info.StartSound, self.CenterPosition);
            currentSounds.Add(currentSound);
        }

        public void Sold(Actor self)
        {
            StopSound();
        }

        public void BuildingComplete(Actor self)
        {
            isitend = true;
            StopSound();
            currentSound = Game.Sound.Play(SoundType.World, info.EndSound, self.CenterPosition);
            currentSounds.Add(currentSound);
        }
    } 
}
