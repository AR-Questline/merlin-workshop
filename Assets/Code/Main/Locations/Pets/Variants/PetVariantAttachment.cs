using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Pets.Variants {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Technical, "Defines a pet variant behaviour")]
    public class PetVariantAttachment : MonoBehaviour, IAttachmentSpec {
        const string VFXGroup = "VFX";
        const string TimingsGroup = "Timings";

        [SerializeField] public bool hasDuration = true;
        [SerializeField, ShowIf(nameof(hasDuration))] public float duration;
        [SerializeField, ShowIf(nameof(hasDuration))] public float prolongDuration;
        
        [SerializeField, FoldoutGroup(TimingsGroup)] public float spawnDelayOnStart;
        [SerializeField, FoldoutGroup(TimingsGroup)] public float spawnDelayAfterVfx;
        [SerializeField, FoldoutGroup(TimingsGroup)] public float disappearDelayOnEnd;
        [SerializeField, FoldoutGroup(TimingsGroup)] public float disappearDelayAfterVfx;
        
        
        [ARAssetReferenceSettings(new[] { typeof(GameObject) }, group: AddressableGroup.VFX)]
        [SerializeField, FoldoutGroup(VFXGroup)] public ShareableARAssetReference variantSpawnVFX;
        
        [ARAssetReferenceSettings(new[] { typeof(GameObject) }, group: AddressableGroup.VFX)]
        [SerializeField, FoldoutGroup(VFXGroup)] public ShareableARAssetReference variantEndVFX;

        [SerializeField] VariantType variantType;
        
        public Element SpawnElement() {
            return variantType switch {
                VariantType.Normal => new PetVariant(),
                VariantType.AoE => new AoEPetVariant(),
                VariantType.Mount => new MountPetVariant(),
                VariantType.NpcAlly => new NpcAllyPetVariant(),
                _ => null
            };
        }

        public bool IsMine(Element element) {
            return element.GetType() == variantType switch {
                VariantType.Normal => typeof(PetVariant),
                VariantType.AoE => typeof(AoEPetVariant),
                VariantType.Mount => typeof(MountPetVariant),
                VariantType.NpcAlly => typeof(NpcAllyPetVariant),
                _ => null
            };
        }
        
        [Serializable]
        enum VariantType {
            Normal,
            AoE,
            Mount,
            NpcAlly,
        }
    }
}