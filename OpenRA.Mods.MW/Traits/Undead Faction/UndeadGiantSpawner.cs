using System;
using System.Collections.Generic;
using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits
{
    public class UndeadGiantSpawnerInfo : ITraitInfo
    {
        [Desc("Cooldwon until another Actor can Spawn from here")]
        public readonly int Cooldown = 100;
        public readonly CPos Exit = new CPos(0, 1);
        public readonly WPos SpawnOffset = new WPos(0, 0, 0);

        public object Create(ActorInitializer init) { return new UndeadGiantSpawner(init.Self, this); }
    }

    public class UndeadGiantSpawner : ITick
    {
        private UndeadGiantSpawnerInfo info;
        public bool Canspawn;
        public int Countdown;
        public UndeadGiantSpawner(Actor self, UndeadGiantSpawnerInfo info)
        {
            this.info = info;
            Countdown = info.Cooldown;
        }

        void ITick.Tick(Actor self)
        {
            if (Countdown-- > 0)
            {
                if (Canspawn == true)
                {
                    Canspawn = false;
                }
            }
            else if (Canspawn == false)
            {
                Canspawn = true;
            }
        }

        public void Reset()
        {
            Countdown = info.Cooldown;
            Canspawn = false;
        }
    }
}
