using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Grounds;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Animations {
    public partial class VCAnimationRiggingHandler {
        class ObservingAnimationRigging : AnimationRigging {
            const float DotTowardsHeroToStartGlancing = 0.1f;
            const float DotTowardsHeroToStopGlancing = -0.1f;

            bool _isObserving;
            bool _isSeeing;

            public bool Active => _isObserving && _isSeeing;
            public override ref readonly AnimationRiggingData Data => ref _data;
            
            protected override void OnInit() {
                Location.ListenTo(NpcCrimeReactions.Events.ObservingStateChanged, OnObservingStateChanged, Handler);
                
                _data.lookAt = GroundedPosition.HeroPosition;
                
                _data.rootRigDesiredWeight = 0;
                _data.headRigDesiredWeight = 1;
                _data.bodyRigDesiredWeight = 1;
                _data.combatRigDesiredWeight = 0;
                _data.attackRigDesiredWeight = 0;
                
                _data.headTurnSpeed = DefaultHeadRigUpdateSpeed;
                _data.bodyTurnSpeed = DefaultBodyRigUpdateSpeed;
                _data.rootTurnSpeed = DefaultRootRigUpdateSpeed;
                _data.combatTurnSpeed = DefaultCombatRigUpdateSpeed;
                _data.attackTurnSpeed = DefaultCombatRigUpdateSpeed;
            }

            void OnObservingStateChanged(bool state) {
                _isObserving = state;
                SetupRigsWeightsFromCurrentInteraction();
            }

            public void Update(float deltaTime) {
                if (!_isObserving) {
                    _isSeeing = false;
                    return;
                }

                if (CurrentInteraction is { AllowGlancing: false }) {
                    _isSeeing = false;
                    return;
                }

                if (_isSeeing) {
                    _isSeeing = DotTowardsHero > DotTowardsHeroToStopGlancing;
                } else {
                    _isSeeing = DotTowardsHero > DotTowardsHeroToStartGlancing;
                }
            }
        }
    }
}