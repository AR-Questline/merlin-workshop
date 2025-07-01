using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.MVC.Domains;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Scenes.SceneConstructors.SubdividedScenes {
    public class SubdividedSceneUnloadOperation : ISceneLoadOperation {
        const float MotherTimeShare = 0.7f;
        const float ChildrenTimeShare = 1f - MotherTimeShare;
        
        SceneLoadOperation _motherLoad;
        readonly SceneReference _ownerScene;
        readonly SceneLoadOperation[] _childScenes;

        int _loaded;
        int ToLoad => _childScenes.Length;

        public string Name => _ownerScene.Name;
        public IEnumerable<string> MainScenesNames => _ownerScene.Name.Yield();

        public bool IsDone {
            get {
                if (!_childScenes.All(op => op.IsDone)) {
                    return false;
                }

                // Hack for OnComplete callback not getting triggered
                _motherLoad ??= SceneService.UnloadSceneAsync(_ownerScene);
                
                return _motherLoad.IsDone;
            }
        }

        public float Progress => _motherLoad != null
            ? MotherTimeShare + _motherLoad.Progress * ChildrenTimeShare
            : MotherTimeShare * (_loaded / (float) ToLoad);
        
        public SubdividedSceneUnloadOperation(SceneReference ownerScene, SceneLoadOperation[] childScenes) {
            _ownerScene = ownerScene;
            _childScenes = childScenes;

            // TODO: OnComplete callback doesn't get triggered for unloading scene
            // foreach (var scene in otherScenes) {
            //     scene.OnComplete(On`UnloadedOtherScene);
            // }
        }

        [UnityEngine.Scripting.Preserve]
        void OnUnloadedOtherScene() {
            _loaded++;
            if (_loaded == ToLoad) {
                _motherLoad = SceneService.UnloadSceneAsync(_ownerScene);
            }
        }
    }
}