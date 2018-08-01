using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits
{
    [Desc("How much the unit is worth in Peasants.")]
    public class HuntableDeerInfo : ITraitInfo
    {
        [FieldLoader.Require]
        [Desc("Huntable Type")]
        public readonly HashSet<string> HuntTypes = new HashSet<string>();

        [ActorReference, FieldLoader.Require]
        [Desc("Actor to spawn on death.")]
        public readonly string Actor = null;

        [Desc("Probability the actor spawns.")]
        public readonly int Probability = 100;

        [Desc("Owner of the spawned actor. Allowed keywords:" +
              "'Victim', 'Killer' and 'InternalName'.")]
        public readonly OwnerType OwnerType = OwnerType.Victim;

        [Desc("Map player to use when 'InternalName' is defined on 'OwnerType'.")]
        public readonly string InternalOwner = null;

        [Desc("DeathType that triggers the actor spawn. " +
              "Leave empty to spawn an actor ignoring the DeathTypes.")]
        public readonly string DeathType = null;

        [Desc("Skips the spawned actor's make animations if true.")]
        public readonly bool SkipMakeAnimations = true;

        [Desc("Should an actor only be spawned when the 'Creeps' setting is true?")]
        public readonly bool RequiresLobbyCreeps = false;

        public object Create(ActorInitializer init) { return new HuntableDeer(init, this); }
    }

    public class HuntableDeer : INotifyKilled
    {
        readonly HuntableDeerInfo info;
        readonly string faction;
        readonly bool enabled;

        public Actor Hunter;

        public HuntableDeer(ActorInitializer init, HuntableDeerInfo info)
        {
            this.info = info;
            enabled = !info.RequiresLobbyCreeps || init.Self.World.WorldActor.Trait<MapCreeps>().Enabled;
            faction = init.Contains<FactionInit>() ? init.Get<FactionInit, string>() : init.Self.Owner.Faction.InternalName;
        }

        void INotifyKilled.Killed(Actor self, AttackInfo e)
        {
            if (!enabled)
                return;

            if (!self.IsInWorld)
                return;

            if (self.World.SharedRandom.Next(100) > info.Probability)
                return;

            if (info.DeathType != null && !e.Damage.DamageTypes.Contains(info.DeathType))
                return;

            self.World.AddFrameEndTask(w =>
            {
                // Actor has been disposed by something else before its death (for example `Enter`).
                if (self.Disposed)
                    return;

                var td = new TypeDictionary
                {
                    new ParentActorInit(self),
                    new LocationInit(self.Location),
                    new CenterPositionInit(self.CenterPosition),
                    new FactionInit(faction)
                };

                if (info.OwnerType == OwnerType.Victim)
                    td.Add(new OwnerInit(self.Owner));
                else if (info.OwnerType == OwnerType.Killer)
                    td.Add(new OwnerInit(e.Attacker.Owner));
                else
                    td.Add(new OwnerInit(self.World.Players.First(p => p.InternalName == info.InternalOwner)));

                if (info.SkipMakeAnimations)
                    td.Add(new SkipMakeAnimsInit());

                foreach (var modifier in self.TraitsImplementing<IDeathActorInitModifier>())
                    modifier.ModifyDeathActorInit(self, td);

                var huskActor = self.TraitsImplementing<IHuskModifier>()
                    .Select(ihm => ihm.HuskActor(self))
                    .FirstOrDefault(a => a != null);

                var unit = w.CreateActor(huskActor ?? info.Actor, td);

                if (e.Attacker != null && e.Attacker.IsInWorld && !e.Attacker.IsDead && e.Attacker.TraitsImplementing<DeerHunter>().Any())
                    e.Attacker.TraitsImplementing<DeerHunter>().FirstOrDefault().DeerLoot = unit;

                if (e.Attacker != null && e.Attacker.IsInWorld && !e.Attacker.IsDead && unit.TraitsImplementing<LootableBody>().Any())
                    unit.TraitsImplementing<LootableBody>().FirstOrDefault().Hunter = e.Attacker;
            });
        }
    }
}
