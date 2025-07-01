using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Relations;
using Awaken.TG.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Animations {
    public partial class VCAnimationRiggingHandler {
        class CombatAnimationRigging : AnimationRigging {
            const float DefaultAttackIKSliderValue = 0.5f;

            bool _isInCombat;
            bool _isInAttack;
            bool IsInAttack => _isInAttack && !Handler._isArcher;
            public bool Active => _isInCombat;

            protected override void OnInit() {
                NpcElement.ListenTo(NpcElement.Events.AnimatorEnteredAttackState, OnAttackStart, Handler);
                NpcElement.ListenTo(NpcElement.Events.AnimatorExitedAttackState, OnAttackEnd, Handler);
                NpcElement.ListenTo(AITargetingUtils.Relations.Targets.Events.Changed, OnTargetingChanged, Handler);

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
                _isInCombat = NpcElement.NpcAI?.InCombat == true;
                
                if (!Handler._inBand) {
                    Handler.UpdateCombatSlider(DefaultAttackIKSliderValue, deltaTime);
                    return;
                }
                
                if (_isInCombat) {
                    var hero = Hero.Current;
                    
                    Handler.CancelGlancing();
                    _data.combatRigDesiredWeight = IsInAttack ? 0 : 1;
                
                    float currentY = Handler.transform.position.y;
                    float aimTargetY = hero.Coords.y;
                    if (hero.IsCrouching) {
                        aimTargetY -= hero.Data.standingHeightData.height - hero.Data.crouchingHeightData.height;
                    }

                    float desiredCombatSliderValue;
                    float heightDifference = aimTargetY - currentY;
                    if (Mathf.Abs(heightDifference) <= IgnoreHeightDifferenceThreshold) {
                        desiredCombatSliderValue = DefaultAttackIKSliderValue;
                        _data.attackRigDesiredWeight = 0;
                    } else if (heightDifference > 0) {
                        desiredCombatSliderValue = Mathf.InverseLerp(currentY, currentY + ReachMaxLookUpWhenHeroAbove, aimTargetY);
                        desiredCombatSliderValue = desiredCombatSliderValue.Remap(0, 1, DefaultAttackIKSliderValue, 1);
                        _data.attackRigDesiredWeight = IsInAttack ? 1 : 0;
                    } else {
                        desiredCombatSliderValue = Mathf.InverseLerp(currentY + ReachMaxLookDownWhenHeroBelow, currentY, aimTargetY);
                        desiredCombatSliderValue = desiredCombatSliderValue.Remap(0, 1, 0, DefaultAttackIKSliderValue);
                        _data.attackRigDesiredWeight = IsInAttack ? 1 : 0;
                    }
                    Handler.UpdateCombatSlider(desiredCombatSliderValue, deltaTime);
                    CombatIKTarget.position = Vector3.Lerp(CombatIKPosLow.position, CombatIKPosUp.position, CombatIKSlider);
                } else {
                    Handler.UpdateCombatSlider(DefaultAttackIKSliderValue, deltaTime);
                }
            }

            void OnAttackStart() {
                if (Handler._isArcher) {
                    return;
                }
                _data.attackRigDesiredWeight = 1;
                _data.combatRigDesiredWeight = 0;
                _isInAttack = true;
            }

            void OnAttackEnd() {
                if (Handler._isArcher) {
                    return;
                }
                _data.attackRigDesiredWeight = 0;
                _data.combatRigDesiredWeight = 1;
                _isInAttack = false;
            }
            
            void OnTargetingChanged(RelationEventData data) {
                if (data is { to: IGrounded grounded }) {
                    _data.lookAt = GroundedPosition.ByGrounded(grounded);
                }
            }
        }
    }
}