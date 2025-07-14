using System;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Combat.CustomDeath;
using Awaken.TG.Main.AI.Combat.CustomDeath.Forwarder;
using Awaken.TG.Main.AI.Movement.RootMotions;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Overrides;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Attachments.Elements.DeathBehaviours;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Cysharp.Threading.Tasks;
using Pathfinding;
using Pathfinding.RVO;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.Fights.Finishers {
    [Serializable]
    public class FinisherData {
        const float HeroMovePercent = 0.4f;
        [LabelText("Required Enemy Abstracts. Uses AND logic for each Abstract on the list")]
        [SerializeField, TemplateType(typeof(NpcTemplate))] TemplateReference[] targetAbstracts;
        [SerializeField] CustomDeathAnimation targetDeathAnimation;
        [SerializeField, HeroAnimancerAnimationsAssetReference] ShareableARAssetReference heroAnimationRef;

        [BoxGroup("Health Conditions"), SerializeField] bool overrideHealthConditions;
        [BoxGroup("Health Conditions"), SerializeField, ShowIf(nameof(overrideHealthConditions))] FinisherHealthCondition hpCondition = FinisherHealthCondition.Default;
        [BoxGroup("Position Snap"), SerializeField] Vector3 heroPositionOffset;
        [BoxGroup("Position Snap"), SerializeField] float heroSnapDuration = 0.33f;
        [BoxGroup("Position Snap"), SerializeField, RichEnumExtends(typeof(EasingType))] RichEnumReference heroSnapEasingType = EasingType.CubicInOut;
        [BoxGroup("Position Snap"), SerializeField] float targetSnapDuration = 0.33f;
        [BoxGroup("Position Snap"), SerializeField, RichEnumExtends(typeof(EasingType))] RichEnumReference targetSnapEasingType = EasingType.CubicInOut;
        [BoxGroup("Slowdown"), SerializeField] bool slowDownTime;
        
        ARAsyncOperationHandle<ARHeroStateToAnimationMapping> _heroAnimationHandle;
        
        public void Init() {
            targetDeathAnimation.Preload();
            _heroAnimationHandle = heroAnimationRef.Get().LoadAsset<ARHeroStateToAnimationMapping>();
        }


        public bool CheckConditions(DamageOutcome damageOutcome, float predictedDamage, bool hpConditionIsFulfilled, bool isValidationCheck = false) {
            if (damageOutcome.Target is not NpcElement { HasBeenDiscarded: false, IsAlive: true, CanUseExternalCustomDeath: true } target) {
                return false;
            }

            float npcHp = damageOutcome.FinalAmount;
            if (overrideHealthConditions ? !hpCondition.IsFulfilled(predictedDamage, npcHp, target) : !hpConditionIsFulfilled) {
                return false;
            }
            
            if (_heroAnimationHandle.Result == null) {
                return false;
            }
            
            return targetAbstracts.All(abstractTemplateReference => target.Template.InheritsFrom(abstractTemplateReference.Get<NpcTemplate>())) 
                   && targetDeathAnimation.CheckConditions(damageOutcome, isValidationCheck);
        }

        public void PlayAnimations(DamageOutcome outcome, NpcElement target, Hero hero) {
            LoadAndPlayNpcAnimation(outcome, target).Forget();
            LoadHeroAnimation(hero);
            
            var targetLocation = target.ParentModel;
            CalculateData(hero, targetLocation, out var heroFinalPos, out var targetStartingPos, out var targetFinalPos);
            
            hero.Trigger(FinisherState.Events.FinisherStarted, new RuntimeData() {
                heroFinalPosition = heroFinalPos,
                heroLookAtPosition = targetFinalPos,
                heroMoveEasingType = heroSnapEasingType.EnumAs<EasingType>(),
                heroMoveDeltaTimeMultiplier = 1 / heroSnapDuration,
                target = targetLocation,
                slowDownTime = slowDownTime
            });
            hero.ListenToLimited(FinisherState.Events.FinisherEnded, FinishPlayAnimations, hero);

            MoveTargetToPosition(targetLocation, targetStartingPos, targetFinalPos, Quaternion.LookRotation(heroFinalPos-targetFinalPos)).Forget();
        }

        async UniTaskVoid LoadAndPlayNpcAnimation(DamageOutcome outcome, NpcElement target) {
            target.Movement.InterruptState(new NoMove());
            targetDeathAnimation.Load(target);
            targetDeathAnimation.Apply();

            var location = target.ParentModel;
            var ragdollData = targetDeathAnimation.UseCustomRagdollData ? targetDeathAnimation.CustomRagdollData : PostponedRagdollBehaviourBase.RagdollEnableData.Default;
            location.AddElement(new FinisherDeathAnimationForwarder(ragdollData));
            location.Trigger(DeathElement.Events.RefreshDeathBehaviours, true);

            var controller = target.Controller;
            var targetTransform = controller.transform;
           
            RichAI richAI = controller.RichAI;
            RootMotion rm = controller.RootMotion;
            RVOController rvo = controller.RvoController;

            if (!await AsyncUtil.DelayFrame(target)) {
                return;
            }
            
            controller.RemoveMovementRefs();
            target.Element<HealthElement>().KillFromFinisher(outcome);
            HandleNPCRootMotion(rm, richAI, rvo, location, targetTransform, ragdollData).Forget();
        }

        async UniTaskVoid HandleNPCRootMotion(RootMotion rm, RichAI richAI, RVOController rvo, Location location, Transform targetTransform,PostponedRagdollBehaviourBase.RagdollEnableData ragdollData) {
            rm.enabled = true;
            richAI.enabled = true;
            rvo.enabled = true;
            
            Vector3 rvoPositionOnLastMove = Vector3.zero;
            Vector3 accumulatedVelocitySinceLastRvoUpdate = Vector3.zero;
            
            rm.OnAnimatorMoved += OnAnimatorMoved;

            if (ragdollData.enableRagdollAfterAnimation) {
                if (!await AsyncUtil.DelayTime(location, ragdollData.delayToEnterRagdoll.max)) {
                    return;
                }
            } else {
                if (!await AsyncUtil.DelayTime(location, 5)) {
                    return;
                }
            }

            if (rm != null) {
                rm.OnAnimatorMoved -= OnAnimatorMoved;
                Object.Destroy(rm);
            }
            if (richAI != null) {
                Object.Destroy(richAI);
            }
            if (rvo != null) {
                Object.Destroy(rvo);
            }

            void OnAnimatorMoved(Animator animator) {
                if (animator.deltaPosition.magnitude > 0.01f) {
                    if (rvo != null) {
                        if (rvo.position != rvoPositionOnLastMove) {
                            rvoPositionOnLastMove = rvo.position;
                            accumulatedVelocitySinceLastRvoUpdate = Vector3.zero;
                        }
                        accumulatedVelocitySinceLastRvoUpdate += animator.deltaPosition / location.GetDeltaTime();
                        rvo.Move(accumulatedVelocitySinceLastRvoUpdate);
                    } else if (richAI != null) {
                        richAI.Move(animator.deltaPosition);
                    }
                }

                if (animator.deltaRotation != Quaternion.identity) {
                    float deltaAngle = Mathf.DeltaAngle(0, animator.deltaRotation.eulerAngles.y);
                    richAI.rotation *= Quaternion.Euler(0, deltaAngle, 0);
                }
                
                richAI.FinalizeMovement(targetTransform.position, targetTransform.rotation);
            }
        }

        void LoadHeroAnimation(Hero hero) {
            var heroAnimancer = hero.MainView.GetComponentInChildren<ARHeroAnimancer>(true);
            heroAnimancer.ApplyOverrides(this, _heroAnimationHandle.Result);
        }
        
        void CalculateData(Hero hero, Location target, out Vector3 heroFinalPos, out Vector3 targetStartingPos, out Vector3 targetFinalPos) {
            targetStartingPos = target.Coords;
            var heroStartingPos = hero.Coords;
            heroFinalPos = targetStartingPos + target.Rotation * heroPositionOffset;
            var heroMoveDir = heroFinalPos - heroStartingPos;
            targetFinalPos = targetStartingPos - heroMoveDir * (1 - HeroMovePercent);
            heroFinalPos = heroStartingPos + heroMoveDir * HeroMovePercent;
        }

        async UniTaskVoid MoveTargetToPosition(Location targetLocation, Vector3 targetFrom, Vector3 targetTo, Quaternion targetRotation) {
            float percentage = 0f;
            float deltaTimeMultiplier = 1f / targetSnapDuration;
            EasingType easingType = targetSnapEasingType.EnumAs<EasingType>();
            while (percentage < 1f) {
                percentage += targetLocation.GetDeltaTime() * deltaTimeMultiplier;
                float easedLerpValue = easingType.Calculate(percentage);
                var pos = Vector3.Lerp(targetFrom, targetTo, easedLerpValue);
                var rot = Quaternion.Lerp(targetLocation.Rotation, targetRotation, easedLerpValue);
                targetLocation.MoveAndRotateTo(pos, rot);
                await AsyncUtil.DelayFrame(targetLocation);
            }
            targetLocation.MoveAndRotateTo(targetTo, targetRotation);
        }

        void FinishPlayAnimations() {
            var heroAnimancer = Hero.Current.MainView.GetComponentInChildren<ARHeroAnimancer>(true);
            heroAnimancer.RemoveOverrides(this, _heroAnimationHandle.Result);
            targetDeathAnimation.UnloadAndClear();
        }

        public void Unload() {
            targetDeathAnimation.UnloadAndClear();
            targetDeathAnimation.UnloadPreload();
            _heroAnimationHandle.Release();
        }

        public struct RuntimeData {
            public Vector3 heroFinalPosition;
            public Vector3 heroLookAtPosition;
            public EasingType heroMoveEasingType;
            public float heroMoveDeltaTimeMultiplier;
            public Location target;
            public bool slowDownTime;
        }
    }
}
