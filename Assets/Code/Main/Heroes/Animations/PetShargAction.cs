using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Attachments.Customs;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Overrides;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Heroes.MovementSystems;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Awaken.TG.Main.Heroes.Animations {
    public partial class PetShargAction : AbstractLocationAction {
        public sealed override bool IsNotSaved => true;

        bool _handCutOff;
        IEventListener _handCutListener, _exitListener;
        HeroLocationInteractionInvolvement _involvement;
        ARAsyncOperationHandle<GameObject> _cutOffHandHandle;
        ARAssetReference _cutOffHand;
        GameObject _cutOffHandInstance;
        bool _exited;
        
        Sharg _sharg;
        Sharg Sharg => ParentModel.CachedElement(ref _sharg);
        public override string DefaultActionName => LocTerms.Pet.Translate();

        public override ActionAvailability GetAvailability(Hero hero, IInteractableWithHero interactable) {
            if (Hero.Current.HasElement<HeroOffHandCutOff>()) {
                return ActionAvailability.Disabled;
            }

            if (!Sharg.IsSleeping) {
                return ActionAvailability.Disabled;
            }

            Vector3 shargForward = Sharg.NpcElement.Forward().ToHorizontal3();
            Vector3 dirToHero = (Hero.Current.Coords - Sharg.Coords).ToHorizontal3().normalized;
            float dot = Vector3.Dot(shargForward, dirToHero);
            return dot > 0.85f ? base.GetAvailability(hero, interactable) : ActionAvailability.Disabled;
        }

        protected override void OnStart(Hero hero, IInteractableWithHero interactable) {
            _exitListener = Hero.Current.ListenToLimited(HeroPetSharg.Events.PetShargEnded, Exit, this);
            _exited = false;
            _cutOffHand = CommonReferences.Get.cutOffHand.Get();
            _cutOffHand.PreloadLight<GameObject>();

            VHeroController heroController = hero.VHeroController;
            heroController.HeroCamera.SetPitch(0);
            heroController.HeroCamera.SetActiveFppArmsRotation(false);
            Transform fppArmsTransform = heroController.fppParent.transform;
            var armsXRotation = fppArmsTransform.localEulerAngles.x;
            ResetFPPArmsRotation(fppArmsTransform).Forget();
            heroController.dialogueVirtualCamera.transform.localEulerAngles = new Vector3(armsXRotation, 0, 0);
            heroController.ToggleCrouch(0.25f, false);
            
            ParentModel.AddElement<HeroInteractionFocusOverride>();
            _involvement = ParentModel.AddElement(new HeroLocationInteractionInvolvement(ParentModel, true, false));
            hero.Trigger(Hero.Events.HideWeapons, true);

            HeroAnimatorSubstateMachine machine = Hero.TppActive 
                ? hero.Element<LegsFSM>() 
                : hero.Element<HeroOverridesFSM>();
            machine.SetCurrentState(HeroStateType.PetSharg, 0f);
            hero.TrySetMovementType(out SnapToPositionMovement movement);
            movement.AssignDesiredPosition(Sharg.ShargPetHeroPosition);
            
            _handCutListener = Sharg.ListenToLimited(EnemyBaseClass.Events.AnimationEvent, OnAnimationEvent, this);
            
            Sharg.NpcElement.NpcAI.SetActivePerceptionUpdate(false);
            Sharg.NpcElement.SetAnimatorState(NpcFSMType.OverridesFSM, NpcStateType.CustomAction, 0);
        }

        async UniTaskVoid ResetFPPArmsRotation(Transform fppArmsTransform) {
            Vector3 eulerAngles = fppArmsTransform.localEulerAngles;
            while (math.abs(eulerAngles.x) > 0.01f) {
                eulerAngles.x = Mathf.MoveTowards(eulerAngles.x, 0, ParentModel.GetDeltaTime() * 15f);
                fppArmsTransform.localEulerAngles = eulerAngles;
                if (!await AsyncUtil.DelayFrame(this, 1)) {
                    return;
                }
            }
        }
        
        void OnAnimationEvent(ARAnimationEvent animationEvent) {
            if (_handCutOff) {
                return;
            }

            if (animationEvent.actionType == ARAnimationEvent.ActionType.SpecialAttackStart) {
                Transform hand = Hero.Current.OffHand;
                PrefabPool.InstantiateAndReturn(CommonReferences.Get.cutOffHandVFX, hand.position, hand.rotation, 1f).Forget();
                SpawnCutOffHand();
                _handCutOff = true;
            }
        }

        void SpawnCutOffHand() {
            _cutOffHandHandle = _cutOffHand.LoadAsset<GameObject>();
            _cutOffHandHandle.OnComplete(h => {
                if (HasBeenDiscarded || _exited) {
                    ReleaseCutoffHand();
                    return;
                }

                if (h.Status != AsyncOperationStatus.Succeeded || h.Result == null) {
                    ReleaseCutoffHand();
                    return;
                }
                
                _cutOffHandInstance = Object.Instantiate(h.Result, Sharg.NpcElement.MainHand, false);
            });
        }

        void Exit() {
            if (_exited) {
                return;
            }

            _exited = true;
            var hero = Hero.Current;
            Sharg.NpcElement.Interactor.Stop(InteractionStopReason.StoppedIdlingInstant, false);
            Sharg.NpcElement.NpcAI.SetActivePerceptionUpdate(true);
            Sharg.NpcElement.Controller.ToggleGlobalRichAIActivity(true);
            Sharg.NpcAI.EnterCombatWith(hero);
            Sharg.StartWaitBehaviour();
            ParentModel.RemoveElementsOfType<HeroInteractionFocusOverride>();
            
            hero.ReturnToDefaultMovement();
            hero.AddElement<HeroOffHandCutOff>();
            hero.VHeroController.HeroCamera.SetActiveFppArmsRotation(true);
            
            _involvement?.Discard();
            _involvement = null;

            ReleaseCutoffHand();
            if (_cutOffHandInstance != null) {
                Object.Destroy(_cutOffHandInstance);
                _cutOffHandInstance = null;
            }
            _cutOffHand.ReleaseAsset();
            _cutOffHand = null;
            
            World.EventSystem.TryDisposeListener(ref _handCutListener);
            World.EventSystem.TryDisposeListener(ref _exitListener);
        }

        void ReleaseCutoffHand() {
            if (_cutOffHandHandle.IsValid()) {
                _cutOffHandHandle.Release();
                _cutOffHandHandle = default;
            }
        }
    }
}