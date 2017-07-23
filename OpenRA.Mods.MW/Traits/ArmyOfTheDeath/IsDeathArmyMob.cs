using System.Collections.Generic;
using System.Drawing;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;

namespace OpenRA.Mods.Mw.Traits
{
	[Desc("Is this Unit part of the Dead Army Chooser.")]
	public class IsDeathArmyMobInfo  : ITraitInfo
	{
		[FieldLoader.Require]
		[Desc("Wich Role Takes this unit in the Army. Possible are: Fighter, Commander and Utility.")]
		public readonly string Role = "Fighter";
		
		public object Create(ActorInitializer init) { return new IsDeathArmyMob(init, this); }

	}

	public class IsDeathArmyMob : INotifyAttack, INotifyDamage, ITick, IRenderAboveShroud
	{
		private readonly IsDeathArmyMobInfo info;
		private int pseudoidleDuration;
		public Player GetAttackedorAttacking;
		private WPos Selfposition;
		
		public IsDeathArmyMob(ActorInitializer init,IsDeathArmyMobInfo info)
		{
			this.info = info;
			pseudoidleDuration = 100;
			Selfposition = init.Self.CenterPosition;
			GetAttackedorAttacking = null;
		}
		
		IEnumerable<IRenderable> IRenderAboveShroud.RenderAboveShroud(Actor self, WorldRenderer wr)
		{
			return RenderText(self, wr);
		}
		
		IEnumerable<IRenderable> RenderText(Actor self, WorldRenderer wr)
		{
			if (self.World.RenderPlayer != null)
			{
				if (self.Owner != self.World.RenderPlayer)
					return Enumerable.Empty<IRenderable>();
			}
			string message;
			if (self.CurrentActivity != null)
			{
				string[] g = self.CurrentActivity.ToString().Split('.');
				message = g.Last().Replace("Activity","");
			}
			else
				message = "Idle";

			var font = Game.Renderer.Fonts["Infestregular"];
			var screenPos = wr.ScreenPxPosition(self.CenterPosition);
			var rend = new IRenderable[] { new TextRenderable(font, wr.ProjectedPosition(screenPos)+new WVec(0,0,1024), -1024, Color.Bisque,message) };
			return rend;
		}

		void ITick.Tick(Actor self)
		{
			pseudoidleDuration--;
			if (pseudoidleDuration < 0)
			{
				pseudoidleDuration = 100;
				if (!self.IsIdle && self.CenterPosition == Selfposition)
				{
					self.CancelActivity();

					var mobile = self.Trait<Mobile>();
				
					var cell = self.Location;
					var moveTo = mobile.NearestMoveableCell(cell, 1, 3);
					self.QueueActivity(mobile.MoveTo(moveTo, 0));
					self.QueueActivity(new CallFunc(() =>
					{
						Selfposition = self.CenterPosition;
					}));
				}
			}
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			GetAttackedorAttacking = e.Attacker.Owner;
		}
		
		void INotifyAttack.Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			pseudoidleDuration = 100;
			GetAttackedorAttacking = target.Actor.Owner;
		}
		void INotifyAttack.PreparingAttack(Actor self, Target target, Armament a, Barrel barrel)
		{

		}
		
	}
}
