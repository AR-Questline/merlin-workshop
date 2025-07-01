using Awaken.TG.Main.AI.Idle.Data.Runtime;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Data.Attachment {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Common, "Idle data setup for NPC - daily routine.")]
    public class IdleDataAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField] Type type;

        [SerializeField, ShowIf(nameof(IsEmbed))] IdleData data = IdleData.Default;

        [TemplateType(typeof(IdleDataTemplate))]
        [SerializeField, ShowIf(nameof(IsExplicit))] TemplateReference template;

        [SerializeField, HideLabel, BoxGroup("Fallback Interaction")] FallbackInteractionData fallbackInteractionData = FallbackInteractionData.Default;

        public bool IsExplicit => type == Type.Explicit;
        public bool IsEmbed => type == Type.Embed;
        
        IdleDataTemplate Template => template.Get<IdleDataTemplate>();
        public ref readonly IdleData EmbedData => ref data;
        
        public float PositionRange => IsEmbed ? data.positionRange : Template.PositionRange;
        public ref readonly IdleData Data => ref IsEmbed ? ref data : ref Template.Data;

        public FallbackInteractionData FallbackInteractionData => fallbackInteractionData;

        public Element SpawnElement() => new IdleDataElement();
        public bool IsMine(Element element) => element is IdleDataElement;

        enum Type {
            Embed,
            Explicit,
        }
    }
}