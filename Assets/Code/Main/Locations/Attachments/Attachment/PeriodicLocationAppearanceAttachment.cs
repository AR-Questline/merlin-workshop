using System;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Times;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.ExtraCustom, "Creates temporary locations at given time intervals.")]
    public class PeriodicLocationAppearanceAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField] Appearance[] appearances = Array.Empty<Appearance>();
        public Appearance[] Appearances => appearances;
        
        public Element SpawnElement() => new PeriodicLocationAppearance();
        public bool IsMine(Element element) => element is PeriodicLocationAppearance;

        
        [Serializable]
        public struct Appearance {
            const string ConditionsGroup = "Conditions";
            
            [TemplateType(typeof(LocationTemplate))] 
            public TemplateReference locationToAppear;
            public Transform appearPoint;
            public float duration;
            public ARTimeOfDay time;
            public ARTimeSpan randomDelay;
            
            [FoldoutGroup(ConditionsGroup), Range(0f, 1f)] public float chancesToAppear;
            [FoldoutGroup(ConditionsGroup)] public float minDistanceToHero;
            [FoldoutGroup(ConditionsGroup)] public float maxDistanceToHero;

            public LocationTemplate LocationToAppear => locationToAppear.Get<LocationTemplate>();
        }
    }
}