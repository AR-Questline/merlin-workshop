using System.Collections.Generic;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Skills;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Collections;
using Sirenix.OdinInspector;

namespace Awaken.TG.Main.Heroes.HUD {
    public abstract class StatusHUD : ViewComponent {
        [ShowInInspector, HideReferenceObjectPicker, ReadOnly]
        protected CharacterStatuses _statuses;

        [ShowInInspector, HideReferenceObjectPicker, ReadOnly]
        protected Dictionary<string, StatusSourceReferences> _statusVisuals = new();

        IEventListener _targetStatusListener;

        void UpdateStatusView(Element element) {
            if (element is not Status {HiddenOnHUD: false} status) return;
            
            if (_statusVisuals.TryGetValue(status.SourceInfo.SourceUniqueID, out var statusReferences)) {
                if (status.HasBeenDiscarded) {
                    statusReferences.StatusInstances.Remove(status);
                    if (statusReferences.StatusInstances.Count == 0) {
                        statusReferences.statusVisual.Discard();
                        _statusVisuals.Remove(status.SourceInfo.SourceUniqueID);
                    }
                } else {
                    statusReferences.StatusInstances.Add(status);
                    if (IsPriorityStatus(statusReferences, status)) {
                        statusReferences.statusVisual.AssignTargetStatus(status);
                    }
                }
                return;
            }

            var vhudStatus = World.SpawnView<VHUDStatus>(status.ParentModel, forcedParent: transform);
            _statusVisuals.Add(status.SourceInfo.SourceUniqueID, new StatusSourceReferences(vhudStatus) {StatusInstances = {status}});
            vhudStatus.Init(status.SourceInfo, status);
        }

        static bool IsPriorityStatus(StatusSourceReferences statusReferences, Status status) {
            if (statusReferences.statusVisual.TargetStatus?.Template.IsBuildupAble ?? false) {
                return true;
            }

            float statusLeftDuration = status.TimeLeftSeconds ?? -1f;
            float durationTimeLeft = statusReferences.statusVisual.Duration?.TimeLeft ?? 0;
            if (statusLeftDuration > durationTimeLeft) {
                return true;
            }

            return false;
        }

        protected void Init(ICharacter character) {
            Clear();

            _statuses = character.Statuses;
            _targetStatusListener = _statuses.ListenTo(Model.Events.AfterElementsCollectionModified, UpdateStatusView, this);

            foreach (Status activeStatus in _statuses.AllStatuses) {
                UpdateStatusView(activeStatus);
            }
        }

        protected void ReleaseListener() {
            if (_targetStatusListener != null) {
                World.EventSystem.RemoveListener(_targetStatusListener);
                _targetStatusListener = null;
            }
        }

        public void ClearViews() {
            _statusVisuals.ForEach(v => {
                if (!v.Value.statusVisual.HasBeenDiscarded) {
                    v.Value.statusVisual.Discard();
                }
            });
            _statusVisuals.Clear();
        }

        public void Clear() {
            ReleaseListener();
            ClearViews();
        }

        protected class StatusSourceReferences {
            public List<Status> StatusInstances { get; } = new();
            public readonly VHUDStatus statusVisual;

            public StatusSourceReferences(VHUDStatus statusVisual) {
                this.statusVisual = statusVisual;
            }
        }
    }
}