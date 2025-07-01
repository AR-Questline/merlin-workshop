using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Attachments.Audio {
    [AttachesTo(typeof(ItemTemplate), AttachmentCategory.Common, "Used to override audio for items.")]
    public class ItemAudioAttachment : MonoBehaviour, IAttachmentSpec {
        [ShowIf(nameof(ShowArmorSurfaceType)), RichEnumExtends(typeof(SurfaceType)), SerializeField]
        RichEnumReference armorSurfaceType = SurfaceType.ArmorFabric;
        [InlineProperty, LabelWidth(90)]
        public ItemAudioContainerWrapper itemAudioContainerWrapper;

        public SurfaceType ArmorSurfaceType => armorSurfaceType.EnumAs<SurfaceType>();

        bool ShowArmorSurfaceType => itemAudioContainerWrapper.Data?.audioType.HasFlagFast(ItemAudioContainer.AudioType.Armor) ?? false;
        
        public Element SpawnElement() {
            return new ItemAudio();
        }
        
        public bool IsMine(Element element) {
            return element is ItemAudio;
        }
    }
}
