using Awaken.TG.Main.Tutorials.Views;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.Main.Utility.UI.Keys.Components;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Tutorials.TutorialPrompts {
    [SpawnsView(typeof(VTutorialPromptUI), forceParentMember: nameof(ViewHost))]
    public partial class TutorialPrompt : Element<HUD>, IKeyProvider<int> {
        public sealed override bool IsNotSaved => true;

        readonly KeyIcon.Data _key0;
        readonly KeyIcon.Data? _key1;

        public string Description { get; }

        public Transform ViewHost => ParentModel.View<VTutorialHUD>().PromptsParent;
        public bool HasSecondKey => _key1.HasValue;

        TutorialPrompt(string description, KeyIcon.Data key0, KeyIcon.Data? key1) {
            _key0 = key0;
            _key1 = key1;
            Description = description;
        }
        
        public static TutorialPrompt Show(string description, KeyIcon.Data key0, KeyIcon.Data? key1) {
            HUD hud = World.Only<HUD>();
            TutorialPrompt tutorialPrompt = hud.AddElement(new TutorialPrompt(description, key0, key1));
            hud.TriggerChange();
            return tutorialPrompt;
        }

        public KeyIcon.Data GetKey(int key) {
            return key switch {
                0 => _key0,
                1 => _key1 ?? default,
                _ => default
            };
        }
    }
}