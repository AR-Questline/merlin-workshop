using System.Collections.Generic;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC.Elements;
using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Attachments {
    public abstract class LogicEmitterAttachmentBase : MonoBehaviour, IAttachmentSpec {
        [LocStringCategory(Category.Interaction)]
        public LocString customInteractLabel;
        [SerializeField, DisableInPlayMode] protected LocationReference locations;
        [SerializeField, TemplateType(typeof(StoryGraph))] protected TemplateReference storyOnInteract;
        
        public bool onlyOnce;
        [ShowIf(nameof(onlyOnce))] public bool onlyOnceEvenIfInactive = true;
        public EventReference interactionSound;
        [ShowIf(nameof(ShowInactiveInteractionSound))]
        public EventReference inactiveInteractionSound;
        
        public IEnumerable<Location> Locations => locations.MatchingLocations(null);
        public TemplateReference StoryOnInteract => storyOnInteract;
        
        protected abstract bool ShowInactiveInteractionSound { get; }
        
        public abstract Element SpawnElement();
        public abstract bool IsMine(Element element);
    }
}