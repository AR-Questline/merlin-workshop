using System;
using Awaken.TG.Main.General.NewThings;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Quests.UI {
    public class VCQuestLogTabButton : QuestLogSubTabs.VCHeaderTabButton, INewThingContainer {
        [RichEnumExtends(typeof(QuestLogSubTabType))] 
        [SerializeField] RichEnumReference tabType;
        public override QuestLogSubTabType Type => tabType.EnumAs<QuestLogSubTabType>();
        public override string ButtonName => Type.Title;
        public event Action onNewThingRefresh;

        protected override void OnAttach() {
            base.OnAttach();
            World.Services.Get<NewThingsTracker>().RegisterContainer(this);
        }

        public bool NewThingBelongsToMe(IModel model) {
            if (Type == QuestLogSubTabType.Default) {
                return model is Quest { HasBeenDiscarded: false, VisibleInQuestLog: true, Category: QuestCategory.Default };
            } 
            
            if (Type == QuestLogSubTabType.Sarras) {
                return model is Quest { HasBeenDiscarded: false, VisibleInQuestLog: true, Category: QuestCategory.Sarras };
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