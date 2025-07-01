using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.AI.Combat.Attachments.Customs {
    public partial class StatusConsumption : Element<IStatusConsumptioner> {
        public sealed override bool IsNotSaved => true;

        readonly StatusTemplate _initialConsumedElemental;
        readonly float _buildupStrength;
        
        StatusTemplate _consumedStatus;
        StatusSourceInfo SourceInfo => StatusSourceInfo.FromStatus(_consumedStatus);

        public new static class Events {
            public static readonly Event<StatusConsumption, StatusTemplate> ConsumedStatus = new(nameof(ConsumedStatus));
        }
        
        public StatusConsumption(float buildupStrength) {
            _buildupStrength = buildupStrength;
        }
        
        protected override void OnInitialize() {
            ParentModel.StatusesOwner.ListenTo(CharacterStatuses.Events.AddedStatus, OnStatusApplied, this);
            ParentModel.StatusesOwner.ParentModel.ListenTo(HealthElement.Events.OnDamageDealt, OnDamageDealt, this);
        }

        void OnStatusApplied(Status status) {
            if (_consumedStatus == status.Template) {
                status.Discard();
                ParentModel.OnStatusDiscarded();
                return;
            }
            
            if (ConsumeStatus(status.Template)) {
                status.Discard();
            }
        }

        public bool ConsumeStatus(StatusTemplate statusTemplate) {
            if (ParentModel.CanConsume(statusTemplate)) {
                _consumedStatus = statusTemplate;
                this.Trigger(Events.ConsumedStatus, _consumedStatus);
                return true;
            }

            return false;
        }

        void OnDamageDealt(DamageOutcome outcome) {
            if (_consumedStatus != null && outcome.Target is { IsAlive: true } and ICharacter character) {
                VGUtils.ApplyStatus(character.Statuses, _consumedStatus, SourceInfo, _buildupStrength, null, null);
            }
        }
    }
}