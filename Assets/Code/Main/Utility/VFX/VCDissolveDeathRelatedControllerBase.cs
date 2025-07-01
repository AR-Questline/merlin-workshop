using Awaken.TG.Main.Character;
using Awaken.TG.Main.Grounds.CullingGroupSystem;
using Awaken.TG.Main.Grounds.CullingGroupSystem.CullingGroups;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Utility.VFX {
    public abstract class VCDissolveDeathRelatedControllerBase<T> : VCDissolveControllerBase<T> {
        IEventListener _appearEventListener;

        protected override void OnAttach() {
            Target.OnVisualLoaded(OnVisualLoaded);
            if (Target.TryGetElement<TemporaryDeathElement>(out var temporaryDeathElement)) {
                temporaryDeathElement.ListenTo(TemporaryDeathElement.Events.TemporaryDeathStateChanged, dead => {
                    if (dead) {
                        StartDisappear().Forget();
                    } else {
                        Appear(transform.parent);
                    }
                }, this);
            }
            
            Target.TryGetElement<IAlive>()?.ListenTo(IAlive.Events.AfterDeath, _ => {
                _discardOnDisappeared = true;
                StartDisappear().Forget();
            }, this);
            
            // -- Summons
            Target.TryGetElement<DiscardParentAfterDuration>()?.ListenTo(DiscardParentAfterDuration.Events.DiscardingParent, DurationElapsed, this);
            
            base.OnAttach();
        }
        
        void OnVisualLoaded(Transform parentTransform) {
            int currentBand = Target.GetCurrentBandSafe(LocationCullingGroup.LastBand);
            if (LocationCullingGroup.InNpcVisibilityBand(currentBand)) {
                Appear(parentTransform);
            } else {
                _appearEventListener = Target.ListenTo(ICullingSystemRegistreeModel.Events.DistanceBandChanged,
                    band => OnBandChanged(band, parentTransform), this);
            }
        }
        
        void OnBandChanged(int band, Transform parentTransform) {
            if (LocationCullingGroup.InNpcVisibilityBand(band)) {
                Appear(parentTransform);
                World.EventSystem.DisposeListener(ref _appearEventListener);
            }
        }
        
        void DurationElapsed(HookResult<DiscardParentAfterDuration, Model> hook) {
            hook.Prevent();
            Disappear().Forget();
        }
    }
}