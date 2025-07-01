using System;
using Awaken.TG.Main.UI.ButtonSystem;
using FMODUnity;

namespace Awaken.TG.Main.UI {
    [UnityEngine.Scripting.Preserve]
    public class HoldPromptAudio : PromptAudio {
        public override EventReference KeyUpSound { 
            get => keyUpSound;
            init => keyUpSound = value; 
        }
        
        public Func<bool, EventReference> ConditionalKeyUpSound { get; [UnityEngine.Scripting.Preserve] init; }

        public override void OnHoldKeyUp(Prompt source, bool completed) {
            keyUpSound = ConditionalKeyUpSound(completed);
            base.OnHoldKeyUp(source, completed);
        }
    }
}