using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Relations;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    public partial class SarrasSickle : Tool {
        public override ushort TypeForSerialization => SavedModels.SarrasSickle;

        [Saved] float _charges;
        IEventListener _ownerDealtDamageListener;

        public int Charges => Mathf.FloorToInt(_charges);
        public int MaxCharges { get; private set; }
        public float ChargeIncrementPerKill { get; private set; }
        public float ChargeProgress => _charges % 1;
        public override bool CanBeUsed => Charges > 0;

        public new static class Events {
            public static readonly Event<SarrasSickle, SarrasSickle> SickleStateUpdated =
                new(nameof(SickleStateUpdated));
        }

        public override void InitFromAttachment(ToolAttachment spec, bool isRestored) {
            base.InitFromAttachment(spec, isRestored);
            if (spec is SarrasSickleAttachment sickleSpec) {
                if (!isRestored) {
                    _charges = sickleSpec.initialCharges;
                }

                MaxCharges = sickleSpec.maxCharges;
                ChargeIncrementPerKill = sickleSpec.chargeIncrementPerKill;
            }
        }

        protected override void OnInitialize() {
            if (ParentModel.Owner is ICharacter character) {
                AssignOwnerDealtDamageListener(character);
            }
            ParentModel.ListenTo(IItemOwner.Relations.OwnedBy.Events.AfterAttached, AfterOwnerAdded, this);
            World.EventSystem.ListenTo(EventSelector.AnySource, IAlive.Events.AfterDeath, this, OnNpcDeath);
        }

        void AfterOwnerAdded(RelationEventData data) {
            if (data.to is ICharacter character) {
                AssignOwnerDealtDamageListener(character);
            }
        }

        void AssignOwnerDealtDamageListener(ICharacter character) {
            World.EventSystem.TryDisposeListener(ref _ownerDealtDamageListener);
            _ownerDealtDamageListener = character.ListenTo(HealthElement.Events.OnKill, outcome => {
                if (outcome.Damage.Item == ParentModel && outcome.Damage.Type == DamageType.Interact) {
                    UseCharge();
                }
            }, this);
        }

        void OnNpcDeath(DamageOutcome outcome) {
            if (outcome.TargetPure is NpcElement {WasLastDamageFromHero: true}) {
                IncrementCharge(ChargeIncrementPerKill);
            }
        }

        public void UseCharge() {
            _charges -= 1;
            _charges = math.max(0, _charges);
            this.Trigger(Events.SickleStateUpdated, this);
        }

        public void IncrementCharge(float amount) {
            _charges += amount;
            _charges = math.min(_charges, 0.99f + MaxCharges);
            this.Trigger(Events.SickleStateUpdated, this);
        }
    }
}