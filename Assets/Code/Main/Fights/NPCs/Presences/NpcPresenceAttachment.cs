using System.Linq;
using Awaken.TG.Graphics.Scene;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.Main.Utility.RichLabels;
using Awaken.TG.Main.Utility.RichLabels.SO;
using Awaken.TG.Main.Utility.Tags;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Fights.NPCs.Presences {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Technical, "Defines an NPC presence - a place in which NPC might appear.")]
    public class NpcPresenceAttachment : MonoBehaviour, IAttachmentSpec, IRichLabelUser {
        [InfoBox("Must be unique npc", InfoMessageType.Error, nameof(InvalidLocation))] 
        [SerializeField, TemplateType(typeof(LocationTemplate))] TemplateReference template;

        [SerializeField] RichLabelSet richLabelSet;
        [SerializeField, HideInInspector] bool manual;
        [SerializeField, HideInInspector] bool initialAvailability;
        [SerializeField, HideInInspector] bool forceTeleportIn = true;
        [SerializeField, HideInInspector] bool forceTeleportOut = true;

        [SerializeField, FoldoutGroup("Visibility"), BoxGroup("Visibility/Flag"), HideIf(nameof(manual)), HideLabel, PropertyOrder(1)] FlagLogic availability;

        [ShowInInspector, FoldoutGroup("Visibility"), PropertyOrder(0)] VisibilityChangedBy ChangedBy {
            get => manual ? VisibilityChangedBy.StorySteps : VisibilityChangedBy.Flag;
            set => manual = value == VisibilityChangedBy.StorySteps;
        }

        [ShowInInspector, FoldoutGroup("Visibility"), ShowIf(nameof(manual)), PropertyOrder(1)] InitialVisibility StartAs {
            get => initialAvailability ? InitialVisibility.Visible : InitialVisibility.Hidden;
            set => initialAvailability = value == InitialVisibility.Visible;
        }

        [ShowInInspector, FoldoutGroup("Visibility"), HideIf(nameof(manual)), PropertyOrder(2)] TeleportType OnBecomeVisible {
            get => forceTeleportIn ? TeleportType.Teleport : TeleportType.Walk;
            set => forceTeleportIn = value == TeleportType.Teleport;
        }

        [ShowInInspector, FoldoutGroup("Visibility"), HideIf(nameof(manual)), PropertyOrder(2)] TeleportType OnBecomeHidden {
            get => forceTeleportOut ? TeleportType.Teleport : TeleportType.Walk;
            set => forceTeleportOut = value == TeleportType.Teleport;
        }
        
        public LocationTemplate Template => template.Get<LocationTemplate>();
        public bool Manual => manual;
        public FlagLogic FlagAvailability => availability;
        public bool ForceTeleportIn => forceTeleportIn;
        public bool ForceTeleportOut => forceTeleportOut;
        public bool InitialManualAvailability => initialAvailability;
        public RichLabelSet RichLabelSet => richLabelSet;
        
        public void SetLocation(TemplateReference template) {
            this.template = template;
        }

        public Element SpawnElement() => new NpcPresence();
        public bool IsMine(Element element) => element is NpcPresence;

        bool InvalidLocation => UniqueNpcUtils.InvalidLocation(template, this);

        enum VisibilityChangedBy : byte {
            StorySteps,
            Flag,
        }

        enum InitialVisibility : byte {
            Visible,  
            Hidden
        }

        enum TeleportType : byte {
            Teleport,
            Walk,
        }
        
        public RichLabelConfigType RichLabelConfigType => RichLabelConfigType.Presence;
        public bool AutofillEnabled => true;
        
        public void Editor_Autofill() {
            richLabelSet.richLabelGuids.Clear();
            var locationTemplate = Template;
            if (locationTemplate) {
                UniqueNpcAttachment uniqueNpc = locationTemplate.GetComponent<UniqueNpcAttachment>();
                if (uniqueNpc.GetActor() != DefinedActor.None.ActorRef) {
                    if (uniqueNpc.GetActor().IsEmpty) {
                        Log.Minor?.Error($"Unique Npc Presence {gameObject.name} with no Actor! {gameObject.scene.name}: {gameObject.PathInSceneHierarchy()}");
                    } else {
                        richLabelSet.richLabelGuids.Add(uniqueNpc.GetActor().guid);
                    }
                } else {
                    Log.Minor?.Error($"Unique Npc Presence {gameObject.name} with Actor set up to None! {gameObject.scene.name}: {gameObject.PathInSceneHierarchy()}");
                }
            }

            // if (!gameObject.IsPrefab() && gameObject.scene.isLoaded) {
            //     SceneConfig sceneConfig = CommonReferences.Get.SceneConfigs.AllScenes.First(s => s.sceneName == gameObject.scene.name);
            //     richLabelSet.richLabelGuids.Add(sceneConfig.GUID);
            // }
        }
    }
}