using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    public class PersistentAoEWithVisualsAttachment : PersistentAoEAttachment {
        [SerializeField, FoldoutGroup(AdvancedGroupName), ShowIf(nameof(discardParentOnEnd))] 
        public float discardParentOnEndDelay = 5.0f;
        
        public override Element SpawnElement() {
            float? tick = UsesTick ? tickInterval : null;
            IDuration duration = persistent ? new UntilDiscarded() : new TimeDuration(lifeTime);
            return new PersistentAoEWithVisuals(tick, duration, StatusTemplate, buildupStrength, null, GetDamageParameters(), onlyOnGrounded, isRemovingOther, isRemovable, canApplyToSelf, discardParentOnEnd, discardOnOwnerDeath);
        }
        
        public override bool IsMine(Element element) {
            return element is PersistentAoEWithVisuals;
        }
    }
}
