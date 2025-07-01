using Awaken.TG.MVC;
using Awaken.Utility;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Statuses.Duration {
    public partial class StatusDuration : DurationProxy<Status> {
        public override ushort TypeForSerialization => SavedModels.StatusDuration;

        public override IModel TimeModel => ParentModel.Character;
        public override bool CanEvaluateTime => Time.timeScale > 0;

        public static StatusDuration Create(Status status, IDuration duration) {
            duration = ApplyBuffDurationMultiplier(status, duration);
            return new StatusDuration(duration);
        }
        
        [JsonConstructor, UnityEngine.Scripting.Preserve] protected StatusDuration() { }
        protected StatusDuration(IDuration duration) : base(duration) { }
        
        public override void Prolong(IDuration duration) {
            duration = ApplyBuffDurationMultiplier(ParentModel, duration);
            base.Prolong(duration);
        }
        
        public override void Renew(IDuration duration) {
            duration = ApplyBuffDurationMultiplier(ParentModel, duration);
            base.Renew(duration);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            if (!ParentModel.HasBeenDiscarded) {
                ParentModel.ParentModel.Trigger(CharacterStatuses.Events.ExtinguishedStatus, ParentModel);
                ParentModel.Discard();
            }
        }

        protected static IDuration ApplyBuffDurationMultiplier(Status status, IDuration duration) {
            if (duration is TimeDuration timeDuration && status is { Character: { HasBeenDiscarded: false} character }) {
                var stats = character.CharacterStats;
                float multiplier = status.Type.IsPositive ? stats.BuffDuration : stats.DebuffDuration;
                return new TimeDuration(timeDuration.OriginalTime * multiplier, timeDuration.UnscaledTime);
            }
            return duration;
        }
    }
}