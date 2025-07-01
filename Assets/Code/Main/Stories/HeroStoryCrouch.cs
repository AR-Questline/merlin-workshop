using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Stories {
    public partial class HeroStoryCrouch : Element<Story> {
        public sealed override bool IsNotSaved => true;

        readonly float _crouchDuration;
        readonly bool _useRealCrouch;
        bool _revertOnDiscard;
        
        HeroStoryCrouch(float duration, bool useRealCrouch, bool revertOnDiscard) {
            _crouchDuration = duration;
            _useRealCrouch = useRealCrouch;
            _revertOnDiscard = revertOnDiscard;
        }
        
        protected override void OnInitialize() {
            SetHeroCrouchedState(true, _crouchDuration);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            if (fromDomainDrop) {
                return;
            }

            if (_revertOnDiscard) {
                SetHeroCrouchedState(false, _crouchDuration);
            }
        }
        
        void SetHeroCrouchedState(bool state, float duration = 0.5f) {
            var heroController = Hero.Current.VHeroController;
            if (_useRealCrouch) {
                if (heroController.IsCrouching != state) {
                    heroController.ToggleCrouch(duration);
                }
            } else {
                heroController.StoryBasedCrouch(state, duration);
            }
        }

        void MarkToRevertOnDiscard() {
            _revertOnDiscard = true;
        }

        public static void StartCrouching(Story api, float duration = 0.5f, bool useRealCrouch = false, bool revertOnDiscard = false) {
            if (!api.TryGetElement(out HeroStoryCrouch storyCrouch)) {
                storyCrouch = new HeroStoryCrouch(duration, useRealCrouch, revertOnDiscard);
                api.AddElement(storyCrouch);
            } else if (revertOnDiscard) {
                storyCrouch.MarkToRevertOnDiscard();
            }
        }

        public static void StopCrouching(Story api, float duration = 0.5f) {
            if (api.TryGetElement(out HeroStoryCrouch storyCrouch)) {
                storyCrouch.SetHeroCrouchedState(false, duration);
            }
        }
    }
}