using System.Threading;
using Awaken.TG.Assets;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Fights.Mounts;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Settings.Accessibility;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.Main.Stories.Steps;
using Awaken.TG.Main.UI.Menu.DeathUI;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.MVC;
using Awaken.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Awaken.TG.Main.Heroes.MovementSystems {
    public partial class MountedMovement : HeroMovementSystem {
        public override ushort TypeForSerialization => SavedModels.MountedMovement;

        bool _originalPlayerPerspective;
        static CancellationTokenSource _perspectiveTokenSource;
        MountElement _mount;
        ARAsyncOperationHandle<ARHeroStateToAnimationMapping> _movementAnimationOverrides;
        
        [UnityEngine.Scripting.Preserve] public VMount MountView { get; private set; }
        public override MovementType Type => MovementType.Mounted;
        public override bool CanCurrentlyBeOverriden => false;
        public override bool RequirementsFulfilled => true;

        protected override void Init() {
            Hero.Trigger(Hero.Events.HideWeapons, true);
            Controller.ToggleCrouch(0, false);
            Controller.UpdateRotationConstraint(true);
        }
        
        public void AssignMount(MountElement mount) {
            _mount = mount;
            MountView = mount.View<VMount>();
            
            Hero.ListenTo(Hero.Events.HeroPerspectiveChanged, OnPerspectiveChanged, this);
            OnPerspectiveChanged(Hero.TppActive);
            
            SetHeroPerspective(World.Any<HorsePerspectiveSetting>()?.IsTPP ?? false).Forget();
        }

        void OnPerspectiveChanged(bool isTpp) {
            if (isTpp) {
                LoadAnimationOverrides();
            } else {
                UnloadAnimationOverrides();
            }
        }
        
        void LoadAnimationOverrides() {
            if (!_mount.MountedHeroOverridesRef.IsSet) {
                return;
            }
            
            _movementAnimationOverrides = _mount.MountedHeroOverridesRef.LoadAsset<ARHeroStateToAnimationMapping>();
            _movementAnimationOverrides.OnCompleteForceAsync(OnAnimationOverridesLoaded);
        }
        
        void OnAnimationOverridesLoaded(ARAsyncOperationHandle<ARHeroStateToAnimationMapping> handle) {
            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null || handle.IsCancelled) {
                handle.Release();
                return;
            }
            ParentModel.VHeroController.Animancer.ApplyOverrides(this, handle.Result);
            ParentModel.TryGetElement<LegsFSM>()?.SetCurrentState(HeroStateType.Movement, 0.0f);
        }
        
        void UnloadAnimationOverrides() {
            if (_movementAnimationOverrides.IsValid() && _movementAnimationOverrides.Result != null) {
                ParentModel.VHeroController.Animancer.RemoveOverrides(this, _movementAnimationOverrides.Result);
            }
            _movementAnimationOverrides.Release();
            _movementAnimationOverrides = default;
        }

        public override void Update(float deltaTime) { }

        public override void FixedUpdate(float deltaTime) { }

        protected override void SetupForceExitConditions() {
            Hero.ListenTo(SCloseHeroEyes.Events.EyesClosed, _ => Dismount(), this);
            Hero.ListenTo(Hero.Events.Revived, _ => Dismount(), this);
            Hero.ListenTo(VJailUI.Events.GoingToJail, _ => Dismount(), this);
        }

        public void Dismount() {
            _mount.Dismount();
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            UnloadAnimationOverrides();
            SetHeroPerspective(World.Any<PerspectiveSetting>()?.IsTPP ?? false).Forget();
            Controller.UpdateRotationConstraint(false);
        }
        
        public static async UniTaskVoid SetHeroPerspective(bool isTPP) {
            _perspectiveTokenSource?.Cancel();
            _perspectiveTokenSource = new CancellationTokenSource();

            if (Hero.TppActive == isTPP) {
                return;
            }
            
            bool success = false;
            while (!success) {
                success = await Hero.Current.VHeroController.ChangeHeroPerspective(isTPP);
                if (!success) {
                    (bool isCanceled, bool result) = await AsyncUtil.DelayFrame(Hero.Current)
                        .AttachExternalCancellation(_perspectiveTokenSource.Token)
                        .SuppressCancellationThrow();
                    if (isCanceled || !result) {
                        return;
                    }
                }
            }
        }
    }
}