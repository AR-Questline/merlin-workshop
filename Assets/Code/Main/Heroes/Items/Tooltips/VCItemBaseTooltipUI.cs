using System.Linq;
using Awaken.TG.Main.Heroes.Items.Tooltips.Components;
using Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using Awaken.Utility.GameObjects;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Tooltips {
    public abstract class VCItemBaseTooltipUI : View<ItemTooltipUI>, IPromptHost {
        [Title("General")]
        [SerializeField] GameObject contentParent;
        [SerializeField] Transform promptsHost;
        [SerializeField] bool hidePromptSection;
        [SerializeField, HideIf(nameof(hidePromptSection))] GameObject promptParent;
        
        protected abstract IItemTooltipComponent[] AllSections { get; }
        protected abstract IItemTooltipComponent[] ReadMoreSectionsToShow { get; }
        protected abstract IItemTooltipComponent[] ReadMoreSectionsToHide { get; }

        bool UseReadMore => ReadMoreSectionsToShow.Length > 0 && ReadMoreSectionsToShow.Any(section => section.UseReadMore);
        public Transform PromptsHost => promptsHost;
        bool _readMoreActive;

        public static class Events {
            public static readonly Event<VCItemBaseTooltipUI, VCItemBaseTooltipUI> ContentRefreshed = new(nameof(ContentRefreshed));
        }

        protected override void OnInitialize() {
            if (hidePromptSection) {
                return;
            }
            
            var prompts = Target.AddElement(new Prompts(this));
            prompts.AddPrompt(Prompt.VisualOnlyTap(KeyBindings.UI.Generic.ReadMore, LocTerms.UIGenericReadMore.Translate()), Target);
            this.ListenTo(Events.ContentRefreshed, _ => promptParent.SetActiveOptimized(UseReadMore), this);
        }

        protected override void OnFullyInitialized() {
            SetupSections(this);
        }
        
        public void SetupSections(View view) {
            foreach (var tooltipSection in AllSections) {
                tooltipSection.SetupComponent(view);
            }        
        }

        public virtual void RefreshContent(IItemDescriptor descriptor, IItemDescriptor descriptorToCompare) {
            _readMoreActive = false;
            SetReadMoreSections(false);
            contentParent.SetActive(descriptor != null);
        }
        
        public void ToggleReadMore() {
            if (!UseReadMore) return;
            
            _readMoreActive = !_readMoreActive;
            SetReadMoreSections(_readMoreActive);
        }
        
        void SetReadMoreSections(bool readMore) {
            foreach (var section in ReadMoreSectionsToShow) {
                section.Visibility.SetExternal(readMore);
            }
            
            foreach (var section in ReadMoreSectionsToHide) {
                section.Visibility.SetExternal(!readMore);
            }
        }
    }
}