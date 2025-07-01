using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes.Tags;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.ExtraCustom, "Changes location visibility based on flag.")]
    public class MutableVisibilityAttachment : MonoBehaviour, IAttachmentSpec, ISelfValidator {
        const string BoxHeader = "Location Should Be Visible:";
        
        [SerializeField, Tags(TagsCategory.Flag)] string flag;
        [SerializeField, BoxGroup(BoxHeader)] bool ifNoFlag;
        [SerializeField, BoxGroup(BoxHeader)] bool ifFlagTrue;
        [SerializeField, BoxGroup(BoxHeader)] bool ifFlagFalse;

        public string Flag => flag;
        public bool IfNoFlag => ifNoFlag;
        public bool IfFlagTrue => ifFlagTrue;
        public bool IfFlagFalse => ifFlagFalse;
        
        public Element SpawnElement() {
            return new MutableVisibility();
        }

        public bool IsMine(Element element) {
            return element is MutableVisibility;
        }
        
        public void Validate(SelfValidationResult result) {
#if UNITY_EDITOR
            var spec = gameObject.GetComponent<LocationSpec>();
            if (spec == null) {
                if (gameObject.GetComponent<AttachmentGroup>()) {
                    spec = gameObject.GetComponentInParent<LocationSpec>();
                } else {
                    string error = "Location Spec is missing! Attachment require Location Specs.";
                    Log.Critical?.Error(error, this);
                    result.AddError(error);
                    return;
                }
            }
            
            if (spec.prefabReference is { IsSet: true }) {
                for (int i = 0; i < transform.childCount; i++) {
                    var child = transform.GetChild(i);
                    if (child == spec.EditorPrefabInstance.transform) {
                        continue;
                    }
                    if (!child.TryGetComponent(out AttachmentGroup _)) {
                        string error = "Location Spec with setup Prefab has hardcoded children! They won't hide." +
                                       "Move them to the prefab that is used as prefabReference in Location Spec." +
                                       "or remove the prefab from prefabReference in Location Spec and place it directly as hardcoded children as well.";
                        Log.Critical?.Error(error, this);
                        result.AddError(error);
                        break;
                    }
                }
                return;
            }

            if (!spec.IsHidableStatic) {
                string error = "Location Spec is not set as Hidable Static! Click the toggle at the Location Spec component";
                Log.Critical?.Error(error, this);
                result.AddError(error);
            }
#endif
        }
    }
}