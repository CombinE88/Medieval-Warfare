using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using System.Linq;



namespace OpenRA.Mods.MW.Traits
{
	public class FormationInfoInfo : ITraitInfo
	{
		[Desc("Role this actor plays ina  formatin, possibles are: 'meele', 'pike', 'ranged', 'siege'")]
		public readonly string FormationRole;
		
		public object Create(ActorInitializer init) { return new FormationInfo(init, this); }
	}

	public class FormationInfo : INotifySelected, INotifySelection
	{

		public Actor FormationCoreActor = null;
		public Actor self;

		private readonly FormationInfoInfo info;
		


		public FormationInfo(ActorInitializer init, FormationInfoInfo info)
		{
			self = init.Self;
			this.info = info;
		}
		
		void INotifySelection.SelectionChanged()
		{
			
			
		}

		void INotifySelected.Selected(Actor self)
		{
			if (FormationCoreActor != null)
			{
				var Cache = OpenRA.Selection.;
			}
			
		}
		

}