using System;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.Main.Utility.Animations.Gestures;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Attachments {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Common, "Adds dialogue to the location.")]
    public class DialogueAttachment : MonoBehaviour, IAttachmentSpec {
        [LocStringCategory(Category.Interaction)]
        public LocString customDialogueLabel;
        public StoryBookmark bookmark;
        [LabelText("Gesture Overrides"), PropertySpace(spaceBefore: 10, spaceAfter: 10)]
        public GesturesSerializedWrapper gesturesWrapper;
        
        [Tooltip("If not set, ViewFocus will be set to first child of spec with name containing \"head\"")]
        public Transform viewFocus;

        [Tooltip("The distance at which the story will be force quit if not hero involved")]
        [SerializeField]
#if UNITY_EDITOR
        [DisableIf(nameof(EDITOR_IsHeroInvolved)), InfoBox("Hero is involved, disabled", InfoMessageType.Info, visibleIfMemberName: nameof(EDITOR_IsHeroInvolved))]
#endif
        HeroRequiredDialogueDistance endStoryDistance = HeroRequiredDialogueDistance.Short;

        public float EndStoryDistanceSqr => DialogueDistanceSqr(endStoryDistance);

        public static float DialogueDistanceSqr(HeroRequiredDialogueDistance distance) => distance switch {
            HeroRequiredDialogueDistance.VeryShort => 2 * 2,
            HeroRequiredDialogueDistance.Short => 5 * 5,
            HeroRequiredDialogueDistance.Medium => 10 * 10,
            HeroRequiredDialogueDistance.Long => 15 * 15,
            HeroRequiredDialogueDistance.Never => -1,
            _ => throw new ArgumentOutOfRangeException()
        };
        
        public Element SpawnElement() {
            return new DialogueAction();
        }

        public bool IsMine(Element element) {
            return element is DialogueAction;
        }

#if UNITY_EDITOR
        bool EDITOR_IsHeroInvolved() {
            return bookmark != null && bookmark.IsValid && bookmark.EDITOR_Graph.Settings(bookmark).InvolveHero;
        }
#endif
    }

    public enum HeroRequiredDialogueDistance : byte {
        VeryShort,
        Short,
        Medium,
        Long,
        Never
    }
}