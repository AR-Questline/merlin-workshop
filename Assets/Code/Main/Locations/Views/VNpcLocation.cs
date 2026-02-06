using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Grounds.CullingGroupSystem;
using Awaken.TG.Main.Grounds.CullingGroupSystem.CullingGroups;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Events;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;

namespace Awaken.TG.Main.Locations.Views {
    [UsesPrefab("Locations/VNpcLocation")]
    public class VNpcLocation : VDynamicLocation {
        bool _initialized;
        bool _ignoreInitDelay;
        bool _suspended;
        IEventListener _bandChangedListener;

        NpcElement _npcElement;
        NpcElement NpcElement => Target.TryGetCachedElement(ref _npcElement);

        int CurrentDistanceBand => math.min(NpcElement?.CurrentDistanceBand ?? LocationCullingGroup.LastBand,
            Target.GetCurrentBandSafe(LocationCullingGroup.LastBand));

        protected override void OnInitialize() {
            Target.AfterFullyInitialized(() => {
                Target.MoveAndRotateTo(Target.SavedCoords, Target.SavedRotation);
                WaitForCullingSystem().Forget();
            }, this);
        }

        async UniTaskVoid WaitForCullingSystem() {
            if (!await AsyncUtil.DelayFrameOrTime(gameObject, 3, 150)) {
                if (!_initialized && !HasBeenDiscarded) {
                    Target.VisualLoadingFailed();
                }
                return;
            }
            _ignoreInitDelay = true;
            
            if (_initialized) {
                return;
            }

            bool success = World.Services.TryGet<CullingSystem>() != null ||
                           await AsyncUtil.WaitUntil(this, () => World.Services.TryGet<CullingSystem>() != null);
            if (HasBeenDiscarded) {
                return;
            }
            
            if (_initialized) {
                return;
            }

            if (!success) {
                Target.VisualLoadingFailed();
                return;
            }

            if (NpcElement == null) {
                Initialize();
                return;
            }
            
            if (!CheckInDistanceBand()) {
                _bandChangedListener = Target.ListenTo(ICullingSystemRegistreeModel.Events.DistanceBandChanged, _ => CheckInDistanceBand(), this);
                Target.VisualLoadingSuspended();
                _suspended = true;
            }
        }

        void Initialize() {
            if (_initialized) {
                return;
            }

            if (_suspended) {
                Target.ContinueVisualLoading();
            }

            OnInitializedAsync(_ignoreInitDelay).Forget();
            _suspended = false;
            _initialized = true;
        }
        
        public bool CheckInDistanceBand() {
            bool visible = LocationCullingGroup.InNpcVisibilityBand(CurrentDistanceBand);
            if (visible) {
                Initialize();
                World.EventSystem.TryDisposeListener(ref _bandChangedListener);
            }
            return visible;
        }
    }
}