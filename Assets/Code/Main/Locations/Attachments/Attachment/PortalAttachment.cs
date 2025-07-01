using System;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Debugging;
using Awaken.Utility;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Common, "Attaches portal to location, that allows travelling to another scene.")]
    public class PortalAttachment : MonoBehaviour, IAttachmentSpec {
        // === Serialized
        [InfoBox("@" + nameof(TypeInfo))]
        public PortalType type = PortalType.To;
        [HideIf(nameof(IsNoneType)), InfoBox("$"+nameof(ErrorMessage), InfoMessageType.Error, nameof(ShowErrorMessage))]
        public InteractionType interaction = InteractionType.OnTrigger;
        public Vector3 offset;

        public bool isSpawn;
        public bool isHiddenFromUI;
        public bool isLocationNameHidden;
        public bool doNotAutoSaveAfterPortaling;
        public NpcInteractability npcInteractability = NpcInteractability.Default;

        [HideIf(nameof(IsNoneType))]
        public SceneReference targetScene;
        [HideIf(nameof(IsNoneType))]
        public string indexTag;
        [ShowIf(nameof(IsProperInteraction)), FoldoutGroup("Interact Settings"), LocStringCategory(Category.Interaction)]
        public LocString customInteractLabel;

        public bool ShouldSetPitchOnExit;
        [Indent, ShowIf(nameof(ShouldSetPitchOnExit))] public bool useTransform = false;
        [DisableIf(nameof(useTransform))]
        [Indent, ShowIf(nameof(ShouldSetPitchOnExit))] public float PitchOnExit = -14;

        [Tooltip("Check this only on Test Arenas")]
        public bool debugFastPortal;
        
        [InfoBox("Default NpcInteractability checks if portal is HiddenFromUI, is TwoWay and is not Triggered from Story"), ShowInInspector]
        bool CanBeInteractedByAnNPC => Portal.CanNpcFindPortal(npcInteractability, isHiddenFromUI, type, interaction);
        
        // === Operations
        public Element SpawnElement() => new Portal();
        public bool IsMine(Element element) {
            return element is Portal;
        }
        
        // === Editor things
        string TypeInfo => type switch {
            PortalType.To => "TO: Tu gracz się pojawi (Arrival)",
            PortalType.From => "FROM: Stąd gracz zniknie (Departure)",
            PortalType.TwoWay => "Player can be both teleported from and to this portal",
            PortalType.None => "This portal is not active",
            PortalType.ScreenshotPosition => "This portal is used for screenshot position using the ss. command",
            _ => throw new ArgumentOutOfRangeException()
        };
        
        bool IsNoneType => type == PortalType.None;

        string ErrorMessage {
            get {
                if (!IsProperInteraction) {
                    return "For interaction portal you need to create Collider with IsTrigger set to true and on PlayerInteraction layer. " +
                           "This collider must be on child object";
                } else if (!IsProperTrigger) {
                    return "For trigger portal you need to create Collider with IsTrigger set to true and Default/TriggerVolume Layer";
                }
                return "";
            }
        }

        bool ShowErrorMessage => !IsProperInteraction || !IsProperTrigger;
        
        bool IsProperInteraction {
            get {
                if (interaction != InteractionType.OnInteract) {
                    return true;
                }

                Collider coll = gameObject.GetComponentsInChildren<Collider>()
                    .FirstOrDefault(c => c.gameObject != gameObject && c.isTrigger && c.gameObject.layer == RenderLayers.PlayerInteractions);
                return coll != null;
            }
        }

        bool IsProperTrigger {
            get {
                if (interaction != InteractionType.OnTrigger) {
                    return true;
                }

                Collider coll = gameObject.GetComponentsInChildren<Collider>()
                    .FirstOrDefault(c =>
                        c.isTrigger && c.gameObject.layer is RenderLayers.Default or RenderLayers.TriggerVolumes);
                return coll != null;
            }
        }

        [Button, ShowIf(nameof(CanDebugTravel))]
        void Travel() {
            string id = GetComponent<LocationSpec>().GetLocationId();
            Portal portal = World.All<Portal>().FirstOrDefault(p => p.ID.Contains(id));
            if (portal != null) {
                portal.Execute(Hero.Current);
            } else {
                Log.Important?.Error($"Failed to find Portal assigned to this PortalAttachment! Location Id: {id}");
            }
        }

        bool CanDebugTravel() => type is PortalType.From or PortalType.TwoWay or PortalType.ScreenshotPosition && World.HasAny<Hero>();

        void OnDrawGizmosSelected() {
            var _transform = transform;
            Vector3 pos = _transform.position + Vector3.up * 0.2f + offset;
            var origin1 = pos + _transform.right * 0.15f;
            var origin2 = pos - _transform.right * 0.15f;
            var origin3 = pos + _transform.forward;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(origin1, origin3);
            Gizmos.DrawLine(origin2, origin3);
            Gizmos.DrawLine(origin1, origin2);
        }
    }

    public enum PortalType {
        To = 0,
        From = 1,
        TwoWay = 2,
        None = 3,
        ScreenshotPosition = 5,
    }

    public enum InteractionType {
        OnTrigger = 0,
        OnInteract = 1,
        TriggerFromStory = 2,
    }

    public enum NpcInteractability {
        Default = 0,
        Allowed = 1,
        Forbidden = 2
    }
}