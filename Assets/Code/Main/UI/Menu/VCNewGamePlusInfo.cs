using Awaken.TG.Main.NewGamePlus;
using Awaken.TG.MVC;
using Awaken.Utility.GameObjects;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.UI.Menu {
    public class VCNewGamePlusInfo : ViewComponent {
        [SerializeField] GameObject sectionRoot;
        [SerializeField] TMP_Text ngpInfo;

        protected override void OnAttach() {
            if (NewGamePlusSystem.Level <= 0) {
                sectionRoot.SetActiveOptimized(false);
                return;
            }
            
            sectionRoot.SetActiveOptimized(true);
            SetupDescription();
        }
        
        void SetupDescription() {
            string info = NewGamePlusUtils.NewGamePlusLevel(NewGamePlusSystem.Level, true);
            ngpInfo.SetText(info);
        }
    }
}
