using System;
using Awaken.TG.Main.General.NewThings;
using Awaken.TG.Main.Heroes.Development;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Stories.Quests;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Tabs {
    public class VCCharacterSheetTabButton : CharacterSheetTabs.VCHeaderTabButton, INewThingContainer {
        [RichEnumExtends(typeof(CharacterSheetTabType))] 
        [SerializeField] RichEnumReference tabType;
        [SerializeField] bool visibleMarkAllAsSeenPrompt;
        
        public override CharacterSheetTabType Type => tabType.EnumAs<CharacterSheetTabType>();
        public override string ButtonName => Type.Title;

        protected override void OnAttach() {
            base.OnAttach();
            World.Services.Get<NewThingsTracker>().RegisterContainer(this);
        }

        protected override void Refresh(bool selected) {
            base.Refresh(selected);
            if (selected) {
                Target.ParentModel.SetMarkAllAsSeenPromptActive(visibleMarkAllAsSeenPrompt);
            }
        }

        public event Action onNewThingRefresh;
        public bool NewThingBelongsToMe(IModel model) {
            if (Type == CharacterSheetTabType.Quests) {
                return model is Quest {VisibleInQuestLog: true};
            }
            
            if (Type == CharacterSheetTabType.Character) {
                return model is HeroTalentPointsAvailableMarker or HeroStatPointsAvailableMarker or HeroMemoryShardAvailableMarker;
            }

            if (Type == CharacterSheetTabType.Inventory) {
                return model is Item {HiddenOnUI: false, Owner: Hero};
            }
            
            return false;
        }

        public void RefreshNewThingsContainer() {
            onNewThingRefresh?.Invoke();
        }

        protected override void OnDiscard() {
            World.Services.Get<NewThingsTracker>().UnregisterContainer(this);
            base.OnDiscard();
        }
    }
}