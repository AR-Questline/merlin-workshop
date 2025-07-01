using System.Collections;
using Awaken.TG.Main.Saving.Models;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.VisualScripts.Units.Awaits {
    /// <summary>
    /// Delays flow by waiting until the next frame, makes sure user doesn't save during this
    /// </summary>
    [UnitCategory("AR/Awaits")]
    [UnitTitle("AR Wait For Frames")]
    [UnitOrder(4)]
    [UnityEngine.Scripting.Preserve]
    public class ARWaitForFramesUnit : WaitUnit {
        InlineValueInput<bool> _blockSave;
        InlineValueInput<int> _frames;

        protected override void Definition() {
            _blockSave = new InlineValueInput<bool>(ValueInput("block save", true));
            _frames = new InlineValueInput<int>(ValueInput("frames", 1));
            base.Definition();
        }

        protected override IEnumerator Await(Flow flow) {
            SavePostpone postpone = null;
            int frames = _frames.Value(flow);
            if (_blockSave.Value(flow)) {
                postpone = SavePostpone.Create(flow);
                SanityCheck(flow, frames);
            }

            while (frames > 0) {
                yield return null;
                frames--;
            }

            // HACK: Since execution of exit can throw exception we might not reach postpone discard so call it here but execute in next frame.
            DiscardPostpone(postpone).Forget();
            yield return exit;
        }

        static async UniTaskVoid DiscardPostpone(SavePostpone postpone) {
            await UniTask.DelayFrame(1);
            if (postpone is { HasBeenDiscarded: false }) {
                postpone.Discard();
            }
        }

        void SanityCheck(Flow flow, int frames) {
            if (frames > 10) {
                Log.Important?.Error($"Saving has been blocked for longer than 10 frames by {flow.stack.self.name}. Is this valid behaviour?" +
                               " If yes, talk to the programmers.", flow.stack.self);
            }
        }
    }
}