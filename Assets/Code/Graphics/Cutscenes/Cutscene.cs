using System;
using Awaken.CommonInterfaces;
using Awaken.TG.Assets;
using Awaken.TG.Graphics.Transitions;
using Awaken.TG.Main.Cameras;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Character.Features;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items.Attachments.Interfaces;
using Awaken.TG.Main.Heroes.MovementSystems;
using Awaken.TG.Main.Locations.Mobs;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.Main.UI.Stickers;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.States;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Awaken.TG.Graphics.Cutscenes {
    [SpawnsView(typeof(VCutsceneSkipPauseHandler), false)]
    public partial class Cutscene : Model, IUIStateSource, ICustomClothesOwner {
        const int CutsceneCameraLowestPriority = -999;
        
        public override Domain DefaultDomain => Domain.Gameplay;
        public sealed override bool IsNotSaved => true;

        public UIState UIState { get; }

        public bool IsTriggeringPortalOnExit { get; }
        bool _heroTeleported;
        float _toBlackDuration;
        float _toBlackAtEndDuration;
        float _blackAtEndDuration;
        
        // === ICutsceneClothesOwner
        public Transform HeadSocket => View<VCutsceneFPP>().HeadSocket;
        public Transform MainHandSocket => View<VCutsceneFPP>().MainHandSocket;
        public Transform OffHandSocket => View<VCutsceneFPP>().OffHandSocket;
        public Transform HipsSocket => View<VCutsceneFPP>().RootSocket;
        public Transform RootSocket => View<VCutsceneFPP>().RootSocket;
        
        public uint? LightRenderLayerMask => null;
        public int? WeaponLayer => null;
        public bool AllowSkip { get; }
        public bool TakeAwayControl { get; }
        public bool Stopped { get; private set; } = true;

        // === References
        StepResult Result { get; }
        CutsceneTemplate CutsceneTemplate { get; }

        ARAsyncOperationHandle<GameObject> _cutsceneViewHandle;
        
        // === Events
        public new static class Events {
            [UnityEngine.Scripting.Preserve] public static readonly Event<Hero, Cutscene> CutsceneStarted = new(nameof(CutsceneStarted));
        }

        // === Constructors
        public Cutscene(CutsceneTemplate cutsceneTemplate, StepResult result, float toBlackDuration, bool isTriggeringPortalOnExit, bool takeAwayControl) {
            CutsceneTemplate = cutsceneTemplate;
            AllowSkip = cutsceneTemplate.allowSkip;
            Result = result;
            _toBlackDuration = toBlackDuration;
            IsTriggeringPortalOnExit = isTriggeringPortalOnExit;
            TakeAwayControl = takeAwayControl;
            UIState = takeAwayControl ? UIState.ModalState(HUDState.EverythingHidden).WithCursorHidden() : UIState.TransparentState;
        }

        // === Initialization
        protected override void OnInitialize() {
            if (_toBlackDuration >= 0) {
                World.Services.Get<TransitionService>().ToBlack(_toBlackDuration).Forget();
            }

            Services.Get<MapStickerUI>().SetActive(false);
            
            Hero hero = Hero.Current;
            if (hero.MainViewInitialized) {
                Init(hero.VHeroController, false).Forget();
            } else {
                hero.ListenTo(Hero.Events.MainViewInitialized, h => Init(h, true).Forget());
            }
        }

        async UniTaskVoid Init(VHeroController vHeroController, bool delayInit) {
            if (delayInit) {
                bool success = await AsyncUtil.DelayFrame(this);
                if (!success) return;
            }
            vHeroController.Hide();
            if (TakeAwayControl) {
                vHeroController.Target.TrySetMovementType<CutsceneMovement>();
            }
            BodyFeatures bodyFeatures = AddElement(new BodyFeatures());
            bodyFeatures.CopyFrom(vHeroController.Target.BodyFeatures());
            bodyFeatures.RefreshDistanceBand(0);
            SpawnView();
        }

        
        // === Cutscene End
        public void CutsceneStarted() {
            Stopped = false;
        }
        
        public void SkipCutscene() {
            View<VCutsceneBase>().SkipCutsceneWithTransition().Forget();
        }
        
        public void OnCutsceneEnd() {
            if (_heroTeleported) {
                return;
            }
            _heroTeleported = true;
            VHeroController heroController = Hero.Current.VHeroController;
            if (IsTriggeringPortalOnExit) { 
               heroController.Show(); 
               return;
            }
            RemoveElementsOfType<CustomHeroClothes>();
            if (TakeAwayControl) {
                heroController.Target.ReturnToDefaultMovement();
            }
            Transform cutsceneHero = View<VCutsceneBase>().TeleportHeroPosition;
            if (cutsceneHero != null) {
                heroController.Target.TeleportTo(cutsceneHero.position, Quaternion.Euler(0, cutsceneHero.eulerAngles.y, 0), () => OnHeroTeleported(heroController));
            } else {
                heroController.Show();
            }
        }

        void OnHeroTeleported(VHeroController heroController) {
            if (HasBeenDiscarded || View<VCutsceneBase>() == null) {
                if (heroController is { HasBeenDiscarded: false }) {
                    heroController.Show();
                }
                return;
            }
            View<VCutsceneBase>().CutsceneCamera.Priority = CutsceneCameraLowestPriority;
            heroController.Show();
        }

        // === Discarding
        protected override void OnDiscard(bool fromDomainDrop) {
            _cutsceneViewHandle.Release();
            Services.Get<MapStickerUI>().SetActive(true);
            OnCutsceneEnd();
            RestoreCameraSettings().Forget();
            if (!Result.IsDone) {
                Result.Complete();
            }
        }

        static async UniTaskVoid RestoreCameraSettings() {
            var gameCamera = World.Only<GameCamera>();
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            if (!gameCamera.HasBeenDiscarded) {
                gameCamera.RestoreDefaultPhysicalProperties();
            }
        }

        // === Helpers
        void SpawnView() {
            var viewRef = CutsceneTemplate.CutsceneView();
            _cutsceneViewHandle = viewRef.LoadAsset<GameObject>();
            _cutsceneViewHandle.OnComplete(h => {
                if (h.Status != AsyncOperationStatus.Succeeded) {
                    viewRef.ReleaseAsset();
                    return;
                }

                var instance = Object.Instantiate(h.Result);
                if (CutsceneTemplate.spawnPosition == CutsceneTemplate.SpawnPosition.Hero) {
                    instance.transform.SetPositionAndRotation(Hero.Current.Coords, Hero.Current.Rotation);
                }
                OnViewLoaded(instance);
            });
        }

        void OnViewLoaded(GameObject cutsceneView) {
            if (!WasDiscarded && cutsceneView != null) {
                cutsceneView.SetUnityRepresentation(new IWithUnityRepresentation.Options() {
                    linkedLifetime = true, movable = true,
                });
                World.BindView(this, cutsceneView.GetComponent<VCutsceneBase>(), true, true);
            } else {
                _cutsceneViewHandle.Release();
            }

            if (!CutsceneTemplate.stopsStory) {
                Result.Complete();
            }
        }
        
        // == ICustomClothesOwner
        public IInventory Inventory => Character!.Inventory;
        public ICharacter Character => Hero.Current;
        public IEquipTarget EquipTarget => this;
        public IBaseClothes<IItemOwner> Clothes => TryGetElement<CustomHeroClothes>();
        public IView BodyView => MainView;
    }
}