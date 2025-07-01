using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Debugging.Cheats;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Sources;

namespace Awaken.TG.Main.UI.TitleScreen {
    public partial class TitleScreenSceneSelection : Element<TitleScreenUI>, IUIAware {
        readonly SceneReference[] _sceneReferences;

        public IReadOnlyList<SceneReference> SceneReferences => _sceneReferences;

        public TitleScreenSceneSelection(SceneReference[] sceneReferences) {
            _sceneReferences = sceneReferences;
        }
        
        protected override void OnInitialize() {
            World.Any<GameUI>().AddElement(new AlwaysPresentHandlers(UIContext.Keyboard, this, this, 1));
        }

        public UIResult Handle(UIEvent evt) {
            if (CheatController.CheatsEnabled() && evt is UIKeyDownAction keyDownAction && keyDownAction.Name == KeyBindings.Debug.DebugSceneSelection) {
                if (ShowScenesDialog()) {
                    return UIResult.Accept;
                }
                return UIResult.Ignore;
            }
            return UIResult.Ignore;
        }

        bool ShowScenesDialog() {
            var existingView = View<VTitleScreenSceneSelection>();
            if (existingView == null) {
                World.SpawnView<VTitleScreenSceneSelection>(this, true);
                return true;
            }
            return false;
        }
        public void Load(int selectedOption) {
            Hide();
            StartGameData data = new() {
                sceneReference = _sceneReferences[selectedOption],
            };
            TitleScreenUtils.StartNewGame(data);
        }
        
        public void Hide() {
            var view = View<VTitleScreenSceneSelection>();
            view.Discard();
        }
    }
}