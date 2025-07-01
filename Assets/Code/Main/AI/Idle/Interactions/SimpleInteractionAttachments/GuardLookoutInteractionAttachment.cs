using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Saving.Models;
using Awaken.TG.Main.Templates.Attachments;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Interactions.SimpleInteractionAttachments {
    [AttachesTo(typeof(SimpleInteractionBase), AttachmentCategory.Common, "Improves the NPC sight distance during the Interaction")]
    public class GuardLookoutInteractionAttachment : SimpleInteractionAttachment {
        [SerializeField, InfoBox("Increases View Range of the NPCs during Interaction")]
        float sightLengthMultiplier = 1.5f;
        [SerializeField, InfoBox("Increases how fast Alert is increased during Interaction")]
        float sightPowerMultiplier = 1f;
        
        StatTweak _lengthTweak;
        StatTweak _powerTweak;

        public override void OnStarted(NpcElement npc) {
            if (sightLengthMultiplier != 1f) {
                _lengthTweak = StatTweak.Multi(npc.Stat(NpcStatType.SightLengthMultiplier), sightLengthMultiplier, null, npc);
                _lengthTweak.MarkedNotSaved = true;
            }
            if (sightPowerMultiplier != 1f) {
                _powerTweak = StatTweak.Multi(npc.Stat(NpcStatType.Sight), sightPowerMultiplier, null, npc);
                _powerTweak.MarkedNotSaved = true;
            }
        }

        public override void OnEnded(NpcElement npc) {
            _powerTweak?.Discard();
            _powerTweak = null;
            _lengthTweak?.Discard();
            _lengthTweak = null;
        }
    }
}