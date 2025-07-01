using System;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;
using Awaken.CommonInterfaces;
using Awaken.TG.Assets;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Attachments;
using FMODUnity;
using Sirenix.OdinInspector;

namespace Awaken.TG.Main.Locations.Actions.Attachments {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Rare, "Adds a dig out action to the location.")]
    public class DigOutAttachment : MonoBehaviour, IAttachmentSpec, IDrakeRepresentationOptionsProvider {
        public Transform objectToMove;
        public GameObject[] objectsToEnableAfterDigging = Array.Empty<GameObject>();
        public GameObject[] objectToDisableAfterDigging = Array.Empty<GameObject>();
        public EventReference digUpSound;
        [ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.VFX)]
        public ShareableARAssetReference digUpVFX;

        [SerializeField, FoldoutGroup("Story"), TemplateType(typeof(StoryGraph))] 
        TemplateReference storyOnDugOut;
        
        public StoryBookmark StoryOnDugOut => StoryBookmark.ToInitialChapter(storyOnDugOut);
        public bool ProvideRepresentationOptions => true;

        public Element SpawnElement() {
            return new DigOutAction();
        }
        
        public bool IsMine(Element element) {
            return element is DigOutAction;
        }

        public IWithUnityRepresentation.Options GetRepresentationOptions() {
            return new IWithUnityRepresentation.Options() {
                movable = true,
                linkedLifetime = true,
            };
        }
    }
}