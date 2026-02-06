using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Settings.GammaSettingScreen;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.UI.Menu;
using Awaken.TG.MVC;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Technical/Gamma: Open Gamma Screen")]
    public class SEditorOpenGammaScreen : EditorStep {
        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SOpenGammaScreen();
        }
    }
    
    public partial class SOpenGammaScreen : StoryStep {
        StepResult _result;
        public override StepResult Execute(Story story) {
            _result = new StepResult();
            ShowGammaScreen().Forget();
            return _result;
        }
        
        async UniTaskVoid ShowGammaScreen() {
            await UniTask.Delay(600);

            MenuUI spawnedMenuUI = null;
            if (!World.HasAny<MenuUI>()) {
                spawnedMenuUI = World.Add(new MenuUI());
            }
            await GammaScreen.ShowGammaScreen(false);
            spawnedMenuUI?.Discard();
            _result.Complete();
        }
    }
}