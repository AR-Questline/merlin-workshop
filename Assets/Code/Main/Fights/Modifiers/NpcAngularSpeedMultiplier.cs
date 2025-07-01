using System.Collections.Generic;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Main.Fights.Modifiers {
    public partial class NpcAngularSpeedMultiplier : Element<NpcElement>, IWithDuration {
        public sealed override bool IsNotSaved => true;
        public IModel TimeModel => ParentModel;

        readonly Dictionary<IDuration, float> _multiplierRecords;
        public float Multiplier { get; private set; }

        // === Constructor
        NpcAngularSpeedMultiplier() {
            _multiplierRecords = new Dictionary<IDuration, float>(2);
            Multiplier = 1.0f;
        }
        
        // === Public API
        public static void AddAngularSpeedMultiplier(NpcElement npcElement, float multiplier, IDuration duration) {
            var currentMultiplier = npcElement.AngularSpeedMultiplier;
            if (currentMultiplier == null) {
                currentMultiplier = new NpcAngularSpeedMultiplier();
                npcElement.AddElement(currentMultiplier);
                npcElement.AngularSpeedMultiplier = currentMultiplier;
            }
            
            multiplier = Mathf.Clamp(multiplier, 0, 1);
            currentMultiplier.AddMultiplierWithDuration(multiplier, duration);
        }
        
        // === Helpers
        void AddMultiplierWithDuration(float multiplier, IDuration duration) {
            Multiplier *= multiplier;
            AddElement(duration);
            duration.ListenTo(Events.AfterDiscarded, () => OnUsedDurationDiscarded(duration), this);
        }

        void OnUsedDurationDiscarded(IDuration duration) {
            if (IsBeingDiscarded) {
                return;
            }
            
            _multiplierRecords.Remove(duration);
            if (_multiplierRecords.IsEmpty()) {
                Discard();
                return;
            }
            RecalculateMultiplier();
        }

        void RecalculateMultiplier() {
            Multiplier = 1.0f;
            foreach (var entry in _multiplierRecords) {
                Multiplier *= entry.Value;
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            _multiplierRecords.Clear();
            ParentModel.AngularSpeedMultiplier = null;
            base.OnDiscard(fromDomainDrop);
        }
    }
}