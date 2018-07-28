using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits
{
    class GarbageCollectorInfo : ConditionalTraitInfo
    {
        [Desc("Wich Resource can be collected.")]
        public readonly HashSet<string> ResourcesNames = new HashSet<string>();

        public readonly int CollectInterval = 250;

        public readonly int CollectRange = 10;

        [WeaponReference, FieldLoader.Require, Desc("Default weapon to use for explosion Effect")]
        public readonly string Weapon = null;

        public WeaponInfo WeaponInfo { get; private set; }

        public override object Create(ActorInitializer init) { return new GarbageCollector(this); }

        public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
        {
            WeaponInfo = string.IsNullOrEmpty(Weapon) ? null : rules.Weapons[Weapon.ToLowerInvariant()];

            base.RulesetLoaded(rules, ai);
        }
    }

    class GarbageCollector : ConditionalTrait<GarbageCollectorInfo>, ITick, INotifyCreated
    {
        readonly GarbageCollectorInfo info;
        private int tickler;
        private ResourceLayer resLayer;

        public GarbageCollector(GarbageCollectorInfo info) : base(info)
        {
            this.info = info;
            tickler = info.CollectInterval;
        }

        void INotifyCreated.Created(Actor self)
        {
            resLayer = self.World.WorldActor.Trait<ResourceLayer>();
        }

        void ITick.Tick(Actor self)
        {
            if (IsTraitDisabled)
                return;

            if (tickler-- <= 0)
            {
                var cells = self.World.Map.FindTilesInCircle(self.Location, info.CollectRange, true)
                    .Where(c =>
                    {
                        if (resLayer.GetResource(c) == null)
                            return false;
                        if (info.ResourcesNames.Contains(resLayer.GetResource(c).Info.Name))
                            return true;
                        return false;
                    });

                var zapRandom = CPos.Zero;

                if (cells != null && cells.Any() && cells.Count() > 1)
                    zapRandom = cells.MinByOrDefault(c => (self.Location - c).LengthSquared);
                else if (cells != null && cells.Any() && cells.Count() == 1)
                    zapRandom = cells.First();

                if (zapRandom != CPos.Zero)
                {
                    var ammount = resLayer.GetResource(zapRandom).Info.ValuePerUnit;
                    var playerResources = self.Owner.PlayerActor.Trait<PlayerResources>();

                    playerResources.GiveResources(ammount);

                    if (ammount > 0 && self.IsInWorld && !self.IsDead)
                    {
                        var floattest = ammount.ToString();

                        floattest = "+ " + floattest + " Essence";

                        if (self.Owner.IsAlliedWith(self.World.RenderPlayer))
                            self.World.AddFrameEndTask(w => w.Add(new FloatingText(self.CenterPosition,
                                self.Owner.Color.RGB, floattest, 30)));
                    }

                    resLayer.Harvest(zapRandom);
                    if (resLayer.GetResourceDensity(zapRandom) <= 0)
                        resLayer.Destroy(zapRandom);

                    var weapon = Info.WeaponInfo;
                    if (weapon == null)
                        return;

                    if (weapon.Report != null && weapon.Report.Any())
                        Game.Sound.Play(SoundType.World, weapon.Report.Random(self.World.SharedRandom), self.CenterPosition);

                    weapon.Impact(Target.FromPos(self.World.Map.CenterOfCell(zapRandom)), self, Enumerable.Empty<int>());

                    tickler = info.CollectInterval;
                }
            }
        }
    }
}
