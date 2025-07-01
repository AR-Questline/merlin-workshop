using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Statuses.Attachments;
using Awaken.TG.Main.Heroes.Statuses.BuildUp;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Heroes.Statuses {
    /// <summary>
    /// Generic statuses holder for any ICharacter
    /// </summary>
    public sealed partial class CharacterStatuses : Element<ICharacter> {
        public override ushort TypeForSerialization => SavedModels.CharacterStatuses;

        // === Properties
        public ModelsSet<Status> AllStatuses => Elements<Status>();
        readonly List<BuildupStatus> _statusBuildups = new();
        
        IEnumerable<TemplateReference> InvulnerableToStatuses => ParentModel.StatusStats.InvulnerableToStatuses;

        public new static class Events {
            public static readonly Event<CharacterStatuses, Status> AddedStatus = new(nameof(AddedStatus));
            public static readonly Event<CharacterStatuses, Status> RemovedStatus = new(nameof(RemovedStatus));
            public static readonly Event<CharacterStatuses, Status> ExtinguishedStatus = new(nameof(ExtinguishedStatus));
            public static readonly Event<CharacterStatuses, Status> VanishedStatus = new(nameof(VanishedStatus));
        }

        // === Initialization
        protected override void OnInitialize() {
            if (ParentModel is NpcElement npc) {
                npc.ParentModel.ListenTo(NpcElement.Events.BeforeNpcOutOfVisualBand, RemoveAllTimeBasedStatuses, this);
            }
            ParentModel.ListenTo(Model.Events.AfterChanged, TriggerChange, this);
            ParentModel.GetOrCreateTimeDependent().WithUpdate(Update).ThatDoesNotProcessWhenPause();
        }

        void Update(float dt) {
            for (int i = _statusBuildups.Count - 1; i >= 0; i--) {
                _statusBuildups[i].Decay(dt);
            }
        }

        // === Operations

        public AddResult BuildupStatus(float buildupStrength, StatusTemplate statusTemplate, StatusSourceInfo sourceInfo) {
            if (!statusTemplate.IsBuildupAble) {
                throw new NotSupportedException();
            }

            var buildupData = statusTemplate.GetComponent<BuildupAttachment>();
            var buildup = _statusBuildups.FirstOrDefault(b => b.BuildupStatusStatusType == buildupData.BuildupStatusType);
            
            // Handling missing buildup tracker
            if (buildup == null) {
                BuildUp.BuildupStatus.CreateBuildupStatus(buildupData, this, statusTemplate, sourceInfo, buildupStrength);
                return new AddResult {type = StatusAddType.Add};
            }

            buildup.Buildup(buildupStrength);
            return new AddResult {type = StatusAddType.None};
        }

        public void AddBuildupStatus(BuildupStatus buildup) {
            _statusBuildups.Add(buildup);
            this.Trigger(Events.AddedStatus, buildup);
            if (buildup is { HasBeenDiscarded: false }) {
                buildup.ListenTo(Model.Events.BeforeDiscarded, b => _statusBuildups.Remove(b as BuildupStatus), this);
            } else {
                _statusBuildups.Remove(buildup);
            }
        }
        
        public AddResult AddStatus(StatusTemplate statusTemplate, [NotNull] StatusSourceInfo sourceInfo, 
            IDuration duration = null, SkillVariablesOverride variableOverride = null) {
            if (!statusTemplate.CanBeApplied) {
                return new AddResult {type = StatusAddType.None};
            }

            foreach (var statusRef in InvulnerableToStatuses) {
                var invStatus = statusRef.Get<StatusTemplate>();
                if (invStatus.IsAbstract) {
                    if (statusTemplate.InheritsFrom(invStatus)) {
                        return new AddResult { type = StatusAddType.None };
                    }
                } else {
                    if (string.Equals(statusRef.GUID, statusTemplate.GUID, StringComparison.InvariantCulture)) {
                        ParentModel.Trigger(ICharacter.Events.TriedToApplyInvulnerableStatus, sourceInfo.SourceCharacter.Get());
                        return new AddResult { type = StatusAddType.None };
                    }
                }
            }

            if (statusTemplate.StatusType == StatusType.Blessing) {
                RemoveAllStatusesOfType(StatusType.Blessing);
            }
            
            if (statusTemplate.StatusType == StatusType.Curse) {
                RemoveAllStatusesOfType(StatusType.Curse);
            }
            
            var addType = ChooseAddType(statusTemplate);
            duration = AdjustDuration(statusTemplate, duration, statusTemplate.StatusType);
            return ResultFromType(statusTemplate, duration, variableOverride, addType, sourceInfo);
        }

        IDuration AdjustDuration(StatusTemplate statusTemplate, IDuration duration, StatusType statusType) {
            duration ??= statusTemplate.TryGetDuration();

            if (duration is TimeDuration timeDuration) {
                float durationModifier = 1f;
                if (!statusType.IsPositive) {
                    durationModifier *= 1 - ParentModel.AliveStats.StatusResistance;
                }
                if (durationModifier != 1f) {
                    duration = new TimeDuration(timeDuration.TimeLeft * durationModifier, timeDuration.UnscaledTime);
                }
            } else if (!statusType.IsPositive) {
                duration?.ReduceTime(ParentModel.AliveStats.StatusResistance);
            }

            return duration;
        }

        AddResult ResultFromType(StatusTemplate statusTemplate, IDuration duration, SkillVariablesOverride variableOverride, StatusAddType addType, StatusSourceInfo sourceInfo) {
            if (statusTemplate.OverrideToAddForDifferentItems && sourceInfo != null && sourceInfo.SourceItem.TryGet(out var sourceItem)) {
                if (!AllStatuses.Any(s => s.SourceInfo?.SourceItem.Get()?.Template.GUID == sourceItem.Template.GUID)) {
                    addType = StatusAddType.Add;
                }
            }
            
            switch (addType) {
                case StatusAddType.Add: {
                    var status = new Status(statusTemplate, sourceInfo, variableOverride);
                    AddNewStatus(status, duration);
                    return new AddResult {type = addType, oldStatus = null, newStatus = status};
                }
                case StatusAddType.Upgrade: {
                    if (variableOverride != null) {
                        Log.Important?.Error("Cannot apply overrides for status that is upgradable", statusTemplate);
                    }
                    
                    var oldStatus = FirstFrom(statusTemplate);
                    oldStatus.Discard();
                    var newStatus = AddStatus(statusTemplate.UpgradeReference, sourceInfo).newStatus;
                    return new AddResult {type = addType, oldStatus = oldStatus, newStatus = newStatus};
                }
                case StatusAddType.Renew: {
                    if (variableOverride != null) {
                        Log.Important?.Error("Cannot apply overrides for status that is renewable", statusTemplate);
                    }

                    var statusToRenew = FirstFrom(statusTemplate);
                    statusToRenew.Renew(duration);
                    return new AddResult {type = addType, oldStatus = statusToRenew, newStatus = statusToRenew};
                }
                case StatusAddType.Prolong: {
                    if (variableOverride != null) {
                        Log.Important?.Error("Cannot apply overrides for status that is prolongable", statusTemplate);
                    }

                    var oldStatus = FirstFrom(statusTemplate);
                    oldStatus.Prolong(duration);
                    return new AddResult {type = addType, oldStatus = oldStatus, newStatus = oldStatus};
                }
                case StatusAddType.AddAndProlong: {
                    var status = new Status(statusTemplate, sourceInfo, variableOverride);
                    AddNewStatus(status, duration);

                    var oldStatuses = AllStatuses.Where(s => s.Template == statusTemplate);
                    foreach (Status s in oldStatuses) {
                        s.Prolong(duration);
                    }
                    return new AddResult {type = addType, oldStatus = null, newStatus = status};
                }
                case StatusAddType.AddAndRenew: {
                    var status = new Status(statusTemplate, sourceInfo, variableOverride);
                    AddNewStatus(status, duration);

                    var oldStatuses = AllStatuses.Where(s => s.Template == statusTemplate);
                    foreach (Status s in oldStatuses) {
                        s.Renew(duration);
                    }
                    return new AddResult {type = addType, oldStatus = null, newStatus = status};
                }
                case StatusAddType.Replace: {
                    var oldStatus = FirstFrom(statusTemplate);
                    oldStatus.Discard();
                    
                    var newStatus = new Status(statusTemplate, sourceInfo, variableOverride);
                    AddNewStatus(newStatus, duration);
                    return new AddResult {type = addType, oldStatus = oldStatus, newStatus = newStatus};
                }
                case StatusAddType.Stack: {
                    var oldStatus = FirstFrom(statusTemplate);
                    oldStatus.IncreaseStack();
                    
                    AddResult addResult = ResultFromType(statusTemplate, duration, variableOverride, statusTemplate.AddTypeOnStacking, sourceInfo);
                    addResult.oldStatus = oldStatus;
                    return addResult;
                }
                case StatusAddType.None:
                    return new AddResult {type = addType};
                default:
                    throw new Exception($"There is no implementation for AddType({addType})");
            }
        }

        [UnityEngine.Scripting.Preserve] public bool HasStatus(StatusType statusType) => AllStatuses.Any(s => s.Type == statusType);
        public bool HasStatus(StatusTemplate template) => AllStatuses.Any(s => s.Template == template);

        public void RemoveStatus(StatusTemplate statusTemplate) {
            var status = AllStatuses.FirstOrDefault(s => s.Template == statusTemplate);
            RemoveStatus(status);
        }
        public void RemoveStatus(Status status) {
            if (status is { HasBeenDiscarded: false } && status.GenericParentModel == this) {
                status.Discard();
                this.Trigger(Events.RemovedStatus, status);
                TriggerChange();
            }
        }

        public void RemoveAllStatus(StatusTemplate statusTemplate) {
            var statuses = AllStatuses.Where(s => s.Template == statusTemplate).ToArray();
            if (statuses.Length > 0) {
                foreach (var status in statuses) {
                    status.Discard();
                    this.Trigger(Events.RemovedStatus, status);
                }
                TriggerChange();
            }
        }

        public void RemoveAllStatusesOfType(StatusType type) {
            var statuses = AllStatuses.Where(s => s.Type == type).ToArray();
            if (statuses.Length > 0) {
                foreach (var status in statuses) {
                    status.Discard();
                    this.Trigger(Events.RemovedStatus, status);
                }
                TriggerChange();
            }
        }

        public void RemoveAllNegativeStatuses() {
            var statuses = AllStatuses.Where(s => s.Type == StatusType.Curse || s.Type == StatusType.Sin || s.Type == StatusType.Debuff).ToArray();
            if (statuses.Length > 0) {
                foreach (var status in statuses) {
                    status.Discard();
                    this.Trigger(Events.RemovedStatus, status);
                }
                TriggerChange();
            }
        }

        void RemoveAllTimeBasedStatuses() {
            bool anyRemoved = false;
            var statuses = AllStatuses.Where(s => s.TimeLeftSeconds.HasValue && !float.IsPositiveInfinity(s.TimeLeftSeconds.Value)).ToArray();
            foreach (var status in statuses) {
                status.Discard();
                this.Trigger(Events.RemovedStatus, status);
                anyRemoved = true;
            }
            for (int i = _statusBuildups.Count - 1; i >= 0; i--) {
                _statusBuildups[i].Discard();
                anyRemoved = true;
            }
            if (anyRemoved) {
                TriggerChange();
            }
        }

        // === Queries
        Status FirstFrom(StatusTemplate statusTemplate) => AllStatuses.FirstOrDefault(s => s is not BuildUp.BuildupStatus && s.Template == statusTemplate);

        // === Status creation
        StatusAddType ChooseAddType(StatusTemplate statusTemplate) {
            var oldStatus = FirstFrom(statusTemplate);
            return oldStatus == null ? StatusAddType.Add : statusTemplate.AddType;
        }

        public void AddNewStatus(Status status, IDuration duration) {
            AddElement(status);
            if (duration != null) {
                status.AttachDuration(duration);
            }
            this.Trigger(Events.AddedStatus, status);
        }
        
        public struct AddResult {
            public StatusAddType type;
            public Status oldStatus;
            public Status newStatus;

            public override string ToString() {
                return type switch {
                    StatusAddType.Add => $"added status: {newStatus.DisplayName}",
                    StatusAddType.Upgrade => $"upgraded status {oldStatus.DisplayName} to {newStatus.DisplayName}",
                    StatusAddType.Renew => $"renewed status: {newStatus.DisplayName}",
                    StatusAddType.Prolong => $"prolong status: {newStatus.DisplayName}",
                    StatusAddType.AddAndProlong => $"added status: {newStatus.DisplayName} and prolong all matching",
                    StatusAddType.AddAndRenew => $"added status: {newStatus.DisplayName} and renewed all matching",
                    StatusAddType.Replace => $"replace status {oldStatus.DisplayName} with {newStatus.DisplayName}",
                    StatusAddType.Stack => $"status {oldStatus.DisplayName} stack level was increased",
                    StatusAddType.None => "No status operation was run",
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
        }
    }
}