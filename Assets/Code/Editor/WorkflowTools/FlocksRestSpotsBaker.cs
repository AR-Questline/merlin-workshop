using Awaken.ECS.Flocks;
using Awaken.Utility.Editor.Scenes;
using UnityEngine.SceneManagement;

namespace Awaken.TG.Editor.WorkflowTools {
    public class FlocksRestSpotsBaker : SceneProcessor {
        public override int callbackOrder => 0;
        public override bool canProcessSceneInIsolation => false;
        protected override void OnProcessScene(Scene scene, bool processingInPlaymode) {
            FlockRestSpot.ConnectAllToNearestFlockGroups();
        }
    }
}