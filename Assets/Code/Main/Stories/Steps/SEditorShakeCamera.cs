using Awaken.TG.Main.Cameras;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Technical/Camera: Shake")]
    public class SEditorShakeCamera : EditorStep {
        public float amplitude = 0.5f;
        public float frequency = 0.15f;
        public float time = 0.5f;
        public float pick = 0.1f;
        public bool awaitShake = true;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SShakeCamera {
                amplitude = amplitude,
                frequency = frequency,
                time = time,
                pick = pick,
                awaitShake = awaitShake
            };
        }
    }
    
    public partial class SShakeCamera : StoryStep {
        public float amplitude = 0.5f;
        public float frequency = 0.15f;
        public float time = 0.5f;
        public float pick = 0.1f;
        public bool awaitShake = true;
        
        public override StepResult Execute(Story story) {
            StepResult result = new();
            AwaitShake(result).Forget();

            return result;
        }

        async UniTask AwaitShake(StepResult result) {
            var gameCamera = World.Only<GameCamera>();
            var shakeTask = gameCamera.Shake(false, amplitude, frequency, time, pick);
            if (awaitShake) {
                await shakeTask;
            }
            
            result.Complete();
        }
    }
}