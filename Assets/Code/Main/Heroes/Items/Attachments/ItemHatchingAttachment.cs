using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    [AttachesTo(typeof(ItemTemplate), AttachmentCategory.ExtraCustom, "For items which can \"hatch\" locations, like pets.")]
    public class ItemHatchingAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField] int ownerStepsToHatch;
        [SerializeField, TemplateType(typeof(LocationTemplate))] TemplateReference hatchSequenceLocation;
        [SerializeField] int hatchSequenceDuration;
        [SerializeField, TemplateType(typeof(LocationTemplate))] TemplateReference locationToHatch;
        [SerializeField] bool hatchInGameplayDomain;
        [SerializeField] ItemHatching.HatchingMethod hatchingMethod;
        [SerializeField] ItemHatching.HatchingPlace hatchingPlace;
        
        public int OwnerStepsToHatch => ownerStepsToHatch;
        public LocationTemplate HatchSequenceLocation => hatchSequenceLocation.Get<LocationTemplate>();
        public int HatchSequenceDuration => hatchSequenceDuration;
        public LocationTemplate LocationToHatch => locationToHatch.Get<LocationTemplate>();
        public bool HatchInGameplayDomain => hatchInGameplayDomain;
        public ItemHatching.HatchingMethod HatchingMethod => hatchingMethod;
        public ItemHatching.HatchingPlace HatchingPlace => hatchingPlace;
        
        public Element SpawnElement() => new ItemHatching();
        public bool IsMine(Element element) => element is ItemHatching;
    }
}