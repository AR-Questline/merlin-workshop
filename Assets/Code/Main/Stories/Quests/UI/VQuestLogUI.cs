using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.EmptyContent;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.Utility;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Quests.UI {
    [UsesPrefab("Quest/VQuestLogUI")]
    public class VQuestLogUI : View<QuestLogUI>, IEmptyInfo {
        [SerializeField] Transform leftContent, rightContent;
        [Title("Empty Info")]
        [SerializeField] CanvasGroup contentGroup;
        [SerializeField] VCEmptyInfo emptyInfo;
        
        public Transform LeftContent => leftContent;
        public Transform RightContent => rightContent;
        public CanvasGroup[] ContentGroups => new[] { contentGroup };
        public VCEmptyInfo EmptyInfoView => emptyInfo;
        
        protected override void OnInitialize() {
            PrepareEmptyInfo();
        }
        
        public void PrepareEmptyInfo() {
            emptyInfo.Setup(ContentGroups, LocTerms.EmptyQuestLogInfo.Translate(), LocTerms.EmptyQuestLogDesc.Translate());
        }
    }
}