using System.Collections;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Saving.Models;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.VisualScripts.Units.Awaits {
    /// <summary>
    /// Delays flow by waiting X seconds, makes sure user doesn't save during this
    /// </summary>
    [UnitCategory("AR/Awaits")]
    [UnitTitle("AR Wait For Seconds")]
    [UnitOrder(4)]
    [UnityEngine.Scripting.Preserve]
    public class ARWaitForSecondsUnit : WaitUnit {
        InlineValueInput<bool> _blockSave;
        InlineValueInput<float> _seconds;
        InlineValueInput<bool> _unscaled;

        protected override void Definition() {
            _blockSave = new InlineValueInput<bool>(ValueInput("block save", true));
            _seconds = new InlineValueInput<float>(ValueInput("seconds", 1f));
            _unscaled = new InlineValueInput<bool>(ValueInput("unscaled", true));
            base.Definition();
        }

        protected override IEnumerator Await(Flow flow) {
            SavePostpone postpone = null;
            float seconds = _seconds.Value(flow);
            bool unscaled = _unscaled.Value(flow);
            if (_blockSave.Value(flow)) {
                postpone = SavePostpone.Create(flow);
                SanityCheck(flow, seconds);
            }

            if (unscaled) {
                yield return new WaitForSecondsRealtime(seconds);
            } else {
                yield return new WaitForSeconds(seconds);
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
        
        void SanityCheck(Flow flow, float seconds) {
            if (seconds > 1f) {
                Log.Minor?.Error($"Saving has been blocked for longer than 1 second by {flow.stack.self.name}. Is this valid behaviour?" +
                               " If yes, talk to the programmers.", flow.stack.self);
            }
        }
    }
}