
using System;
using OpenRA.Mods.Common.Effects;
using OpenRA.Traits;


namespace OpenRA.Mods.MW.Traits
{
	[Desc("A actor has to enter the building before the unit spawns.")]
	public class ResourcePurifierInfo : ITraitInfo
	{
		public readonly int Percentage = 10;
		public readonly bool ShowTicks = true;
		public readonly int TickLifetime = 30;
		public readonly int TickVelocity = 2;

		public object Create(ActorInitializer init) { return new ResourcePurifier(init.Self, this); }
	}

	class ResourcePurifier : ITick
	{
		private Actor self;
		private readonly ResourcePurifierInfo info;
		private PlayerResources playerResources;
		private int Resources;
		private int ResourcesTickBefore;
		int currentDisplayValue;
		


		public ResourcePurifier(Actor self, ResourcePurifierInfo info)
		{
			this.info = info;
			playerResources = self.Owner.PlayerActor.Trait<PlayerResources>();
			Resources = playerResources.Resources;
			ResourcesTickBefore = playerResources.Resources;
		}

		void ITick.Tick(Actor self)
		{
			Resources = playerResources.Resources;
			var CashGrant = Resources-ResourcesTickBefore;
			
			if (CashGrant>0)
			{
				var temp = (int) Math.Ceiling((CashGrant * info.Percentage) / 100.0);
				playerResources.GiveResources(temp);
				
				if (info.ShowTicks && temp > 0)
				{
					if (self.Owner.IsAlliedWith(self.World.RenderPlayer))
						self.World.AddFrameEndTask(w => w.Add(new FloatingText(self.CenterPosition, self.Owner.Color.RGB, FloatingText.FormatCashTick(temp), 30)));
					currentDisplayValue = 0;
				}
				
			}
			
			ResourcesTickBefore = playerResources.Resources;
		}

	}
}