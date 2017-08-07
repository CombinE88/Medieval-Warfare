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
    public class GrimTargetInfo : ITraitInfo
    {
        [Desc("Condition grant to itself while when got reanimated")]
        public readonly string GrantCondition = null;
        
        [Desc("Max distance in Cells wich the puppet can go")]
        public readonly int maxDistance = 5;
        
        
        public object Create(ActorInitializer init) { return new GrimTarget(init.Self, this); }
    }

    public class GrimTarget : ITick, INotifyKilled, INotifyCreated
    {
        public GrimTargetInfo info;
        //private Actor self;
        ConditionManager conditionManager;
        int token = ConditionManager.InvalidConditionToken;

        public Actor Grim;
        public bool Reanimated;
		
        public GrimTarget(Actor self, GrimTargetInfo info)
        {
            this.info = info;
            //this.self = self;
            
        }

        public void Tick(Actor self)
        {
            if (Reanimated && token == ConditionManager.InvalidConditionToken && conditionManager != null)
            {
                GrantCondition(self);
            }

            if (Reanimated && Grim != null && Grim.IsInWorld && !Grim.IsDead && self.Info.HasTraitInfo<IMoveInfo>())
            {
                if (!self.IsDead && self.IsInWorld && (self.Location - Grim.Location).LengthSquared >
                    WDist.FromCells(info.maxDistance).LengthSquared)
                {
                    self.CancelActivity();
                    self.QueueActivity(self.Trait<IMove>().MoveTo(Grim.Location,2));
                }
            }
            else if (Reanimated && Grim == null)
                self.Kill(self);
            else if (Reanimated && Grim != null && (Grim.IsDead || !Grim.IsInWorld))
                self.Kill(self);
        }

        public void Killed(Actor self, AttackInfo e)
        {
            if (!Reanimated && e.Attacker.Info.HasTraitInfo<GrimReanimationInfo>() && e.Attacker.IsInWorld && !e.Attacker.IsDead)
            {
                if (e.Attacker.Trait<GrimReanimation>().Actor == null)
                {
                    var unit = self.World.CreateActor(true, self.Info.Name, new TypeDictionary
                    {
                        new LocationInit(self.Location),
                        new OwnerInit(e.Attacker.Owner),
                        new FacingInit(self.Trait<IFacing>().Facing)
                    });
                    unit.Trait<GrimTarget>().Reanimated = true;
                    unit.Trait<GrimTarget>().Grim = e.Attacker;
                    e.Attacker.Trait<GrimReanimation>().Actor = self;
                }
            }
        }

        public void GrantCondition(Actor self)
        {
            if (Reanimated && self.IsInWorld && !self.IsDead)
            {
                token = conditionManager.GrantCondition(self, info.GrantCondition);
            }
        }

        public void Created(Actor self)
        {
            conditionManager = self.TraitOrDefault<ConditionManager>();
        }
    } 
}
