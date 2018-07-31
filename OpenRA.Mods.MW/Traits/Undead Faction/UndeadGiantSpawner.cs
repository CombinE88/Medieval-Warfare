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
        public bool canspawn;
        public int countdown;
        public UndeadGiantSpawner(Actor self, UndeadGiantSpawnerInfo info)
        {
            this.info = info;
            countdown = info.Cooldown;
        }

        void ITick.Tick(Actor self)
        {
            if (countdown-- > 0)
            {
                if (canspawn == true)
                {
                    canspawn = false;
                }
            }
            else if (canspawn == false)
            {
                canspawn = true;
            }
        }
        public void reset()
        {
            countdown = info.Cooldown;
            canspawn = false;
        }
    }
}
