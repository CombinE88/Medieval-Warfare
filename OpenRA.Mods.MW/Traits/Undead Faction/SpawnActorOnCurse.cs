#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits
{
	[Desc("Spawn another actor immediately upon death.")]
	public class SpawnActorOnCurseInfo : ConditionalTraitInfo
	{
		[Desc("Actor to spawn on death.")]
        public readonly string[] SpawnRandomActors = null;

        public readonly string[] Cussertypes = null;

        [Desc("Offset of the spawned actor relative to the dying actor's position.",
            "Warning: Spawning an actor outside the parent actor's footprint/influence might",
            "lead to unexpected behaviour.")]
        public readonly CVec Offset = CVec.Zero;

        [Desc("Skips the spawned actor's make animations if true.")]
        public readonly bool SkipMakeAnimations = true;

        public override object Create(ActorInitializer init) { return new SpawnActorOnCurse(init, this); }
	}

	class SpawnActorOnCurse : ConditionalTrait<SpawnActorOnCurseInfo>, INotifyRemovedFromWorld
	{
        SpawnActorOnCurseInfo info;
        public Player Cusserplayer;

        public SpawnActorOnCurse(ActorInitializer init, SpawnActorOnCurseInfo info)
            : base(info)
        {
            this.info = info;
        }

        public void AppliedDamage(Actor self, Actor damaged, AttackInfo e)
        {
            throw new NotImplementedException();
        }

        void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			if (Cusserplayer == null)
				return;

            if (IsTraitDisabled)
                return;

			var td = new TypeDictionary
			{
				new ParentActorInit(self),
				new LocationInit(self.Location + Info.Offset),
				new CenterPositionInit(self.CenterPosition),
                new OwnerInit(Cusserplayer)
            };


            foreach (var modifier in self.TraitsImplementing<IDeathActorInitModifier>())
                modifier.ModifyDeathActorInit(self, td);

            if (Info.SkipMakeAnimations)
				td.Add(new SkipMakeAnimsInit());

                self.World.AddFrameEndTask(w => w.CreateActor(info.SpawnRandomActors[self.World.SharedRandom.Next(0, info.SpawnRandomActors.Length)].ToLowerInvariant(), td));
		}
	}
}
