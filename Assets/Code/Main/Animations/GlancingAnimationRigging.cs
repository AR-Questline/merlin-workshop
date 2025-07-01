using System.Threading;
using Awaken.TG.Main.AI.Barks;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Grounds;
using Awaken.TG.MVC;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Animations {
    public partial class VCAnimationRiggingHandler {
        class GlancingAnimationRigging : AnimationRigging {
            const float RepeatGlancingAfterTimeElapsed = 3f;
            const float DotTowardsHeroToStartGlancing = 0.6f;
            const float DotTowardsHeroToStopGlancing = 0.48f;

            bool _isGlancing;
            float _timeElapsedSinceLastGlanceCheck;
            CancellationTokenSource _glanceCancellationToken;

            public bool Active => _isGlancing;

            protected override void OnInit() {
                _data.lookAt = GroundedPosition.HeroPosition;
                
                _data.rootRigDesiredWeight = 0;
                _data.headRigDesiredWeight = 0;
                _data.bodyRigDesiredWeight = 0;
                _data.combatRigDesiredWeight = 0;
                _data.attackRigDesiredWeight = 0;
                
                _data.headTurnSpeed = DefaultHeadRigUpdateSpeed;
                _data.bodyTurnSpeed = DefaultBodyRigUpdateSpeed;
                _data.rootTurnSpeed = DefaultRootRigUpdateSpeed;
                _data.combatTurnSpeed = DefaultCombatRigUpdateSpeed;
                _data.attackTurnSpeed = DefaultCombatRigUpdateSpeed;
            }

            public void Update(float deltaTime) {
                if (!_isGlancing) {
                    _timeElapsedSinceLastGlanceCheck += deltaTime;
                    bool canGlance = Handler._inBand &&
                                     _timeElapsedSinceLastGlanceCheck >= RepeatGlancingAfterTimeElapsed &&
                                     CurrentInteraction is { AllowGlancing: true } &&
                                     DotTowardsHero > DotTowardsHeroToStartGlancing;
                    if (canGlance) {
                        Glance().Forget();
                    }
                } else {
                    bool cannotGlance = !Handler._inBand ||
                                        CurrentInteraction is not { AllowGlancing: true } ||
                                        DotTowardsHero < DotTowardsHeroToStopGlancing;
                    if (cannotGlance) {
                        Cancel();
                    }
                }
            }
            
            async UniTaskVoid Glance() {
                _isGlancing = true;
            
                _glanceCancellationToken?.Cancel();
                _glanceCancellationToken = new CancellationTokenSource();
            
                _timeElapsedSinceLastGlanceCheck = 0;
                SetupRigsWeightsFromCurrentInteraction();
            
                float glanceTime = GlanceTimeRange.RandomPick();
                float glanceDelay = GlanceDelayRange.RandomPick();
            
                NpcElement.TryGetElement<BarkElement>()?.OnNoticeHero();

                bool success = await AsyncUtil.DelayTime(Handler, glanceTime, source: _glanceCancellationToken);
                if (!success) {
                    _isGlancing = false;
                    return;
                }
            
                _data.headRigDesiredWeight = 0;
                _data.bodyRigDesiredWeight = 0;
            
                await AsyncUtil.DelayTime(Handler, glanceDelay, source: _glanceCancellationToken);
            
                _isGlancing = false;
            }
            
            public void Cancel() {
                _isGlancing = false;
                _glanceCancellationToken?.Cancel();
                _glanceCancellationToken = null;
            }
        }
    }
}