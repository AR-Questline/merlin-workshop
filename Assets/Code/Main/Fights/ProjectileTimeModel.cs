using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Skills;
using Awaken.TG.Main.Timing.ARTime;

namespace Awaken.TG.Main.Fights {
    public partial class ProjectileTimeModel : TimeModel, ISkillOwner, ITimeDependentDisabler {
        ICharacter _character;
        ProjectileBehaviour _projectileBehaviour;
        public ICharacter Character => _character;
        
        [UnityEngine.Scripting.Preserve]
        public void SetCharacter(ICharacter character) {
            _character = character;
        }

        public bool TimeUpdatesDisabled => !CachedView(ref _projectileBehaviour)?.isActiveAndEnabled ?? true;
    }
}
