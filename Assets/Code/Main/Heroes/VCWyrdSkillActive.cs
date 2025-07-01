using Awaken.TG.Main.Heroes.Development.WyrdPowers;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Heroes {
    public abstract class VCWyrdSkillActive : ViewComponent<Hero> {
        protected bool _isWyrdSkillActive;
        
        protected abstract void RefreshState();
        
        protected override void OnAttach() {
            Target.AfterFullyInitialized(AfterFullyInitialized, this);
        }

        void AfterFullyInitialized() {
            _isWyrdSkillActive = World.Any<WyrdSoulFragments>()?.HasAnySkill ?? false;
            if (!_isWyrdSkillActive) {
                Target.ListenToLimited(Hero.Events.WyrdSoulFragmentCollected, () => {
                    _isWyrdSkillActive = true;
                    RefreshState();
                }, this);
            }
            
            RefreshState();
        }
    }
}