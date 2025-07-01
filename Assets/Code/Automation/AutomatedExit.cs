using Awaken.TG.Main.UI.TitleScreen;
using Awaken.Utility.Automation;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Automation {
    public class AutomatedExit : IAutomation {
        [RuntimeInitializeOnLoadMethod]
        static void Register() {
            Automations.TryRegister("exit", new AutomatedExit());
        }
        
        public async UniTask Run(string[] parameters) {
            await UniTask.DelayFrame(1);
            TitleScreenUI.Exit();
        }
    }
}