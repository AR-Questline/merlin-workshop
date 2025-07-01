using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Attachments {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Technical, "Updates location interactability based on Logic Emitter signal")]
    public class OnLogicStateChangedSetInteractabilityAttachment : MonoBehaviour, IAttachmentSpec {
        public bool changeOnEnable;
        [SerializeField, ShowIf(nameof(changeOnEnable)), RichEnumExtends(typeof(LocationInteractability))] public RichEnumReference onEnableInteractability = LocationInteractability.Active;
        [ShowIf(nameof(changeOnEnable))] public bool discardElementAfterEnable;
        public bool changeOnDisable;
        [SerializeField, ShowIf(nameof(changeOnDisable)), RichEnumExtends(typeof(LocationInteractability))] public RichEnumReference onDisableInteractability = LocationInteractability.Inactive;
        [ShowIf(nameof(changeOnDisable))] public bool discardElementAfterDisable;

        public LocationInteractability OnEnableInteractability => onEnableInteractability.EnumAs<LocationInteractability>();
        public LocationInteractability OnDisableInteractability => onDisableInteractability.EnumAs<LocationInteractability>();
        
        public Element SpawnElement() {
            return new OnLogicStateChangedSetInteractability();
        }

        public bool IsMine(Element element) {
            return element is OnLogicStateChangedSetInteractability;
        }
    }
}