using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using System.Linq;



namespace OpenRA.Mods.MW.Traits
{
	public class AmmoPoolConditionsInfo : ConditionalTraitInfo, Requires<AmmoPoolInfo>
	{
		public readonly string ConditionFull = null;
		public readonly string ConditionEmpty = null;
		public readonly string AmmoPoolName = null;
		public override object Create(ActorInitializer init) { return new AmmoPoolConditions(init.Self, this); }
	}

	public class AmmoPoolConditions : ConditionalTrait<AmmoPoolConditionsInfo>, ITick
	{
		
		public AmmoPoolConditions(Actor self, AmmoPoolConditionsInfo info)
			: base(info) { }
		
		ConditionManager conditionManager;
		int tokenFull = ConditionManager.InvalidConditionToken;
		int tokenEmpty = ConditionManager.InvalidConditionToken;
		AmmoPool ammoPool;
		int maxAmmo;

		protected override void Created(Actor self)
		{
			ammoPool = self.TraitsImplementing<AmmoPool>().FirstOrDefault(la => la.Info.Name == Info.AmmoPoolName);
			maxAmmo = ammoPool.Info.Ammo;

			conditionManager = self.TraitOrDefault<ConditionManager>();
			base.Created(self);
		}

		public int GetAmmoCount() { return ammoPool.CurrentAmmo; }
		
		void ITick.Tick(Actor self)
		{

			if (conditionManager == null)
			{
				//Log.Write("debug", "Terminate");
				return;
			}
			
			if (GetAmmoCount() == maxAmmo)
			{
				if (tokenFull == ConditionManager.InvalidConditionToken)
				{
					tokenFull = conditionManager.GrantCondition(self, Info.ConditionFull);
				}
			}
			else if (tokenFull != ConditionManager.InvalidConditionToken)
			{
				tokenFull = conditionManager.RevokeCondition(self, tokenFull);
			}

			if (GetAmmoCount() == 0)
			{
				if (tokenEmpty == ConditionManager.InvalidConditionToken)
				{
					tokenEmpty = conditionManager.GrantCondition(self, Info.ConditionEmpty);
				}
			}
			else if (tokenEmpty != ConditionManager.InvalidConditionToken)
			{
				tokenEmpty = conditionManager.RevokeCondition(self, tokenEmpty);
			}
		}
	}
}