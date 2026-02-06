using System;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Technical, "Animates drake material on Alive Location death.")]
    public class AliveLocationDeathAnimationsAttachment : MonoBehaviour, IAttachmentSpec {
        public bool animateDrakeMaterial;
        [ShowIf(nameof(animateDrakeMaterial))] public bool reverseDirection;
        public bool stopLights;
        public bool modifyVFXes;
        [ShowIf(nameof(modifyVFXes))] public bool stopVFXes;
        [ShowIf(nameof(modifyVFXes))] public VfxPropertyData[] vfxProperties = new VfxPropertyData[0];
        [ShowIf(nameof(HasStopEffect))] public float stopDuration = 1f;
        
        public bool HasStopEffect => stopLights || (modifyVFXes && vfxProperties.Length > 0);
            
        public Element SpawnElement() => new AliveLocationDeathAnimations();
        public bool IsMine(Element element) => element is AliveLocationDeathAnimations;

        [Serializable]
        public struct VfxPropertyData {
            public string propertyName;
            public float value;
        }
    }
}
